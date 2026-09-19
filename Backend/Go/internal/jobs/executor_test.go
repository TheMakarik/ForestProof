package jobs

import (
	"context"
	"encoding/json"
	"errors"
	"sync/atomic"
	"testing"
	"time"

	"forestproof-gateway/internal/upstream"
)

type stubUpstream struct {
	createAnalysisStatus string
	createAnalysisErr    error
	getChangesErr        error
	generateReportErr    error
	sleep                time.Duration

	createCalls  int32
	changesCalls int32
	reportCalls  int32
}

func (s *stubUpstream) CreateAnalysis(ctx context.Context, req upstream.CreateAnalysisRequest) (json.RawMessage, string, error) {
	atomic.AddInt32(&s.createCalls, 1)
	if s.sleep > 0 {
		select {
		case <-time.After(s.sleep):
		case <-ctx.Done():
			return nil, "", ctx.Err()
		}
	}
	if s.createAnalysisErr != nil {
		return nil, "", s.createAnalysisErr
	}
	return json.RawMessage(`{"status":"` + s.createAnalysisStatus + `"}`), s.createAnalysisStatus, nil
}

func (s *stubUpstream) GetChanges(ctx context.Context, aoiID string, startYear, endYear int) (json.RawMessage, error) {
	atomic.AddInt32(&s.changesCalls, 1)
	if s.getChangesErr != nil {
		return nil, s.getChangesErr
	}
	return json.RawMessage(`{"type":"FeatureCollection","features":[]}`), nil
}

func (s *stubUpstream) GenerateReport(ctx context.Context, aoiID string, startYear, endYear int) ([]byte, error) {
	atomic.AddInt32(&s.reportCalls, 1)
	if s.generateReportErr != nil {
		return nil, s.generateReportErr
	}
	return []byte("%PDF-1.4 fake"), nil
}

func newTestExecutor(t *testing.T, up *stubUpstream) (*Executor, *Store) {
	t.Helper()
	store, err := NewStore(t.TempDir())
	if err != nil {
		t.Fatalf("NewStore() error = %v", err)
	}
	return NewExecutor(store, up, time.Second), store
}

func TestExecutorHappyPathCompletesWithFullBundle(t *testing.T) {
	up := &stubUpstream{createAnalysisStatus: "Complete"}
	exec, store := newTestExecutor(t, up)

	job := store.Create(Request{AoiID: "RU_TVER_01", StartYear: 2019, EndYear: 2024}, "hash-1")
	exec.Run(context.Background(), job.ID)

	got, _ := store.Get(job.ID)
	if got.Status != StatusComplete {
		t.Errorf("Status = %q, want complete", got.Status)
	}
	if got.Progress != 100 {
		t.Errorf("Progress = %d, want 100", got.Progress)
	}
	if got.Bundle == nil || got.Bundle.SummaryJSON == nil || got.Bundle.ChangesJSON == nil {
		t.Fatalf("Bundle incomplete: %+v", got.Bundle)
	}
	if !got.HasReport {
		t.Error("HasReport = false, want true")
	}
	if up.changesCalls != 1 || up.reportCalls != 1 {
		t.Errorf("changesCalls=%d reportCalls=%d, want 1 and 1", up.changesCalls, up.reportCalls)
	}
}

func TestExecutorPartialStatusStillPopulatesBundle(t *testing.T) {
	up := &stubUpstream{createAnalysisStatus: "Partial"}
	exec, store := newTestExecutor(t, up)

	job := store.Create(Request{AoiID: "RU_TVER_01", StartYear: 2019, EndYear: 2024}, "hash-1")
	exec.Run(context.Background(), job.ID)

	got, _ := store.Get(job.ID)
	if got.Status != StatusPartial {
		t.Errorf("Status = %q, want partial", got.Status)
	}
	if got.Bundle == nil || got.Bundle.SummaryJSON == nil {
		t.Error("Bundle not populated for a partial result")
	}
}

func TestExecutorCreateAnalysisErrorStopsPipeline(t *testing.T) {
	up := &stubUpstream{createAnalysisErr: errors.New("upstream: status 400: bad request")}
	exec, store := newTestExecutor(t, up)

	job := store.Create(Request{AoiID: "RU_TVER_01", StartYear: 2019, EndYear: 2024}, "hash-1")
	exec.Run(context.Background(), job.ID)

	got, _ := store.Get(job.ID)
	if got.Status != StatusFailed {
		t.Errorf("Status = %q, want failed", got.Status)
	}
	if up.changesCalls != 0 || up.reportCalls != 0 {
		t.Errorf("changesCalls=%d reportCalls=%d, want 0 and 0 (should stop after CreateAnalysis failure)", up.changesCalls, up.reportCalls)
	}
}

func TestExecutorTimeoutYieldsFailed(t *testing.T) {
	up := &stubUpstream{createAnalysisStatus: "Complete", sleep: 100 * time.Millisecond}
	store, err := NewStore(t.TempDir())
	if err != nil {
		t.Fatalf("NewStore() error = %v", err)
	}
	exec := NewExecutor(store, up, 10*time.Millisecond)

	job := store.Create(Request{AoiID: "RU_TVER_01", StartYear: 2019, EndYear: 2024}, "hash-1")
	exec.Run(context.Background(), job.ID)

	got, _ := store.Get(job.ID)
	if got.Status != StatusFailed {
		t.Errorf("Status = %q, want failed", got.Status)
	}
	if got.ErrorMessage == "" {
		t.Error("ErrorMessage empty, want a timeout-flavored message")
	}
}

func TestExecutorPolygonOnlySkipsChangesAndReport(t *testing.T) {
	up := &stubUpstream{createAnalysisStatus: "Complete"}
	exec, store := newTestExecutor(t, up)

	job := store.Create(Request{PolygonGeoJSON: `{"type":"Polygon","coordinates":[]}`, StartYear: 2019, EndYear: 2024}, "hash-1")
	exec.Run(context.Background(), job.ID)

	got, _ := store.Get(job.ID)
	if got.Status != StatusComplete {
		t.Errorf("Status = %q, want complete", got.Status)
	}
	if got.Progress != 100 {
		t.Errorf("Progress = %d, want 100", got.Progress)
	}
	if up.changesCalls != 0 || up.reportCalls != 0 {
		t.Errorf("changesCalls=%d reportCalls=%d, want 0 and 0 for a polygon-only job", up.changesCalls, up.reportCalls)
	}
	if len(got.Warnings) == 0 {
		t.Error("Warnings empty, want a warning explaining changes/report are unavailable")
	}
}

func TestExecutorUnrecognizedUpstreamStatusFails(t *testing.T) {
	up := &stubUpstream{createAnalysisStatus: "SomethingNew"}
	exec, store := newTestExecutor(t, up)

	job := store.Create(Request{AoiID: "RU_TVER_01", StartYear: 2019, EndYear: 2024}, "hash-1")
	exec.Run(context.Background(), job.ID)

	got, _ := store.Get(job.ID)
	if got.Status != StatusFailed {
		t.Errorf("Status = %q, want failed for an unrecognized upstream status", got.Status)
	}
	if up.changesCalls != 0 {
		t.Error("GetChanges was called despite an unrecognized upstream status")
	}
}
