// Package catalog reads the ForestProof dataset's provenance and registry
// CSV/GeoJSON files directly off disk (the same files the C# backend reads),
// so the gateway can serve its own source catalog, SHA-256 verification and
// map-layer resolution without depending on the C# API for anything but the
// actual computation.
package catalog

import (
	"bytes"
	"encoding/csv"
	"fmt"
	"os"
	"strconv"
)

// utf8BOM is the 3-byte UTF-8 byte-order mark that every Dataset CSV starts
// with. Go's encoding/csv, unlike the C# side's CsvHelper (which strips a
// BOM automatically via its StreamReader), does NOT strip it — left in
// place, it silently corrupts the first header cell (the BOM's three bytes
// end up prepended to the first column name), breaking every lookup by
// that column's name.
var utf8BOM = []byte{0xEF, 0xBB, 0xBF}

// Table is a parsed CSV file: a header-name-to-column-index map plus the
// data rows, with lookups that tolerate a short trailing row the way the
// C# side's CsvDocument does (missing cells read back as "").
type Table struct {
	header map[string]int
	rows   [][]string
}

// ReadCSV parses the CSV file at path, stripping a leading UTF-8 BOM if
// present and tolerating rows shorter than the header (a short row is
// padded with empty strings on lookup rather than causing a parse error).
func ReadCSV(path string) (*Table, error) {
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("catalog: read %s: %w", path, err)
	}

	data = stripBOM(data)

	reader := csv.NewReader(bytes.NewReader(data))
	reader.FieldsPerRecord = -1 // tolerate short/long trailing rows

	records, err := reader.ReadAll()
	if err != nil {
		return nil, fmt.Errorf("catalog: parse %s: %w", path, err)
	}
	if len(records) == 0 {
		return &Table{header: map[string]int{}}, nil
	}

	header := make(map[string]int, len(records[0]))
	for i, name := range records[0] {
		header[name] = i
	}

	return &Table{header: header, rows: records[1:]}, nil
}

func stripBOM(data []byte) []byte {
	if len(data) >= 3 && data[0] == utf8BOM[0] && data[1] == utf8BOM[1] && data[2] == utf8BOM[2] {
		return data[3:]
	}
	return data
}

// Rows returns every data row (excluding the header).
func (t *Table) Rows() [][]string { return t.rows }

// String returns the value of column col in row, or "" if the row is
// shorter than the column's index. It errors only if col isn't a known
// header.
func (t *Table) String(row []string, col string) (string, error) {
	idx, ok := t.header[col]
	if !ok {
		return "", fmt.Errorf("catalog: unknown column %q", col)
	}
	if idx >= len(row) {
		return "", nil
	}
	return row[idx], nil
}

// Int parses column col of row as an integer.
func (t *Table) Int(row []string, col string) (int, error) {
	s, err := t.String(row, col)
	if err != nil {
		return 0, err
	}
	if s == "" {
		return 0, nil
	}
	n, err := strconv.Atoi(s)
	if err != nil {
		return 0, fmt.Errorf("catalog: column %q value %q is not an int: %w", col, s, err)
	}
	return n, nil
}

// Int64 parses column col of row as an int64 (used for byte sizes).
func (t *Table) Int64(row []string, col string) (int64, error) {
	s, err := t.String(row, col)
	if err != nil {
		return 0, err
	}
	if s == "" {
		return 0, nil
	}
	n, err := strconv.ParseInt(s, 10, 64)
	if err != nil {
		return 0, fmt.Errorf("catalog: column %q value %q is not an int64: %w", col, s, err)
	}
	return n, nil
}

// Float parses column col of row as a float64.
func (t *Table) Float(row []string, col string) (float64, error) {
	s, err := t.String(row, col)
	if err != nil {
		return 0, err
	}
	if s == "" {
		return 0, nil
	}
	n, err := strconv.ParseFloat(s, 64)
	if err != nil {
		return 0, fmt.Errorf("catalog: column %q value %q is not a float: %w", col, s, err)
	}
	return n, nil
}
