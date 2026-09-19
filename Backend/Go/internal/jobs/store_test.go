package jobs

import (
	"bytes"
	"encoding/json"
	"os"
	"path/filepath"
	"sync"
	"testing"
)

func TestStoreCreateAndFindByHash(t *testing.T) {
	store, err := NewStore(t.TempDir())
	if err != nil {
		t.Fatalf("NewStore() error = %v", err)
	}

	req := Request{AoiID: "RU_TVER_01", StartYear: 2019, EndYear: 2024}
	created := store.Create(req, "hash-1")

	found, ok := store.FindByHash("hash-1")
	if !ok {
		t.Fatal("FindByHash() ok = false, want true")
	}
	if found.ID != created.ID {
		t.Errorf("FindByHash().ID = %q, want %q", found.ID, created.ID)
	}

	if _, ok := store.FindByHash("does-not-exist"); ok {
		t.Error("FindByHash(does-not-exist) ok = true, want false")
	}
}

func TestStoreUpdateConcurrentSafe(t *testing.T) {
	store, err := NewStore(t.TempDir())
	if err != nil {
		t.Fatalf("NewStore() error = %v", err)
	}
	job := store.Create(Request{AoiID: "RU_TVER_01", StartYear: 2019, EndYear: 2024}, "hash-1")

	var wg sync.WaitGroup
	for i := 0; i < 50; i++ {
		wg.Add(1)
		go func(n int) {
			defer wg.Done()
			_ = store.Update(job.ID, func(j *Job) {
				j.Progress = n
			})
		}(i)
	}
	wg.Wait()

	got, ok := store.Get(job.ID)
	if !ok {
		t.Fatal("Get() ok = false after concurrent updates")
	}
	if got.Progress < 0 || got.Progress > 49 {
		t.Errorf("Progress = %d, want a value written by one of the updates", got.Progress)
	}
}

func TestStoreUpdateUnknownID(t *testing.T) {
	store, err := NewStore(t.TempDir())
	if err != nil {
		t.Fatalf("NewStore() error = %v", err)
	}
	if err := store.Update("does-not-exist", func(j *Job) {}); err == nil {
		t.Error("Update(does-not-exist) error = nil, want error")
	}
}

func TestStorePersistsAndReloads(t *testing.T) {
	dir := t.TempDir()
	store, err := NewStore(dir)
	if err != nil {
		t.Fatalf("NewStore() error = %v", err)
	}

	job := store.Create(Request{AoiID: "RU_TVER_01", StartYear: 2019, EndYear: 2024}, "hash-1")
	err = store.Update(job.ID, func(j *Job) {
		j.Status = StatusComplete
		j.Bundle = &Bundle{SummaryJSON: json.RawMessage(`{"status":"Complete"}`)}
	})
	if err != nil {
		t.Fatalf("Update() error = %v", err)
	}

	reopened, err := NewStore(dir)
	if err != nil {
		t.Fatalf("NewStore() (reload) error = %v", err)
	}

	got, ok := reopened.Get(job.ID)
	if !ok {
		t.Fatal("Get() ok = false after reload")
	}
	if got.Status != StatusComplete {
		t.Errorf("Status after reload = %q, want complete", got.Status)
	}
	// The on-disk round trip re-indents the whole job document (including
	// nested raw JSON), so compare semantically rather than byte-for-byte.
	if got.Bundle == nil {
		t.Fatal("Bundle after reload = nil, want SummaryJSON preserved")
	}
	var summary struct {
		Status string `json:"status"`
	}
	if err := json.Unmarshal(got.Bundle.SummaryJSON, &summary); err != nil {
		t.Fatalf("Unmarshal SummaryJSON after reload: %v", err)
	}
	if summary.Status != "Complete" {
		t.Errorf("SummaryJSON.status after reload = %q, want Complete", summary.Status)
	}

	if _, ok := reopened.FindByHash("hash-1"); !ok {
		t.Error("FindByHash(hash-1) after reload: ok = false, want true (index rebuilt)")
	}
}

func TestStoreRestartRecoversInterruptedJobToFailed(t *testing.T) {
	dir := t.TempDir()
	store, err := NewStore(dir)
	if err != nil {
		t.Fatalf("NewStore() error = %v", err)
	}
	job := store.Create(Request{AoiID: "RU_TVER_01", StartYear: 2019, EndYear: 2024}, "hash-1")
	if err := store.Update(job.ID, func(j *Job) { j.Status = StatusRunning }); err != nil {
		t.Fatalf("Update() error = %v", err)
	}

	reopened, err := NewStore(dir)
	if err != nil {
		t.Fatalf("NewStore() (reload) error = %v", err)
	}

	got, ok := reopened.Get(job.ID)
	if !ok {
		t.Fatal("Get() ok = false after reload")
	}
	if got.Status != StatusFailed {
		t.Errorf("Status after reload of a running job = %q, want failed", got.Status)
	}
	if got.ErrorMessage == "" {
		t.Error("ErrorMessage empty after recovering an interrupted job")
	}
}

func TestStoreReportPersistedSeparatelyFromJSON(t *testing.T) {
	dir := t.TempDir()
	store, err := NewStore(dir)
	if err != nil {
		t.Fatalf("NewStore() error = %v", err)
	}
	job := store.Create(Request{AoiID: "RU_TVER_01", StartYear: 2019, EndYear: 2024}, "hash-1")

	pdfBytes := []byte("%PDF-1.4 fake")
	if err := store.Update(job.ID, func(j *Job) {
		j.Bundle = &Bundle{ReportPDF: pdfBytes}
	}); err != nil {
		t.Fatalf("Update() error = %v", err)
	}

	reportPath := store.ReportPath(job.ID)
	got, err := os.ReadFile(reportPath)
	if err != nil {
		t.Fatalf("ReadFile(%s): %v", reportPath, err)
	}
	if string(got) != string(pdfBytes) {
		t.Errorf("report bytes = %q, want %q", got, pdfBytes)
	}

	jsonPath := filepath.Join(dir, job.ID+".json")
	jsonBytes, err := os.ReadFile(jsonPath)
	if err != nil {
		t.Fatalf("ReadFile(%s): %v", jsonPath, err)
	}
	if bytes.Contains(jsonBytes, pdfBytes) {
		t.Error("job JSON file embeds the PDF bytes, want them kept out of the JSON metadata file")
	}

	updated, ok := store.Get(job.ID)
	if !ok || !updated.HasReport {
		t.Error("HasReport = false after a report was attached")
	}
}
