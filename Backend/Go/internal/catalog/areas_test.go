package catalog

import (
	"os"
	"path/filepath"
	"testing"
)

const fixtureAreasCSV = "aoi_id,name,region,analysis_start_year,analysis_end_year,area_ha,selection_role,project_status,stand_age,dominant_species,site_class,bbox_west,bbox_south,bbox_east,bbox_north,baseline_id\n" +
	"RU_TVER_01,Tver control,Tver,2019,2024,1750.47,control,research,,,," +
	"32.91,56.59,32.974,56.63,HIST-AGB-2015-2019-v1\n"

const fixtureAreasGeoJSON = `{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "id": "RU_TVER_01",
      "properties": {"aoi_id": "RU_TVER_01", "name": "Tver control"},
      "geometry": {"type": "Polygon", "coordinates": [[[32.91,56.59],[32.974,56.59],[32.974,56.63],[32.91,56.63],[32.91,56.59]]]}
    }
  ]
}`

func writeFixtureDataset(t *testing.T) string {
	t.Helper()
	dir := t.TempDir()
	if err := os.WriteFile(filepath.Join(dir, "areas.csv"), append(utf8BOM, fixtureAreasCSV...), 0o644); err != nil {
		t.Fatalf("write areas.csv: %v", err)
	}
	if err := os.WriteFile(filepath.Join(dir, "areas.geojson"), []byte(fixtureAreasGeoJSON), 0o644); err != nil {
		t.Fatalf("write areas.geojson: %v", err)
	}
	return dir
}

func TestLoadAreas(t *testing.T) {
	dir := writeFixtureDataset(t)

	areas, err := LoadAreas(dir)
	if err != nil {
		t.Fatalf("LoadAreas() error = %v", err)
	}
	if len(areas) != 1 {
		t.Fatalf("len(areas) = %d, want 1", len(areas))
	}
	area := areas[0]
	if area.ID != "RU_TVER_01" || area.AnalysisStartYear != 2019 || area.AnalysisEndYear != 2024 {
		t.Errorf("unexpected area: %+v", area)
	}
	if area.AreaHectares != 1750.47 {
		t.Errorf("AreaHectares = %v, want 1750.47", area.AreaHectares)
	}
}

func TestFindArea(t *testing.T) {
	dir := writeFixtureDataset(t)
	areas, err := LoadAreas(dir)
	if err != nil {
		t.Fatalf("LoadAreas() error = %v", err)
	}

	if _, ok := FindArea(areas, "RU_TVER_01"); !ok {
		t.Error("FindArea(RU_TVER_01) not found")
	}
	if _, ok := FindArea(areas, "NOPE"); ok {
		t.Error("FindArea(NOPE) unexpectedly found")
	}
}

func TestLoadAreaFeature(t *testing.T) {
	dir := writeFixtureDataset(t)

	feature, err := LoadAreaFeature(dir, "RU_TVER_01")
	if err != nil {
		t.Fatalf("LoadAreaFeature() error = %v", err)
	}
	if len(feature) == 0 {
		t.Fatal("LoadAreaFeature() returned empty raw message")
	}

	if _, err := LoadAreaFeature(dir, "NOPE"); err == nil {
		t.Error("LoadAreaFeature(NOPE) error = nil, want error")
	}
}
