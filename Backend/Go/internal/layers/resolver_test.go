package layers

import (
	"context"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"testing"

	"forestproof-gateway/internal/catalog"
	"forestproof-gateway/internal/stac"
)

func writeFile(t *testing.T, path string, content []byte) {
	t.Helper()
	if err := os.MkdirAll(filepath.Dir(path), 0o755); err != nil {
		t.Fatalf("MkdirAll(%s): %v", filepath.Dir(path), err)
	}
	if err := os.WriteFile(path, content, 0o644); err != nil {
		t.Fatalf("WriteFile(%s): %v", path, err)
	}
}

// buildFixtureDataset creates a minimal but structurally real Dataset/ tree
// for one AOI (RU_TVER_01) with a biomass raster, a GFC raster, a
// 2019-2020 CCI change raster, and two Sentinel-2 scenes (2019 + 2024).
func buildFixtureDataset(t *testing.T) Deps {
	t.Helper()
	dataRoot := t.TempDir()

	writeFile(t, filepath.Join(dataRoot, "areas.csv"),
		[]byte("aoi_id,name,region,analysis_start_year,analysis_end_year,area_ha,selection_role,project_status,stand_age,dominant_species,site_class,bbox_west,bbox_south,bbox_east,bbox_north,baseline_id\n"+
			"RU_TVER_01,Tver,Tver,2019,2024,1750.47,control,research,,,,32.91,56.59,32.974,56.63,HIST\n"))
	writeFile(t, filepath.Join(dataRoot, "areas.geojson"), []byte(`{
		"type":"FeatureCollection",
		"features":[{"type":"Feature","id":"RU_TVER_01","properties":{"aoi_id":"RU_TVER_01"},"geometry":{"type":"Polygon","coordinates":[]}}]
	}`))

	writeFile(t, filepath.Join(dataRoot, "RU_TVER_01", "CCI_Biomass_2019.tif"), []byte("fake-agb-2019"))
	writeFile(t, filepath.Join(dataRoot, "RU_TVER_01", "CCI_Biomass_2024.tif"), []byte("fake-agb-2024"))
	writeFile(t, filepath.Join(dataRoot, "RU_TVER_01", "GFC_2025_v1_13.tif"), []byte("fake-gfc"))
	writeFile(t, filepath.Join(dataRoot, "RU_TVER_01", "CCI_Change_2019_2020.tif"), []byte("fake-change"))
	writeFile(t, filepath.Join(dataRoot, "RU_TVER_01", "Sentinel2", "A_20190625_reflectance.tif"), []byte("fake-s2-a"))
	writeFile(t, filepath.Join(dataRoot, "RU_TVER_01", "Sentinel2", "B_20240710_reflectance.tif"), []byte("fake-s2-b"))
	// Deliberately no MODIS/ directory for RU_TVER_01, matching the real dataset.

	fileCatalogHeader := "relative_path,aoi_id,source_ids,product_version,observation_or_scenario_period,data_kind,bands_or_fields,units,scale_and_offset,dtype,shape_rows_cols_bands,crs,pixel_size_native_units,nodata,processing,original_urls_or_inputs,license_source_ids,retrieved_or_created_date,size_bytes,sha256\n"
	fileCatalogRows := "" +
		"RU_TVER_01/CCI_Biomass_2019.tif,RU_TVER_01,CCI_V7,7.0,2019,external,bands,units,scale,dtype,shape,EPSG:4326,pixel,nodata,proc,urls,lic,2026-09-16,13,deadbeef\n" +
		"RU_TVER_01/CCI_Biomass_2024.tif,RU_TVER_01,CCI_V7,7.0,2024,external,bands,units,scale,dtype,shape,EPSG:4326,pixel,nodata,proc,urls,lic,2026-09-16,13,deadbeef\n" +
		"RU_TVER_01/GFC_2025_v1_13.tif,RU_TVER_01,GFC_2025_V113,1,2025,external,bands,units,scale,dtype,shape,EPSG:4326,pixel,nodata,proc,urls,lic,2026-09-16,8,deadbeef\n" +
		"RU_TVER_01/CCI_Change_2019_2020.tif,RU_TVER_01,CCI_V7,7.0,2019-2020,external,bands,units,scale,dtype,shape,EPSG:4326,pixel,nodata,proc,urls,lic,2026-09-16,11,deadbeef\n"
	writeFile(t, filepath.Join(dataRoot, "file_catalog.csv"), []byte(fileCatalogHeader+fileCatalogRows))

	writeFile(t, filepath.Join(dataRoot, "sources.csv"),
		[]byte("source_id,product,version,kind,primary_url,doi,license_url,redistribution_basis,required_attribution,access_date,limitations\n"+
			"CCI_V7,ESA CCI Biomass,7.0,external,https://example.org,,https://example.org/cci-license,basis,attribution,2026-09-16,limits\n"))

	scenesHeader := "scene_key,aoi_id,item_id,datetime_utc,year,collection,processing_baseline,source_scene_cloud_percent,scl_4_5_6_7_fraction_crop,selection_role,reflectance_path,scl_path,metadata_file\n"
	scenesRows := "" +
		"RU_TVER_01__A,RU_TVER_01,A,2019-06-25T08:25:00Z,2019,sentinel-2-l2a,05.00,10,0.9,summer,RU_TVER_01/Sentinel2/A_20190625_reflectance.tif,RU_TVER_01/Sentinel2/A_20190625_SCL.tif,m.json\n" +
		"RU_TVER_01__B,RU_TVER_01,B,2024-07-10T08:25:00Z,2024,sentinel-2-l2a,05.00,5,0.95,summer,RU_TVER_01/Sentinel2/B_20240710_reflectance.tif,RU_TVER_01/Sentinel2/B_20240710_SCL.tif,m.json\n"
	writeFile(t, filepath.Join(dataRoot, "scenes.csv"), []byte(scenesHeader+scenesRows))

	areas, err := catalog.LoadAreas(dataRoot)
	if err != nil {
		t.Fatalf("LoadAreas() error = %v", err)
	}
	fileCatalog, err := catalog.LoadFileCatalog(dataRoot)
	if err != nil {
		t.Fatalf("LoadFileCatalog() error = %v", err)
	}
	scenes, err := catalog.LoadScenes(dataRoot)
	if err != nil {
		t.Fatalf("LoadScenes() error = %v", err)
	}
	sources, err := catalog.LoadSources(dataRoot)
	if err != nil {
		t.Fatalf("LoadSources() error = %v", err)
	}

	return Deps{DataRoot: dataRoot, Areas: areas, Sources: sources, FileCatalog: fileCatalog, Scenes: scenes}
}

