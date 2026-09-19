package jobs

import (
	"crypto/rand"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"
)

// Store is the gateway's in-process job registry: an in-memory index for
// fast lookups, backed by a per-job JSON file on disk so idempotent lookups
// and demo results survive a process restart (WORK_SPLIT's "no Redis,
// reproducible local cache" requirement).
type Store struct {
	mu       sync.RWMutex
	byID     map[string]*Job
	byHash   map[string]string // inputHash+method profile -> job id
	cacheDir string
}

// NewStore creates cacheDir if it doesn't exist and rebuilds its in-memory
// index from any *.json files already there. A job restored mid-flight
// (status validating/running — meaning the previous process died before it
// finished) is rewritten to failed, so it never permanently blocks
// idempotent dedup for its hash.
func NewStore(cacheDir string) (*Store, error) {
	if err := os.MkdirAll(cacheDir, 0o755); err != nil {
		return nil, fmt.Errorf("jobs: create cache dir %s: %w", cacheDir, err)
	}

	s := &Store{
		byID:     make(map[string]*Job),
		byHash:   make(map[string]string),
		cacheDir: cacheDir,
	}

	entries, err := os.ReadDir(cacheDir)
	if err != nil {
		return nil, fmt.Errorf("jobs: read cache dir %s: %w", cacheDir, err)
	}

	for _, entry := range entries {
		if entry.IsDir() || !strings.HasSuffix(entry.Name(), ".json") {
			continue
		}
		job, err := loadJobFile(filepath.Join(cacheDir, entry.Name()))
		if err != nil {
			continue // a corrupt/partial cache file shouldn't block startup
		}
		if job.Status == StatusValidating || job.Status == StatusRunning {
			job.Status = StatusFailed
			job.ErrorMessage = "interrupted by process restart"
			job.UpdatedAt = time.Now()
		}
		s.byID[job.ID] = job
		s.byHash[job.InputHash] = job.ID
	}

	return s, nil
}

func loadJobFile(path string) (*Job, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, err
	}
	var job Job
	if err := json.Unmarshal(data, &job); err != nil {
		return nil, err
	}
	return &job, nil
}

// FindByHash returns the job previously created with the given input hash,
// if any — this is what makes POST /analyses idempotent.
func (s *Store) FindByHash(hash string) (Job, bool) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	id, ok := s.byHash[hash]
	if !ok {
		return Job{}, false
	}
	return copyJob(s.byID[id]), true
}

// Create registers a new job in state "validating" and persists it.
func (s *Store) Create(req Request, hash string) Job {
	now := time.Now()
	job := &Job{
		ID:        newJobID(),
		InputHash: hash,
		Status:    StatusValidating,
		Phase:     "validating",
		Request:   req,
		CreatedAt: now,
		UpdatedAt: now,
	}

	s.mu.Lock()
	s.byID[job.ID] = job
	s.byHash[hash] = job.ID
	s.mu.Unlock()

	s.persist(job)
	return copyJob(job)
}

// Get returns a copy of the job with the given id.
func (s *Store) Get(id string) (Job, bool) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	job, ok := s.byID[id]
	if !ok {
		return Job{}, false
	}
	return copyJob(job), true
}

// Update locks the store, applies mutate to the job in place, bumps
// UpdatedAt, persists the result, and returns an error only if id is
// unknown.
func (s *Store) Update(id string, mutate func(*Job)) error {
	s.mu.Lock()
	job, ok := s.byID[id]
	if !ok {
		s.mu.Unlock()
		return fmt.Errorf("jobs: unknown job %q", id)
	}
	mutate(job)
	if job.Bundle != nil && len(job.Bundle.ReportPDF) > 0 {
		// Set on the live job (not a later copy) so Get() reflects it
		// immediately, without waiting on the disk write below.
		job.HasReport = true
	}
	job.UpdatedAt = time.Now()
	snapshot := copyJob(job)
	s.mu.Unlock()

	s.persist(&snapshot)
	return nil
}

// ReportPath returns the on-disk path a job's PDF report is (or would be)
// persisted at.
func (s *Store) ReportPath(id string) string {
	return filepath.Join(s.cacheDir, id+".pdf")
}

// persist writes job's metadata (including inline summary/changes JSON) to
// its cache file, and — if a report is attached — writes the PDF bytes to
// their own file so the small JSON files stay fast to scan on restart.
// Bundle.ReportPDF is tagged json:"-", so it's never embedded in the JSON
// file regardless. job.HasReport must already be set correctly by the
// caller (Update) before this runs.
func (s *Store) persist(job *Job) {
	if job.Bundle != nil && len(job.Bundle.ReportPDF) > 0 {
		_ = os.WriteFile(s.ReportPath(job.ID), job.Bundle.ReportPDF, 0o644)
	}

	data, err := json.MarshalIndent(job, "", "  ")
	if err != nil {
		return
	}
	_ = os.WriteFile(filepath.Join(s.cacheDir, job.ID+".json"), data, 0o644)
}

func copyJob(job *Job) Job {
	out := *job
	if job.Warnings != nil {
		out.Warnings = append([]string(nil), job.Warnings...)
	}
	if job.Bundle != nil {
		bundleCopy := *job.Bundle
		out.Bundle = &bundleCopy
	}
	return out
}

func newJobID() string {
	buf := make([]byte, 16)
	if _, err := rand.Read(buf); err != nil {
		// crypto/rand failing is effectively unrecoverable; fall back to a
		// time-based id rather than panicking the whole gateway.
		return fmt.Sprintf("%x", time.Now().UnixNano())
	}
	return hex.EncodeToString(buf)
}
