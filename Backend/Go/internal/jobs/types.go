// Package jobs owns the gateway's concept of an analysis run: a Go-assigned
// id, its lifecycle status, and a cached result bundle — none of which
// exist on the C# side, where every call recomputes from scratch and there
// is no addressable "run id".
package jobs

import (
	"encoding/json"
	"time"
)

// Status is a job's lifecycle phase, matching WORK_SPLIT.md's required set.
type Status string

const (
	StatusValidating       Status = "validating"
	StatusRunning          Status = "running"
	StatusComplete         Status = "complete"
	StatusPartial          Status = "partial"
	StatusUnitsUnavailable Status = "units_unavailable"
	StatusFailed           Status = "failed"
)

// Request is the normalized analysis request a job was created from.
type Request struct {
	AoiID          string `json:"aoiId,omitempty"`
	PolygonGeoJSON string `json:"polygonGeoJson,omitempty"`
	MethodProfile  string `json:"methodProfile,omitempty"`
	StartYear      int    `json:"startYear"`
	EndYear        int    `json:"endYear"`
}

// Bundle is the cached result of running a job's request against the
// upstream C# API. ChangesJSON and ReportPDF stay nil for a polygon-only
// job, since C# only exposes /changes and /reports by aoi_id.
type Bundle struct {
	SummaryJSON json.RawMessage `json:"summaryJson,omitempty"`
	ChangesJSON json.RawMessage `json:"changesJson,omitempty"`
	ReportPDF   []byte          `json:"-"` // persisted separately, see Store
}

// Job is one analysis run tracked by the gateway.
type Job struct {
	ID           string    `json:"id"`
	InputHash    string    `json:"inputHash"`
	Status       Status    `json:"status"`
	Phase        string    `json:"phase"`
	Progress     int       `json:"progress"`
	ErrorMessage string    `json:"errorMessage,omitempty"`
	Warnings     []string  `json:"warnings,omitempty"`
	Request      Request   `json:"request"`
	Bundle       *Bundle   `json:"bundle,omitempty"`
	HasReport    bool      `json:"hasReport"`
	CreatedAt    time.Time `json:"createdAt"`
	UpdatedAt    time.Time `json:"updatedAt"`
}

// TerminalSuccess reports whether the job reached a status that carries a
// usable result bundle (complete, partial, or units_unavailable — as
// opposed to still in flight, or failed outright).
func (j Job) TerminalSuccess() bool {
	switch j.Status {
	case StatusComplete, StatusPartial, StatusUnitsUnavailable:
		return true
	default:
		return false
	}
}
