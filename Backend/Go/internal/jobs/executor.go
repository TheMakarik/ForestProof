package jobs

import (
	"context"
	"encoding/json"
	"errors"
	"time"

	"forestproof-gateway/internal/upstream"
)

// Upstream is the subset of upstream.Client the executor depends on. It's
// an interface purely so tests can inject a stub instead of a real HTTP
// client.
type Upstream interface {
	CreateAnalysis(ctx context.Context, req upstream.CreateAnalysisRequest) (json.RawMessage, string, error)
	GetChanges(ctx context.Context, aoiID string, startYear, endYear int) (json.RawMessage, error)
	GenerateReport(ctx context.Context, aoiID string, startYear, endYear int) ([]byte, error)
}

// Executor runs a job's request against Upstream in the background and
// records progress/results on Store.
type Executor struct {
	Store    *Store
	Upstream Upstream
	Timeout  time.Duration
}

// NewExecutor returns an Executor. A zero or negative timeout defaults to
// 120s.
func NewExecutor(store *Store, up Upstream, timeout time.Duration) *Executor {
	if timeout <= 0 {
		timeout = 120 * time.Second
	}
	return &Executor{Store: store, Upstream: up, Timeout: timeout}
}

// Run executes jobID's pipeline: CreateAnalysis, then — only if the job's
// request has an AoiID (C# has no polygon-based route for these two) —
// GetChanges and GenerateReport. Every step's outcome is folded into the
// job via Store.Update.
//
// Callers MUST pass a context that outlives the HTTP request that
// triggered the job (typically context.Background()), never the inbound
// request's context — using the request's context would cancel this whole
// background pipeline the instant the client's connection closes.
func (e *Executor) Run(ctx context.Context, jobID string) {
	ctx, cancel := context.WithTimeout(ctx, e.Timeout)
	defer cancel()

	job, ok := e.Store.Get(jobID)
	if !ok {
		return
	}

	_ = e.Store.Update(jobID, func(j *Job) {
		j.Status = StatusRunning
		j.Phase = "calling_summary"
		j.Progress = 10
	})

	rawSummary, csharpStatus, err := e.Upstream.CreateAnalysis(ctx, upstream.CreateAnalysisRequest{
		AoiID:          job.Request.AoiID,
		PolygonGeoJSON: job.Request.PolygonGeoJSON,
		StartYear:      job.Request.StartYear,
		EndYear:        job.Request.EndYear,
	})
	if err != nil {
		e.fail(jobID, err)
		return
	}

	mappedStatus := mapUpstreamStatus(csharpStatus)
	_ = e.Store.Update(jobID, func(j *Job) {
		j.Bundle = &Bundle{SummaryJSON: rawSummary}
		j.Status = mappedStatus
		j.Progress = 40
	})

	if mappedStatus == StatusFailed {
		_ = e.Store.Update(jobID, func(j *Job) {
			j.ErrorMessage = "upstream returned unrecognized status " + csharpStatus
		})
		return
	}

	if job.Request.AoiID == "" {
		_ = e.Store.Update(jobID, func(j *Job) {
			j.Phase = "complete"
			j.Progress = 100
			j.Warnings = append(j.Warnings,
				"zone geometry and PDF report unavailable for custom-polygon analyses — "+
					"upstream only exposes /changes and /reports by aoi_id")
		})
		return
	}

	_ = e.Store.Update(jobID, func(j *Job) {
		j.Phase = "calling_changes"
		j.Progress = 55
	})

	rawChanges, err := e.Upstream.GetChanges(ctx, job.Request.AoiID, job.Request.StartYear, job.Request.EndYear)
	if err != nil {
		e.fail(jobID, err)
		return
	}
	_ = e.Store.Update(jobID, func(j *Job) {
		if j.Bundle == nil {
			j.Bundle = &Bundle{}
		}
		j.Bundle.ChangesJSON = rawChanges
		j.Phase = "calling_report"
		j.Progress = 85
	})

	reportPDF, err := e.Upstream.GenerateReport(ctx, job.Request.AoiID, job.Request.StartYear, job.Request.EndYear)
	if err != nil {
		e.fail(jobID, err)
		return
	}
	_ = e.Store.Update(jobID, func(j *Job) {
		if j.Bundle == nil {
			j.Bundle = &Bundle{}
		}
		j.Bundle.ReportPDF = reportPDF
		j.Phase = "complete"
		j.Progress = 100
	})
}

func (e *Executor) fail(jobID string, err error) {
	message := err.Error()
	if errors.Is(err, context.DeadlineExceeded) {
		message = "timed out waiting for upstream: " + message
	}

	_ = e.Store.Update(jobID, func(j *Job) {
		j.Status = StatusFailed
		j.ErrorMessage = message
	})
}

// mapUpstreamStatus translates C#'s AnalysisSummaryResponse.Status string
// into a jobs.Status. An unrecognized value maps to failed rather than
// silently defaulting to something success-shaped.
func mapUpstreamStatus(csharpStatus string) Status {
	switch csharpStatus {
	case "Complete":
		return StatusComplete
	case "Partial":
		return StatusPartial
	case "UnitsUnavailable":
		return StatusUnitsUnavailable
	default:
		return StatusFailed
	}
}
