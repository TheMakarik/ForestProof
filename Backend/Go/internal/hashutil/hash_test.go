package hashutil

import (
	"crypto/sha256"
	"encoding/hex"
	"os"
	"path/filepath"
	"testing"
)

func TestFileSHA256(t *testing.T) {
	dir := t.TempDir()
	path := filepath.Join(dir, "sample.bin")
	data := []byte("forestproof sample bytes")
	if err := os.WriteFile(path, data, 0o644); err != nil {
		t.Fatalf("WriteFile: %v", err)
	}

	got, err := FileSHA256(path)
	if err != nil {
		t.Fatalf("FileSHA256() error = %v", err)
	}

	sum := sha256.Sum256(data)
	want := hex.EncodeToString(sum[:])
	if got != want {
		t.Errorf("FileSHA256() = %q, want %q", got, want)
	}
}

func TestFileSHA256MissingFile(t *testing.T) {
	if _, err := FileSHA256(filepath.Join(t.TempDir(), "missing.bin")); err == nil {
		t.Fatal("FileSHA256() error = nil, want error for missing file")
	}
}

func TestStringSHA256Deterministic(t *testing.T) {
	a := StringSHA256("a", "b", "c")
	b := StringSHA256("a", "b", "c")
	if a != b {
		t.Errorf("StringSHA256 not deterministic: %q != %q", a, b)
	}
}

func TestStringSHA256SeparatorAvoidsCollision(t *testing.T) {
	// Without an unambiguous separator, ("ab", "") and ("a", "b") would
	// naively concatenate to the same string.
	got1 := StringSHA256("ab", "")
	got2 := StringSHA256("a", "b")
	if got1 == got2 {
		t.Error("StringSHA256(\"ab\", \"\") == StringSHA256(\"a\", \"b\"), want distinct hashes")
	}
}
