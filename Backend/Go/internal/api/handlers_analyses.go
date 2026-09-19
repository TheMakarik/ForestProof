package api

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"os"
	"strconv"
	"strings"

	"forestproof-gateway/internal/catalog"
	"forestproof-gateway/internal/hashutil"
	"forestproof-gateway/internal/jobs"
	"forestproof-gateway/internal/layers"
	"forestproof-gateway/internal/upstream"
)

func (d Deps) handleCreateAnalysis(w http.ResponseWriter, r *http.Request) {
	if d.MaxRequestBodyBytes > 0 {
		r.Body = http.MaxBytesReader(w, r.Body, d.MaxRequestBodyBytes)
	}

	var req createAnalysisRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		WriteError(w, http.StatusBadRequest, ErrInvalidRequest, "request body is not valid JSON: "+err.Error())
		return
	}

	aoiID := strings.TrimSpace(req.AoiID)
	polygon := strings.TrimSpace(req.PolygonGeoJSON)

	if (aoiID == "") == (polygon == "") {
		WriteError(w, http.StatusBadRequest, ErrInvalidRequest, "provide exactly one of aoiId or polygonGeoJson")
		return
	}
	if req.StartYear <= 0 || req.EndYear <= 0 {
		WriteError(w, http.StatusBadRequest, ErrInvalidRequest, "startYear and endYear are required")
		return
	}
	if req.StartYear >= req.EndYear {
		WriteError(w, http.StatusBadRequest, ErrInvalidRequest, "startYear must be less than endYear")
		return
	}
	if d.MinAnalysisYear > 0 && req.StartYear < d.MinAnalysisYear {
		WriteError(w, http.StatusBadRequest, ErrInvalidRequest, "startYear is before the supported range")
		return
	}
	if d.MaxAnalysisYear > 0 && req.EndYear > d.MaxAnalysisYear {
		WriteError(w, http.StatusBadRequest, ErrInvalidRequest, "endYear is after the supported range")
		return
	}

	var canonicalPolygon string
	if polygon != "" {
		canonical, err := canonicalizeJSON(polygon)
		if err != nil {
			WriteError(w, http.StatusBadRequest, ErrInvalidRequest, "polygonGeoJson is not valid JSON: "+err.Error())
			return
		}
		canonicalPolygon = canonical
	}
	if aoiID != "" {
		if _, ok := catalog.FindArea(d.Areas, aoiID); !ok {
			WriteError(w, http.StatusBadRequest, ErrInvalidRequest, "unknown aoiId: "+aoiID)
			return
		}
	}

	methodProfile := strings.TrimSpace(req.MethodProfile)
	hash := hashutil.StringSHA256(
		aoiID, canonicalPolygon,
		strconv.Itoa(req.StartYear), strconv.Itoa(req.EndYear),
		methodProfile, d.MethodProfileVersion,
	)

	if existing, ok := d.Store.FindByHash(hash); ok {
		WriteJSON(w, http.StatusOK, toCreateResponse(existing, true))
		return
	}

	job := d.Store.Create(jobs.Request{
		AoiID:          aoiID,
		PolygonGeoJSON: polygon,
		MethodProfile:  methodProfile,
		StartYear:      req.StartYear,
		EndYear:        req.EndYear,
	}, hash)

	// Deliberately context.Background(), not r.Context(): this pipeline
	// must outlive the HTTP request that triggered it.
	go d.Executor.Run(context.Background(), job.ID)

	WriteJSON(w, http.StatusAccepted, toCreateResponse(job, false))
}

func toCreateResponse(job jobs.Job, idempotent bool) createAnalysisResponse {
	return createAnalysisResponse{
		ID:         job.ID,
		Status:     string(job.Status),
		AoiID:      job.Request.AoiID,
		InputHash:  job.InputHash,
		StartYear:  job.Request.StartYear,
		EndYear:    job.Request.EndYear,
		CreatedAt:  job.CreatedAt,
		Idempotent: idempotent,
	}
}