func TestListLayersPopulatesLicenseFromSources(t *testing.T) {
	deps := buildFixtureDataset(t)

	infos := ListLayers(deps, "RU_TVER_01", 2019, 2024)
	var gfc *LayerInfo
	for i := range infos {
		if infos[i].Key == "gfc" {
			gfc = &infos[i]
		}
	}
	if gfc == nil {
		t.Fatal("no LayerInfo for key \"gfc\"")
	}
	if !gfc.Available {
		t.Fatalf("gfc.Available = false, want true: %+v", gfc)
	}
	if gfc.SourceID != "GFC_2025_V113" {
		t.Errorf("gfc.SourceID = %q, want GFC_2025_V113", gfc.SourceID)
	}
}

func TestListLayersAgbStartLicenseFromSources(t *testing.T) {
	deps := buildFixtureDataset(t)

	infos := ListLayers(deps, "RU_TVER_01", 2019, 2024)
	for _, info := range infos {
		if info.Key != "agb_start" {
			continue
		}
		if info.License != "https://example.org/cci-license" {
			t.Errorf("agb_start.License = %q, want https://example.org/cci-license", info.License)
		}
		return
	}
	t.Fatal("no LayerInfo for key \"agb_start\"")
}

func TestResolveCciChangeOnlyFor2019To2020(t *testing.T) {
	deps := buildFixtureDataset(t)

	if _, _, err := ResolveLayer(context.Background(), deps, "RU_TVER_01", 2019, 2020, "cci_change"); err != nil {
		t.Errorf("ResolveLayer(cci_change, 2019-2020) error = %v, want nil", err)
	}
	if _, _, err := ResolveLayer(context.Background(), deps, "RU_TVER_01", 2021, 2022, "cci_change"); err == nil {
		t.Error("ResolveLayer(cci_change, 2021-2022) error = nil, want error explaining the fixed period")
	}
}

func TestResolveModisBurnMissingForAOIWithoutMODIS(t *testing.T) {
	deps := buildFixtureDataset(t)

	if _, _, err := ResolveLayer(context.Background(), deps, "RU_TVER_01", 2019, 2024, "modis_burn"); err == nil {
		t.Error("ResolveLayer(modis_burn) error = nil, want error (no MODIS dir for RU_TVER_01)")
	}
}

