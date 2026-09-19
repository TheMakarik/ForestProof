// Package upstream is the gateway's HTTP client for the C# ForestProof
// backend, which owns all of the actual computation. It treats C#'s JSON
// responses as opaque payloads wherever possible (rather than re-modeling
// C#'s response schema) so the gateway never drifts out of sync with the
// upstream contract as it evolves.
package upstream

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"strconv"
)

// CreateAnalysisRequest mirrors C#'s CreateAnalysisRequest body. Exactly
// one of AoiID/PolygonGeoJSON should be set.
type CreateAnalysisRequest struct {
	AoiID          string
	PolygonGeoJSON string
	MethodProfile  string
	StartYear      int
	EndYear        int
}

// StatusError is returned when C# responds with a non-2xx status. It
// carries the upstream's {"message": "..."} body so callers can surface
// the original validation error instead of a generic failure.
type StatusError struct {
	StatusCode int
	Message    string
}

func (e *StatusError) Error() string {
	return fmt.Sprintf("upstream: status %d: %s", e.StatusCode, e.Message)
}

// Client talks to the C# backend's /api/v1 surface.
type Client struct {
	BaseURL    string
	HTTPClient *http.Client
}

// NewClient returns a Client. If httpClient is nil, http.DefaultClient is
// used.
func NewClient(baseURL string, httpClient *http.Client) *Client {
	if httpClient == nil {
		httpClient = http.DefaultClient
	}
	return &Client{BaseURL: baseURL, HTTPClient: httpClient}
}

type createAnalysisBody struct {
	AoiID          string `json:"aoiId,omitempty"`
	PolygonGeoJSON string `json:"polygonGeoJson,omitempty"`
	MethodProfile  string `json:"methodProfile,omitempty"`
	StartYear      int    `json:"startYear"`
	EndYear        int    `json:"endYear"`
}

// reportRequestBody mirrors C#'s POST /api/v1/analyses/reports body. Unlike
// createAnalysisBody it carries the requested output format.
type reportRequestBody struct {
	AoiID          string `json:"aoiId,omitempty"`
	PolygonGeoJSON string `json:"polygonGeoJson,omitempty"`
	StartYear      int    `json:"startYear"`
	EndYear        int    `json:"endYear"`
	Format         string `json:"format,omitempty"`
}

type summaryStatusOnly struct {
	Status string `json:"status"`
}

// CreateAnalysis calls POST /api/v1/analyses. On success it returns the
// full raw response body (byte-for-byte, to be cached and re-served as-is)
// plus the extracted "status" field used to drive the job state machine.
func (c *Client) CreateAnalysis(ctx context.Context, req CreateAnalysisRequest) (json.RawMessage, string, error) {
	payload, err := json.Marshal(createAnalysisBody{
		AoiID:          req.AoiID,
		PolygonGeoJSON: req.PolygonGeoJSON,
		MethodProfile:  req.MethodProfile,
		StartYear:      req.StartYear,
		EndYear:        req.EndYear,
	})
	if err != nil {
		return nil, "", fmt.Errorf("upstream: encode create-analysis body: %w", err)
	}

	body, err := c.doJSON(ctx, http.MethodPost, "/api/v1/analyses", bytes.NewReader(payload), nil)
	if err != nil {
		return nil, "", err
	}

	var parsed summaryStatusOnly
	if err := json.Unmarshal(body, &parsed); err != nil {
		return nil, "", fmt.Errorf("upstream: decode create-analysis status field: %w", err)
	}

	return json.RawMessage(body), parsed.Status, nil
}

// GetChangesForRequest calls POST /api/v1/analyses/changes with the same
// request body shape as CreateAnalysis, so change detection works for both
// an aoiId and a custom polygon. It returns the raw GeoJSON
// FeatureCollection body.
func (c *Client) GetChangesForRequest(ctx context.Context, req CreateAnalysisRequest) (json.RawMessage, error) {
	payload, err := json.Marshal(createAnalysisBody{
		AoiID:          req.AoiID,
		PolygonGeoJSON: req.PolygonGeoJSON,
		MethodProfile:  req.MethodProfile,
		StartYear:      req.StartYear,
		EndYear:        req.EndYear,
	})
	if err != nil {
		return nil, fmt.Errorf("upstream: encode changes body: %w", err)
	}

	body, err := c.doJSON(ctx, http.MethodPost, "/api/v1/analyses/changes", bytes.NewReader(payload), nil)
	if err != nil {
		return nil, err
	}
	return json.RawMessage(body), nil
}

// GenerateReportForRequest calls POST /api/v1/analyses/reports with the
// given output format (pdf, html or json) and returns the raw response
// bytes, so reports work for both an aoiId and a custom polygon.
func (c *Client) GenerateReportForRequest(ctx context.Context, req CreateAnalysisRequest, format string) ([]byte, error) {
	payload, err := json.Marshal(reportRequestBody{
		AoiID:          req.AoiID,
		PolygonGeoJSON: req.PolygonGeoJSON,
		StartYear:      req.StartYear,
		EndYear:        req.EndYear,
		Format:         format,
	})
	if err != nil {
		return nil, fmt.Errorf("upstream: encode report body: %w", err)
	}

	return c.doJSON(ctx, http.MethodPost, "/api/v1/analyses/reports", bytes.NewReader(payload), nil)
}