// canonicalizeJSON re-encodes raw as a compact, key-ordered JSON string, so
// two semantically identical polygons submitted with different whitespace
// or key order hash the same way.
func canonicalizeJSON(raw string) (string, error) {
	var v any
	if err := json.Unmarshal([]byte(raw), &v); err != nil {
		return "", err
	}
	out, err := json.Marshal(v)
	if err != nil {
		return "", err
	}
	return string(out), nil
}

func (d Deps) handleGetStatus(w http.ResponseWriter, r *http.Request) {
	job, ok := d.requireJob(w, r)
	if !ok {
		return
	}
	WriteJSON(w, http.StatusOK, statusResponse{
		ID:           job.ID,
		Status:       string(job.Status),
		Phase:        job.Phase,
		Progress:     job.Progress,
		ErrorMessage: job.ErrorMessage,
		Warnings:     job.Warnings,
		CreatedAt:    job.CreatedAt,
		UpdatedAt:    job.UpdatedAt,
	})
}

func (d Deps) handleGetSummary(w http.ResponseWriter, r *http.Request) {
	job, ok := d.requireJob(w, r)
	if !ok {
		return
	}
	if !job.TerminalSuccess() {
		writeNotReady(w, job)
		return
	}
	if job.Bundle == nil || job.Bundle.SummaryJSON == nil {
		WriteError(w, http.StatusConflict, ErrConflict, "summary not available for this job")
		return
	}
	w.Header().Set("Content-Type", "application/json")
	w.Write(job.Bundle.SummaryJSON)
}

func (d Deps) handleGetChanges(w http.ResponseWriter, r *http.Request) {
	job, ok := d.requireJob(w, r)
	if !ok {
		return
	}
	if !job.TerminalSuccess() {
		writeNotReady(w, job)
		return
	}
	if job.Bundle == nil || job.Bundle.ChangesJSON == nil {
		WriteError(w, http.StatusConflict, ErrConflict, "changes not available for this job")
		return
	}
	w.Header().Set("Content-Type", "application/geo+json")
	w.Write(job.Bundle.ChangesJSON)
}

func (d Deps) handleGetReport(w http.ResponseWriter, r *http.Request) {
	job, ok := d.requireJob(w, r)
	if !ok {
		return
	}
	if !job.TerminalSuccess() {
		writeNotReady(w, job)
		return
	}

	format := strings.ToLower(strings.TrimSpace(r.URL.Query().Get("format")))
	if format == "" {
		format = "pdf"
	}
	contentType, ok := reportContentType(format)
	if !ok {
		WriteError(w, http.StatusBadRequest, ErrInvalidRequest, "unsupported format "+format+"; want pdf, html or json")
		return
	}

	// The executor caches the default PDF bundle, so serve that when we can
	// instead of recomputing it upstream.
	if format == "pdf" && job.HasReport {
		data, err := os.ReadFile(d.Store.ReportPath(job.ID))
		if err != nil {
			WriteError(w, http.StatusInternalServerError, ErrInternal, "report file missing on disk")
			return
		}
		w.Header().Set("Content-Type", contentType)
		w.Write(data)
		return
	}

	data, err := d.Upstream.GenerateReportForRequest(r.Context(), toUpstreamRequest(job.Request), format)
	if err != nil {
		writeUpstreamError(w, err)
		return
	}
	w.Header().Set("Content-Type", contentType)
	w.Write(data)
}

// reportContentType maps a public report format to its response
// Content-Type, reporting false for anything unsupported.
func reportContentType(format string) (string, bool) {
	switch format {
	case "pdf":
		return "application/pdf", true
	case "html":
		return "text/html", true
	case "json":
		return "application/json", true
	default:
		return "", false
	}
}

// toUpstreamRequest converts a stored job request into the upstream client's
// request shape.
func toUpstreamRequest(req jobs.Request) upstream.CreateAnalysisRequest {
	return upstream.CreateAnalysisRequest{
		AoiID:          req.AoiID,
		PolygonGeoJSON: req.PolygonGeoJSON,
		MethodProfile:  req.MethodProfile,
		StartYear:      req.StartYear,
		EndYear:        req.EndYear,
	}
}

