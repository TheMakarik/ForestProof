package verify

import (
	"context"
	"crypto/sha256"
	"encoding/hex"
	"os"
	"path/filepath"
	"testing"

	"forestproof-gateway/internal/catalog"
)

func writeFile(t *testing.T, dir, name string, content []byte) {
	t.Helper()
	if err := os.WriteFile(filepath.Join(dir, name), content, 0o644); err != nil {
		t.Fatalf("WriteFile(%s): %v", name, err)
	}
}

func sha256Hex(data []byte) string {
	sum := sha256.Sum256(data)
	return hex.EncodeToString(sum[:])
}

func TestScannerRun(t *testing.T) {
	dir := t.TempDir()

	goodContent := []byte("this file matches its recorded hash")
	writeFile(t, dir, "good.tif", goodContent)
	writeFile(t, dir, "bad.tif", []byte("this file does not match"))
	// "missing.tif" intentionally not written.

	entries := []catalog.FileEntry{
		{RelativePath: "good.tif", Sha256Recorded: sha256Hex(goodContent)},
		{RelativePath: "bad.tif", Sha256Recorded: sha256Hex([]byte("something else entirely"))},
		{RelativePath: "missing.tif", Sha256Recorded: "deadbeef"},
	}

	scanner := NewScanner()

	// Before Run, everything is pending.
	if got := scanner.Result("good.tif").Status; got != StatusPending {
		t.Errorf("before Run: Result(good.tif).Status = %q, want pending", got)
	}

	scanner.Run(context.Background(), dir, entries)

	if got := scanner.Result("good.tif").Status; got != StatusVerified {
		t.Errorf("Result(good.tif).Status = %q, want verified", got)
	}
	if got := scanner.Result("bad.tif").Status; got != StatusMismatch {
		t.Errorf("Result(bad.tif).Status = %q, want mismatch", got)
	}
	if got := scanner.Result("missing.tif").Status; got != StatusMissing {
		t.Errorf("Result(missing.tif).Status = %q, want missing", got)
	}
	if got := scanner.Result("never-scanned.tif").Status; got != StatusPending {
		t.Errorf("Result(never-scanned.tif).Status = %q, want pending", got)
	}
}

func TestScannerRunRespectsCancellation(t *testing.T) {
	dir := t.TempDir()
	writeFile(t, dir, "a.tif", []byte("a"))

	ctx, cancel := context.WithCancel(context.Background())
	cancel() // already canceled before Run starts

	scanner := NewScanner()
	scanner.Run(ctx, dir, []catalog.FileEntry{{RelativePath: "a.tif", Sha256Recorded: "x"}})

	if got := scanner.Result("a.tif").Status; got != StatusPending {
		t.Errorf("Result(a.tif).Status = %q, want pending (scan should have been skipped)", got)
	}
}
