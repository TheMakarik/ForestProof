package catalog

import (
	"os"
	"path/filepath"
	"testing"
)

func TestLoadSources(t *testing.T) {
	dir := t.TempDir()
	content := "source_id,product,version,kind,primary_url,doi,license_url,redistribution_basis,required_attribution,access_date,limitations\n" +
		"CCI_V7,\"ESA CCI Biomass, with a comma\",7.0,external,https://example.org,,https://example.org/license,basis,attribution,2026-09-16,limits\n"
	if err := os.WriteFile(filepath.Join(dir, "sources.csv"), append(utf8BOM, content...), 0o644); err != nil {
		t.Fatalf("write sources.csv: %v", err)
	}

	sources, err := LoadSources(dir)
	if err != nil {
		t.Fatalf("LoadSources() error = %v", err)
	}
	if len(sources) != 1 {
		t.Fatalf("len(sources) = %d, want 1", len(sources))
	}
	if sources[0].SourceID != "CCI_V7" {
		t.Errorf("SourceID = %q, want CCI_V7", sources[0].SourceID)
	}
	if sources[0].Product != "ESA CCI Biomass, with a comma" {
		t.Errorf("Product = %q, want embedded-comma value preserved by quoting", sources[0].Product)
	}
}

func TestLoadFileCatalogSplitsSourceIDsOnSemicolon(t *testing.T) {
	dir := t.TempDir()
	header := "relative_path,aoi_id,source_ids,product_version,observation_or_scenario_period,data_kind,bands_or_fields,units,scale_and_offset,dtype,shape_rows_cols_bands,crs,pixel_size_native_units,nodata,processing,original_urls_or_inputs,license_source_ids,retrieved_or_created_date,size_bytes,sha256\n"
	row := "RU_TVER_01/CCI_Biomass_2019.tif,RU_TVER_01,CCI_V7;IPCC_FOREST_2006;CASE_RULES_V1,7.0,2019,external,bands,units,scale,dtype,shape,EPSG:4326,pixel,nodata,processing,urls,licenses,2026-09-16,10274,deadbeef\n"
	if err := os.WriteFile(filepath.Join(dir, "file_catalog.csv"), []byte(header+row), 0o644); err != nil {
		t.Fatalf("write file_catalog.csv: %v", err)
	}

	entries, err := LoadFileCatalog(dir)
	if err != nil {
		t.Fatalf("LoadFileCatalog() error = %v", err)
	}
	if len(entries) != 1 {
		t.Fatalf("len(entries) = %d, want 1", len(entries))
	}
	want := []string{"CCI_V7", "IPCC_FOREST_2006", "CASE_RULES_V1"}
	got := entries[0].SourceIDs
	if len(got) != len(want) {
		t.Fatalf("SourceIDs = %v, want %v", got, want)
	}
	for i := range want {
		if got[i] != want[i] {
			t.Errorf("SourceIDs[%d] = %q, want %q", i, got[i], want[i])
		}
	}
	if entries[0].SizeBytes != 10274 {
		t.Errorf("SizeBytes = %d, want 10274", entries[0].SizeBytes)
	}
}
