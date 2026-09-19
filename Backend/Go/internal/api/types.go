package api

import (
	"time"

	"forestproof-gateway/internal/layers"
	"forestproof-gateway/internal/verify"
)

// createAnalysisRequest is the public POST /api/v1/analyses body.
type createAnalysisRequest struct {
	AoiID          string `json:"aoiId"`
	PolygonGeoJSON string `json:"polygonGeoJson"`
	MethodProfile  string `json:"methodProfile"`
	StartYear      int    `json:"startYear"`
	EndYear        int    `json:"endYear"`
}

// createAnalysisResponse is returned by both a fresh POST and an idempotent
// replay of an identical one.
type createAnalysisResponse struct {
	ID         string    `json:"id"`
	Status     string    `json:"status"`
	AoiID      string    `json:"aoiId,omitempty"`
	InputHash  string    `json:"inputHash"`
	StartYear  int       `json:"startYear"`
	EndYear    int       `json:"endYear"`
	CreatedAt  time.Time `json:"createdAt"`
	Idempotent bool      `json:"idempotent"`
}

// statusResponse is returned by GET .../status.
type statusResponse struct {
	ID            string    `json:"id"`
	Status        string    `json:"status"`
	Phase         string    `json:"phase"`
	Progress      int       `json:"progress"`
	ErrorMessage  string    `json:"errorMessage,omitempty"`
	Warnings      []string  `json:"warnings,omitempty"`
	MethodVersion string    `json:"methodVersion,omitempty"`
	DataVersion   string    `json:"dataVersion,omitempty"`
	CreatedAt     time.Time `json:"createdAt"`
	UpdatedAt     time.Time `json:"updatedAt"`
}

// layersListResponse is returned by GET .../layers.
type layersListResponse struct {
	AoiID  string             `json:"aoiId"`
	Layers []layers.LayerInfo `json:"layers"`
}

// sourceFileInfo is one cataloged file within a sourceResponse.
type sourceFileInfo struct {
	RelativePath   string        `json:"relativePath"`
	AoiID          string        `json:"aoiId"`
	Sha256Recorded string        `json:"sha256Recorded"`
	Sha256Actual   string        `json:"sha256Actual,omitempty"`
	Verified       verify.Status `json:"verified"`
	SizeBytes      int64         `json:"sizeBytes"`
}

// sourceResponse is one entry of GET /api/v1/sources.
type sourceResponse struct {
	SourceID            string           `json:"sourceId"`
	Product             string           `json:"product"`
	Version             string           `json:"version"`
	LicenseURL          string           `json:"licenseUrl"`
	RequiredAttribution string           `json:"requiredAttribution"`
	Files               []sourceFileInfo `json:"files"`
}
