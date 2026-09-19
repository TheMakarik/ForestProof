// Package api is the gateway's public HTTP surface: the /api/v1 routes
// described in Backend/WORK_SPLIT.md, backed by the jobs/catalog/verify/
// layers/upstream packages.
package api

import (
	"log"
	"net/http"
	"time"

	"forestproof-gateway/internal/catalog"
	"forestproof-gateway/internal/jobs"
	"forestproof-gateway/internal/layers"
	"forestproof-gateway/internal/stac"
	"forestproof-gateway/internal/upstream"
	"forestproof-gateway/internal/verify"
)

// Deps bundles every dependency the route handlers need.
type Deps struct {
	Store                *jobs.Store
	Executor             *jobs.Executor
	Upstream             *upstream.Client
	Areas                []catalog.Area
	Sources              []catalog.Source
	FileCatalog          []catalog.FileEntry
	Scenes               []catalog.Scene
	Verifier             *verify.Scanner
	StacClient           *stac.Client
	StacCache            *stac.Cache
	DataRoot             string
	MethodProfileVersion string
	MinAnalysisYear      int
	MaxAnalysisYear      int
	MaxRequestBodyBytes  int64
}

// layersDeps adapts Deps to layers.Deps.
func (d Deps) layersDeps() layers.Deps {
	return layers.Deps{
		DataRoot:    d.DataRoot,
		Areas:       d.Areas,
		Sources:     d.Sources,
		FileCatalog: d.FileCatalog,
		Scenes:      d.Scenes,
		StacClient:  d.StacClient,
		StacCache:   d.StacCache,
	}
}

// NewRouter builds the gateway's HTTP handler.
func NewRouter(deps Deps) http.Handler {
	mux := http.NewServeMux()

	mux.HandleFunc("GET /healthz", handleHealthz)

	mux.HandleFunc("POST /api/v1/analyses", deps.handleCreateAnalysis)
	mux.HandleFunc("GET /api/v1/analyses/{id}/status", deps.handleGetStatus)
	mux.HandleFunc("GET /api/v1/analyses/{id}/summary", deps.handleGetSummary)
	mux.HandleFunc("GET /api/v1/analyses/{id}/changes", deps.handleGetChanges)
	mux.HandleFunc("GET /api/v1/analyses/{id}/layers", deps.handleListLayers)
	mux.HandleFunc("GET /api/v1/analyses/{id}/layers/{layer}", deps.handleGetLayer)
	mux.HandleFunc("POST /api/v1/analyses/{id}/reports", deps.handleGetReport)
	mux.HandleFunc("GET /api/v1/analyses/{id}/yearly.csv", deps.handleGetYearlyCsv)

	mux.HandleFunc("GET /api/v1/sources", deps.handleGetSources)
	mux.HandleFunc("GET /api/v1/areas", deps.handleGetAreas)
	mux.HandleFunc("GET /api/v1/projects", deps.handleGetProjects)

	mux.HandleFunc("GET /api/v1/experiments/sensitivity", deps.handleSensitivity)

	return withLogging(mux)
}

func handleHealthz(w http.ResponseWriter, r *http.Request) {
	WriteJSON(w, http.StatusOK, map[string]string{"status": "ok"})
}

// statusRecorder captures the response status for the logging middleware.
type statusRecorder struct {
	http.ResponseWriter
	status int
}

func (r *statusRecorder) WriteHeader(status int) {
	r.status = status
	r.ResponseWriter.WriteHeader(status)
}

func withLogging(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := time.Now()
		rec := &statusRecorder{ResponseWriter: w, status: http.StatusOK}
		next.ServeHTTP(rec, r)
		log.Printf("%s %s -> %d (%s)", r.Method, r.URL.Path, rec.status, time.Since(start))
	})
}