// handleSensitivity proxies GET /api/v1/experiments/sensitivity to the C#
// backend, which owns the sensitivity computation.
func (d Deps) handleSensitivity(w http.ResponseWriter, r *http.Request) {
	aoiID := strings.TrimSpace(r.URL.Query().Get("aoiId"))
	if aoiID == "" {
		WriteError(w, http.StatusBadRequest, ErrInvalidRequest, "aoiId is required")
		return
	}

	startYear, err := parseYearQuery(r, "startYear")
	if err != nil {
		WriteError(w, http.StatusBadRequest, ErrInvalidRequest, err.Error())
		return
	}
	endYear, err := parseYearQuery(r, "endYear")
	if err != nil {
		WriteError(w, http.StatusBadRequest, ErrInvalidRequest, err.Error())
		return
	}

	body, err := d.Upstream.GetSensitivity(r.Context(), aoiID, startYear, endYear)
	if err != nil {
		writeUpstreamError(w, err)
		return
	}
	w.Header().Set("Content-Type", "application/json")
	w.Write(body)
}

func parseYearQuery(r *http.Request, name string) (int, error) {
	raw := strings.TrimSpace(r.URL.Query().Get(name))
	if raw == "" {
		return 0, fmt.Errorf("%s is required", name)
	}
	value, err := strconv.Atoi(raw)
	if err != nil || value <= 0 {
		return 0, fmt.Errorf("%s must be a positive integer", name)
	}
	return value, nil
}

func (d Deps) handleListLayers(w http.ResponseWriter, r *http.Request) {
	job, ok := d.requireJob(w, r)
	if !ok {
		return
	}
	if job.Request.AoiID == "" {
		WriteError(w, http.StatusNotFound, ErrNotFound, "layers require an aoi_id; this analysis used a custom polygon")
		return
	}
	list := layers.ListLayers(d.layersDeps(), job.Request.AoiID, job.Request.StartYear, job.Request.EndYear)
	WriteJSON(w, http.StatusOK, layersListResponse{AoiID: job.Request.AoiID, Layers: list})
}

func (d Deps) handleGetLayer(w http.ResponseWriter, r *http.Request) {
	job, ok := d.requireJob(w, r)
	if !ok {
		return
	}
	if job.Request.AoiID == "" {
		WriteError(w, http.StatusNotFound, ErrNotFound, "layers require an aoi_id; this analysis used a custom polygon")
		return
	}

	layerKey := r.PathValue("layer")
	file, _, err := layers.ResolveLayer(r.Context(), d.layersDeps(), job.Request.AoiID, job.Request.StartYear, job.Request.EndYear, layerKey)
	if err != nil {
		WriteError(w, http.StatusNotFound, ErrNotFound, err.Error())
		return
	}

	w.Header().Set("Content-Type", file.ContentType)
	if len(file.Bytes) > 0 {
		w.Write(file.Bytes)
		return
	}

	f, err := os.Open(file.Path)
	if err != nil {
		WriteError(w, http.StatusInternalServerError, ErrInternal, "layer file missing on disk")
		return
	}
	defer f.Close()

	info, err := f.Stat()
	if err != nil {
		WriteError(w, http.StatusInternalServerError, ErrInternal, "layer file stat failed")
		return
	}

	http.ServeContent(w, r, file.Path, info.ModTime(), f)
}

// requireJob loads the job named by the {id} path parameter, writing a 404
// and returning ok=false if it doesn't exist.
func (d Deps) requireJob(w http.ResponseWriter, r *http.Request) (jobs.Job, bool) {
	id := r.PathValue("id")
	job, ok := d.Store.Get(id)
	if !ok {
		WriteError(w, http.StatusNotFound, ErrNotFound, "unknown analysis id: "+id)
		return jobs.Job{}, false
	}
	return job, true
}

// writeNotReady writes the appropriate response for a job that hasn't
// reached a terminal successful status yet: 409 while still in flight, or
// the upstream failure message if it failed.
func writeNotReady(w http.ResponseWriter, job jobs.Job) {
	if job.Status == jobs.StatusFailed {
		WriteError(w, http.StatusConflict, ErrConflict, "analysis failed: "+job.ErrorMessage)
		return
	}
	WriteError(w, http.StatusConflict, ErrConflict, "analysis is still "+string(job.Status))
}
