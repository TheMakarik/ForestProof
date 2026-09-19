// Package verify computes real SHA-256 digests of the dataset's cataloged
// files and compares them against the recorded checksums in
// file_catalog.csv — the actual verification step the C# backend's
// SourceCatalogService exposes but never calls from the analysis pipeline.
package verify

import (
	"context"
	"path/filepath"
	"sync"
	"time"

	"forestproof-gateway/internal/catalog"
	"forestproof-gateway/internal/hashutil"
)

// Status is the outcome of checking one cataloged file against its
// recorded SHA-256.
type Status string

const (
	// StatusPending means the file hasn't been checked yet (the scanner
	// hasn't reached it, or hasn't been run at all).
	StatusPending Status = "pending"
	// StatusVerified means the actual file hash matches the catalog.
	StatusVerified Status = "verified"
	// StatusMismatch means the file exists but its hash differs from the
	// catalog's recorded value.
	StatusMismatch Status = "mismatch"
	// StatusMissing means the catalog references a file that isn't on
	// disk.
	StatusMissing Status = "missing"
)

// Result is one file's verification outcome.
type Result struct {
	Status       Status
	ActualSHA256 string
	CheckedAt    time.Time
}

// maxConcurrentHashes bounds how many files are hashed at once, so a large
// dataset scan doesn't saturate disk I/O.
const maxConcurrentHashes = 4

// Scanner holds the in-process, concurrency-safe verification results for
// every file it has been asked to check.
type Scanner struct {
	mu      sync.RWMutex
	results map[string]Result
}

// NewScanner returns an empty Scanner; every path starts at StatusPending
// until Run processes it.
func NewScanner() *Scanner {
	return &Scanner{results: make(map[string]Result)}
}

// Run hashes every entry under dataRoot and records the outcome, honoring
// ctx cancellation between files. It's a blocking call by design — callers
// that want it non-blocking at startup run it via `go scanner.Run(...)`;
// tests call it directly for deterministic, synchronous results.
func (s *Scanner) Run(ctx context.Context, dataRoot string, entries []catalog.FileEntry) {
	sem := make(chan struct{}, maxConcurrentHashes)
	var wg sync.WaitGroup

	for _, entry := range entries {
		if ctx.Err() != nil {
			return
		}

		wg.Add(1)
		sem <- struct{}{}
		go func(entry catalog.FileEntry) {
			defer wg.Done()
			defer func() { <-sem }()
			s.verifyOne(dataRoot, entry)
		}(entry)
	}

	wg.Wait()
}

func (s *Scanner) verifyOne(dataRoot string, entry catalog.FileEntry) {
	path := filepath.Join(dataRoot, entry.RelativePath)

	actual, err := hashutil.FileSHA256(path)
	now := time.Now()

	var result Result
	switch {
	case err != nil:
		result = Result{Status: StatusMissing, CheckedAt: now}
	case actual == entry.Sha256Recorded:
		result = Result{Status: StatusVerified, ActualSHA256: actual, CheckedAt: now}
	default:
		result = Result{Status: StatusMismatch, ActualSHA256: actual, CheckedAt: now}
	}

	s.mu.Lock()
	s.results[entry.RelativePath] = result
	s.mu.Unlock()
}

// Result returns the verification outcome for relativePath, or a zero-value
// {StatusPending} Result if it hasn't been checked (yet, or at all).
func (s *Scanner) Result(relativePath string) Result {
	s.mu.RLock()
	defer s.mu.RUnlock()

	if r, ok := s.results[relativePath]; ok {
		return r
	}
	return Result{Status: StatusPending}
}
