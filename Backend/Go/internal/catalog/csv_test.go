package catalog

import (
	"os"
	"path/filepath"
	"testing"
)

func writeCSV(t *testing.T, dir, name string, withBOM bool, content string) string {
	t.Helper()
	path := filepath.Join(dir, name)
	data := []byte(content)
	if withBOM {
		data = append(utf8BOM, data...)
	}
	if err := os.WriteFile(path, data, 0o644); err != nil {
		t.Fatalf("WriteFile: %v", err)
	}
	return path
}

func TestReadCSVStripsBOM(t *testing.T) {
	dir := t.TempDir()
	path := writeCSV(t, dir, "sample.csv", true, "source_id,version\nCCI_V7,7.0\n")

	table, err := ReadCSV(path)
	if err != nil {
		t.Fatalf("ReadCSV() error = %v", err)
	}
	if len(table.Rows()) != 1 {
		t.Fatalf("Rows() len = %d, want 1", len(table.Rows()))
	}

	got, err := table.String(table.Rows()[0], "source_id")
	if err != nil {
		t.Fatalf("String(source_id) error = %v (BOM likely not stripped from header)", err)
	}
	if got != "CCI_V7" {
		t.Errorf("String(source_id) = %q, want CCI_V7", got)
	}
}

func TestReadCSVToleratesShortRow(t *testing.T) {
	dir := t.TempDir()
	// Second row is missing the trailing "version" cell entirely.
	path := writeCSV(t, dir, "sample.csv", false, "source_id,version\nCCI_V7,7.0\nGFC_2025\n")

	table, err := ReadCSV(path)
	if err != nil {
		t.Fatalf("ReadCSV() error = %v", err)
	}
	if len(table.Rows()) != 2 {
		t.Fatalf("Rows() len = %d, want 2", len(table.Rows()))
	}

	got, err := table.String(table.Rows()[1], "version")
	if err != nil {
		t.Fatalf("String(version) error = %v", err)
	}
	if got != "" {
		t.Errorf("String(version) on short row = %q, want empty string", got)
	}
}

func TestReadCSVUnknownColumn(t *testing.T) {
	dir := t.TempDir()
	path := writeCSV(t, dir, "sample.csv", false, "source_id\nCCI_V7\n")

	table, err := ReadCSV(path)
	if err != nil {
		t.Fatalf("ReadCSV() error = %v", err)
	}
	if _, err := table.String(table.Rows()[0], "does_not_exist"); err == nil {
		t.Fatal("String() with unknown column: error = nil, want error")
	}
}
