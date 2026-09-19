package catalog

import (
	"path/filepath"
	"strings"
)

// Source is one row of sources.csv — a data product ForestProof consumes.
type Source struct {
	SourceID            string
	Product             string
	Version             string
	Kind                string
	PrimaryURL          string
	LicenseURL          string
	RequiredAttribution string
	Limitations         string
	AccessDate          string
}

// FileEntry is one row of file_catalog.csv — a physical file plus the
// provenance metadata needed to verify and cite it.
type FileEntry struct {
	RelativePath   string
	AoiID          string
	SourceIDs      []string
	DataKind       string
	Sha256Recorded string
	SizeBytes      int64
}

const sourcesCSVFileName = "sources.csv"
const fileCatalogCSVFileName = "file_catalog.csv"

// LoadSources parses <dataRoot>/sources.csv.
func LoadSources(dataRoot string) ([]Source, error) {
	table, err := ReadCSV(filepath.Join(dataRoot, sourcesCSVFileName))
	if err != nil {
		return nil, err
	}

	sources := make([]Source, 0, len(table.Rows()))
	for _, row := range table.Rows() {
		var s Source
		var ferr error
		if s.SourceID, ferr = table.String(row, "source_id"); ferr != nil {
			return nil, ferr
		}
		if s.Product, ferr = table.String(row, "product"); ferr != nil {
			return nil, ferr
		}
		if s.Version, ferr = table.String(row, "version"); ferr != nil {
			return nil, ferr
		}
		if s.Kind, ferr = table.String(row, "kind"); ferr != nil {
			return nil, ferr
		}
		if s.PrimaryURL, ferr = table.String(row, "primary_url"); ferr != nil {
			return nil, ferr
		}
		if s.LicenseURL, ferr = table.String(row, "license_url"); ferr != nil {
			return nil, ferr
		}
		if s.RequiredAttribution, ferr = table.String(row, "required_attribution"); ferr != nil {
			return nil, ferr
		}
		if s.Limitations, ferr = table.String(row, "limitations"); ferr != nil {
			return nil, ferr
		}
		if s.AccessDate, ferr = table.String(row, "access_date"); ferr != nil {
			return nil, ferr
		}
		sources = append(sources, s)
	}
	return sources, nil
}

// LoadFileCatalog parses <dataRoot>/file_catalog.csv. Multi-valued cells
// (source_ids) use ";" as the in-cell separator, not ",".
func LoadFileCatalog(dataRoot string) ([]FileEntry, error) {
	table, err := ReadCSV(filepath.Join(dataRoot, fileCatalogCSVFileName))
	if err != nil {
		return nil, err
	}

	entries := make([]FileEntry, 0, len(table.Rows()))
	for _, row := range table.Rows() {
		var e FileEntry
		var ferr error
		if e.RelativePath, ferr = table.String(row, "relative_path"); ferr != nil {
			return nil, ferr
		}
		if e.AoiID, ferr = table.String(row, "aoi_id"); ferr != nil {
			return nil, ferr
		}
		if e.DataKind, ferr = table.String(row, "data_kind"); ferr != nil {
			return nil, ferr
		}
		if e.Sha256Recorded, ferr = table.String(row, "sha256"); ferr != nil {
			return nil, ferr
		}
		if e.SizeBytes, ferr = table.Int64(row, "size_bytes"); ferr != nil {
			return nil, ferr
		}

		sourceIDs, ferr := table.String(row, "source_ids")
		if ferr != nil {
			return nil, ferr
		}
		e.SourceIDs = splitNonEmpty(sourceIDs, ";")

		entries = append(entries, e)
	}
	return entries, nil
}

func splitNonEmpty(s, sep string) []string {
	if s == "" {
		return nil
	}
	parts := strings.Split(s, sep)
	out := make([]string, 0, len(parts))
	for _, p := range parts {
		p = strings.TrimSpace(p)
		if p != "" {
			out = append(out, p)
		}
	}
	return out
}