// GetSensitivity calls GET /api/v1/experiments/sensitivity and returns the
// raw JSON body.
func (c *Client) GetSensitivity(ctx context.Context, aoiID string, startYear, endYear int) (json.RawMessage, error) {
	query := url.Values{
		"startYear": {strconv.Itoa(startYear)},
		"endYear":   {strconv.Itoa(endYear)},
	}
	if aoiID != "" {
		query.Set("aoiId", aoiID)
	}

	body, err := c.doJSON(ctx, http.MethodGet, "/api/v1/experiments/sensitivity?"+query.Encode(), nil, nil)
	if err != nil {
		return nil, err
	}
	return json.RawMessage(body), nil
}

// GetChanges calls GET /api/v1/analyses/{aoiId}/changes and returns the raw
// GeoJSON FeatureCollection body. C# only supports this by aoiId, not by
// polygon.
func (c *Client) GetChanges(ctx context.Context, aoiID string, startYear, endYear int) (json.RawMessage, error) {
	path := fmt.Sprintf("/api/v1/analyses/%s/changes", url.PathEscape(aoiID))
	query := url.Values{
		"startYear": {strconv.Itoa(startYear)},
		"endYear":   {strconv.Itoa(endYear)},
	}
	body, err := c.doJSON(ctx, http.MethodGet, path+"?"+query.Encode(), nil, nil)
	if err != nil {
		return nil, err
	}
	return json.RawMessage(body), nil
}

// GenerateReport calls POST /api/v1/analyses/{aoiId}/reports and returns
// the raw PDF bytes. C# only supports this by aoiId, not by polygon.
func (c *Client) GenerateReport(ctx context.Context, aoiID string, startYear, endYear int) ([]byte, error) {
	path := fmt.Sprintf("/api/v1/analyses/%s/reports", url.PathEscape(aoiID))
	query := url.Values{
		"startYear": {strconv.Itoa(startYear)},
		"endYear":   {strconv.Itoa(endYear)},
	}

	httpReq, err := http.NewRequestWithContext(ctx, http.MethodPost, c.BaseURL+path+"?"+query.Encode(), nil)
	if err != nil {
		return nil, fmt.Errorf("upstream: build report request: %w", err)
	}

	resp, err := c.HTTPClient.Do(httpReq)
	if err != nil {
		return nil, fmt.Errorf("upstream: report request failed: %w", err)
	}
	defer resp.Body.Close()

	respBody, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, fmt.Errorf("upstream: read report response: %w", err)
	}

	if resp.StatusCode < 200 || resp.StatusCode >= 300 {
		return nil, statusErrorFromBody(resp.StatusCode, respBody)
	}
	return respBody, nil
}

// GetAreas calls GET /api/v1/areas and returns the raw JSON body.
func (c *Client) GetAreas(ctx context.Context) (json.RawMessage, error) {
	body, err := c.doJSON(ctx, http.MethodGet, "/api/v1/areas", nil, nil)
	if err != nil {
		return nil, err
	}
	return json.RawMessage(body), nil
}

// GetProjects calls GET /api/v1/projects and returns the raw JSON body.
func (c *Client) GetProjects(ctx context.Context) (json.RawMessage, error) {
	body, err := c.doJSON(ctx, http.MethodGet, "/api/v1/projects", nil, nil)
	if err != nil {
		return nil, err
	}
	return json.RawMessage(body), nil
}

// doJSON performs an HTTP request against the upstream base URL, returning
// the raw response body on 2xx or a *StatusError decoded from a
// {"message": "..."} body on any other status.
func (c *Client) doJSON(ctx context.Context, method, path string, body io.Reader, headers http.Header) ([]byte, error) {
	httpReq, err := http.NewRequestWithContext(ctx, method, c.BaseURL+path, body)
	if err != nil {
		return nil, fmt.Errorf("upstream: build %s %s request: %w", method, path, err)
	}
	httpReq.Header.Set("Content-Type", "application/json")
	httpReq.Header.Set("Accept", "application/json")
	for k, vs := range headers {
		for _, v := range vs {
			httpReq.Header.Add(k, v)
		}
	}

	resp, err := c.HTTPClient.Do(httpReq)
	if err != nil {
		return nil, fmt.Errorf("upstream: %s %s failed: %w", method, path, err)
	}
	defer resp.Body.Close()

	respBody, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, fmt.Errorf("upstream: read %s %s response: %w", method, path, err)
	}

	if resp.StatusCode < 200 || resp.StatusCode >= 300 {
		return nil, statusErrorFromBody(resp.StatusCode, respBody)
	}
	return respBody, nil
}

func statusErrorFromBody(statusCode int, body []byte) error {
	var parsed struct {
		Message string `json:"message"`
	}
	if err := json.Unmarshal(body, &parsed); err != nil || parsed.Message == "" {
		return &StatusError{StatusCode: statusCode, Message: string(body)}
	}
	return &StatusError{StatusCode: statusCode, Message: parsed.Message}
}
