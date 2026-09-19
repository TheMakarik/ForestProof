package stac

import (
	"os"
	"path/filepath"
	"testing"
)

func TestCachePathDeterministic(t *testing.T) {
	cache := NewCache("/tmp/stac-cache")
	a := cache.Path("RU_TVER_01", "S2A_ITEM", "visual", ".tif")
	b := cache.Path("RU_TVER_01", "S2A_ITEM", "visual", ".tif")
	if a != b {
		t.Errorf("Path() not deterministic: %q != %q", a, b)
	}
}

func TestCacheHas(t *testing.T) {
	dir := t.TempDir()
	cache := NewCache(dir)

	if cache.Has("RU_TVER_01", "S2A_ITEM", "visual", ".tif") {
		t.Error("Has() = true before file exists")
	}

	path := cache.Path("RU_TVER_01", "S2A_ITEM", "visual", ".tif")
	if err := os.MkdirAll(filepath.Dir(path), 0o755); err != nil {
		t.Fatalf("MkdirAll: %v", err)
	}
	if err := os.WriteFile(path, []byte("data"), 0o644); err != nil {
		t.Fatalf("WriteFile: %v", err)
	}

	if !cache.Has("RU_TVER_01", "S2A_ITEM", "visual", ".tif") {
		t.Error("Has() = false after file was created")
	}
}