func TestResolveSentinelPairLocalHitNeverTouchesStac(t *testing.T) {
	deps := buildFixtureDataset(t)
	deps.StacClient = stac.NewClient("http://unused.invalid", failingHTTPClient(t))
	deps.StacCache = stac.NewCache(t.TempDir())

	before, after, status, _, err := ResolveSentinelPair(context.Background(), deps, "RU_TVER_01", 2019, 2024)
	if err != nil {
		t.Fatalf("ResolveSentinelPair() error = %v", err)
	}
	if status != "complete" {
		t.Errorf("status = %q, want complete (local hit)", status)
	}
	if before.Path == "" || after.Path == "" {
		t.Errorf("expected local paths, got before=%+v after=%+v", before, after)
	}
}

func TestResolveSentinelPairLocalMissFallsBackToStacAndCaches(t *testing.T) {
	deps := buildFixtureDataset(t)
	// Ask for a period with no local scenes at all so SelectScenePair misses.
	deps.Scenes = nil

	assetBytes := []byte("fake-visual-band")
	var server *httptest.Server
	server = httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/search" {
			w.Header().Set("Content-Type", "application/geo+json")
			json.NewEncoder(w).Encode(map[string]any{
				"features": []map[string]any{
					{
						"id":         "FALLBACK_ITEM",
						"collection": "sentinel-2-l2a",
						"properties": map[string]string{"datetime": "2019-07-10T08:25:00Z"},
						"assets": map[string]any{
							"visual": map[string]string{"href": server.URL + "/asset.tif", "type": "image/tiff"},
						},
					},
				},
			})
			return
		}
		w.Write(assetBytes)
	}))
	defer server.Close()

	deps.StacClient = stac.NewClient(server.URL, server.Client())
	stacCacheDir := t.TempDir()
	deps.StacCache = stac.NewCache(stacCacheDir)

	before, _, status, warnings, err := ResolveSentinelPair(context.Background(), deps, "RU_TVER_01", 2019, 2024)
	if err != nil {
		t.Fatalf("ResolveSentinelPair() error = %v", err)
	}
	if status != "partial" {
		t.Errorf("status = %q, want partial (STAC fallback used)", status)
	}
	if len(warnings) == 0 {
		t.Error("warnings empty, want a warning naming the STAC fallback")
	}
	if before.Path == "" {
		t.Fatal("before.Path empty, want the cached STAC asset path")
	}
	if _, err := os.Stat(before.Path); err != nil {
		t.Errorf("cached asset not found on disk: %v", err)
	}

	// The curated (empty in this test) local Sentinel2/ tree must remain
	// untouched by the fallback write.
	curatedDir := filepath.Join(deps.DataRoot, "RU_TVER_01", "Sentinel2")
	entries, _ := os.ReadDir(curatedDir)
	for _, e := range entries {
		if filepath.Dir(before.Path) == curatedDir {
			t.Fatalf("STAC fallback wrote into the curated dataset dir: %s", before.Path)
		}
		_ = e
	}
}

func TestResolveSentinelPairBothMissReturnsErrorWithoutDeletingAnything(t *testing.T) {
	deps := buildFixtureDataset(t)
	deps.Scenes = nil
	deps.StacClient = nil
	deps.StacCache = nil

	before := filepath.Join(deps.DataRoot, "RU_TVER_01", "CCI_Biomass_2019.tif")
	if _, err := os.Stat(before); err != nil {
		t.Fatalf("fixture setup broken: %v", err)
	}

	if _, _, _, _, err := ResolveSentinelPair(context.Background(), deps, "RU_TVER_01", 2019, 2024); err == nil {
		t.Error("ResolveSentinelPair() error = nil, want error when both local and STAC are unavailable")
	}

	if _, err := os.Stat(before); err != nil {
		t.Errorf("unrelated dataset file was removed: %v", err)
	}
}

// failingHTTPClient returns an http.Client whose transport always errors,
// so a test can assert a code path never makes an HTTP call.
func failingHTTPClient(t *testing.T) *http.Client {
	t.Helper()
	return &http.Client{Transport: failingRoundTripper{}}
}

type failingRoundTripper struct{}

func (failingRoundTripper) RoundTrip(r *http.Request) (*http.Response, error) {
	panic("unexpected HTTP call: local Sentinel-2 resolution should never reach the network")
}
