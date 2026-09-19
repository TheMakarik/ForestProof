package api

import (
	"bytes"
	"context"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"testing"
	"time"

	"forestproof-gateway/internal/catalog"
	"forestproof-gateway/internal/jobs"
	"forestproof-gateway/internal/upstream"
	"forestproof-gateway/internal/verify"
)

func writeFixtureFile(t *testing.T, path string, content []byte) {
	t.Helper()
	if err := os.MkdirAll(filepath.Dir(path), 0o755); err != nil {
		t.Fatalf("MkdirAll: %v", err)
	}
	if err := os.WriteFile(path, content, 0o644); err != nil {
		t.Fatalf("WriteFile(%s): %v", path, err)
	}
}

// newTestServer wires a real api.NewRouter against a fixture Dataset dir
// and a fake upstream C# server (canned responses, no real GDAL/compute),
// so the whole request -> job -> response path is exercised without any
// external process.
func newTestServer(t *testing.T) *httptest.Server {
	t.Helper()
	dataRoot := t.TempDir()

	writeFixtureFile(t, filepath.Join(dataRoot, "areas.csv"),
		[]byte("aoi_id,name,region,analysis_start_year,analysis_end_year,area_ha,selection_role,project_status,stand_age,dominant_species,site_class,bbox_west,bbox_south,bbox_east,bbox_north,baseline_id\n"+
			"RU_TVER_01,Tver,Tver,2019,2024,1750.47,control,research,,,,32.91,56.59,32.974,56.63,HIST\n"))
	writeFixtureFile(t, filepath.Join(dataRoot, "areas.geojson"), []byte(`{
		"type":"FeatureCollection",
		"features":[{"type":"Feature","id":"RU_TVER_01","properties":{"aoi_id":"RU_TVER_01"},"geometry":{"type":"Polygon","coordinates":[]}}]
	}`))

	fileContent := []byte("fake-agb-2019")
	writeFixtureFile(t, filepath.Join(dataRoot, "RU_TVER_01", "CCI_Biomass_2019.tif"), fileContent)

	fileCatalogHeader := "relative_path,aoi_id,source_ids,product_version,observation_or_scenario_period,data_kind,bands_or_fields,units,scale_and_offset,dtype,shape_rows_cols_bands,crs,pixel_size_native_units,nodata,processing,original_urls_or_inputs,license_source_ids,retrieved_or_created_date,size_bytes,sha256\n"
	fileCatalogRow := "RU_TVER_01/CCI_Biomass_2019.tif,RU_TVER_01,CCI_V7,7.0,2019,external,bands,units,scale,dtype,shape,EPSG:4326,pixel,nodata,proc,urls,lic,2026-09-16,13," + sha256Hex(fileContent) + "\n"
	writeFixtureFile(t, filepath.Join(dataRoot, "file_catalog.csv"), []byte(fileCatalogHeader+fileCatalogRow))

	writeFixtureFile(t, filepath.Join(dataRoot, "sources.csv"),
		[]byte("source_id,product,version,kind,primary_url,doi,license_url,redistribution_basis,required_attribution,access_date,limitations\n"+
			"CCI_V7,ESA CCI Biomass,7.0,external,https://example.org,,https://example.org/license,basis,attribution,2026-09-16,limits\n"))

	writeFixtureFile(t, filepath.Join(dataRoot, "scenes.csv"),
		[]byte("scene_key,aoi_id,item_id,datetime_utc,year,collection,processing_baseline,source_scene_cloud_percent,scl_4_5_6_7_fraction_crop,selection_role,reflectance_path,scl_path,metadata_file\n"))

	areas, err := catalog.LoadAreas(dataRoot)
	if err != nil {
		t.Fatalf("LoadAreas() error = %v", err)
	}
	sources, err := catalog.LoadSources(dataRoot)
	if err != nil {
		t.Fatalf("LoadSources() error = %v", err)
	}
	fileCatalog, err := catalog.LoadFileCatalog(dataRoot)
	if err != nil {
		t.Fatalf("LoadFileCatalog() error = %v", err)
	}
	scenes, err := catalog.LoadScenes(dataRoot)
	if err != nil {
		t.Fatalf("LoadScenes() error = %v", err)
	}

	scanner := verify.NewScanner()
	scanner.Run(context.Background(), dataRoot, fileCatalog) // synchronous: deterministic before the server starts

	fakeUpstream := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		switch {
		case r.Method == http.MethodPost && r.URL.Path == "/api/v1/analyses":
			w.Header().Set("Content-Type", "application/json")
			w.Write([]byte(`{"runId":"upstream-run","status":"Complete","aoiId":"RU_TVER_01"}`))
		case r.Method == http.MethodPost && r.URL.Path == "/api/v1/analyses/changes":
			w.Header().Set("Content-Type", "application/geo+json")
			w.Write([]byte(`{"type":"FeatureCollection","features":[]}`))
		case r.Method == http.MethodPost && r.URL.Path == "/api/v1/analyses/reports":
			w.Header().Set("Content-Type", "application/pdf")
			w.Write([]byte("%PDF-1.4 fake"))
		case r.Method == http.MethodGet && r.URL.Path == "/api/v1/experiments/sensitivity":
			w.Header().Set("Content-Type", "application/json")
			w.Write([]byte(`{"aoiId":"RU_TVER_01","sensitivity":[]}`))
		case r.Method == http.MethodGet && r.URL.Path == "/api/v1/analyses/RU_TVER_01/changes":
			w.Header().Set("Content-Type", "application/geo+json")
			w.Write([]byte(`{"type":"FeatureCollection","features":[]}`))
		case r.Method == http.MethodPost && r.URL.Path == "/api/v1/analyses/RU_TVER_01/reports":
			w.Header().Set("Content-Type", "application/pdf")
			w.Write([]byte("%PDF-1.4 fake"))
		case r.Method == http.MethodGet && r.URL.Path == "/api/v1/areas":
			w.Header().Set("Content-Type", "application/json")
			w.Write([]byte(`[{"id":"RU_TVER_01","name":"Tver","area_ha":1750.47}]`))
		default:
			w.WriteHeader(http.StatusNotFound)
		}
	}))
	t.Cleanup(fakeUpstream.Close)

	store, err := jobs.NewStore(t.TempDir())
	if err != nil {
		t.Fatalf("NewStore() error = %v", err)
	}
	upstreamClient := upstream.NewClient(fakeUpstream.URL, fakeUpstream.Client())
	executor := jobs.NewExecutor(store, upstreamClient, 5*time.Second)

	deps := Deps{
		Store:                store,
		Executor:             executor,
		Upstream:             upstreamClient,
		Areas:                areas,
		Sources:              sources,
		FileCatalog:          fileCatalog,
		Scenes:               scenes,
		Verifier:             scanner,
		DataRoot:             dataRoot,
		MethodProfileVersion: "v1",
		MinAnalysisYear:      2019,
		MaxAnalysisYear:      2024,
	}

	server := httptest.NewServer(NewRouter(deps))
	t.Cleanup(server.Close)
	return server
}

func sha256Hex(data []byte) string {
	sum := sha256.Sum256(data)
	return hex.EncodeToString(sum[:])
}

func postJSON(t *testing.T, server *httptest.Server, path string, body any) (*http.Response, map[string]any) {
	t.Helper()
	payload, err := json.Marshal(body)
	if err != nil {
		t.Fatalf("Marshal request body: %v", err)
	}
	resp, err := http.Post(server.URL+path, "application/json", bytes.NewReader(payload))
	if err != nil {
		t.Fatalf("POST %s: %v", path, err)
	}
	defer resp.Body.Close()

	var decoded map[string]any
	_ = json.NewDecoder(resp.Body).Decode(&decoded)
	return resp, decoded
}

func waitForTerminalStatus(t *testing.T, server *httptest.Server, id string) map[string]any {
	t.Helper()
	deadline := time.Now().Add(2 * time.Second)
	for time.Now().Before(deadline) {
		resp, err := http.Get(server.URL + "/api/v1/analyses/" + id + "/status")
		if err != nil {
			t.Fatalf("GET status: %v", err)
		}
		var decoded map[string]any
		_ = json.NewDecoder(resp.Body).Decode(&decoded)
		resp.Body.Close()

		switch decoded["status"] {
		case "complete", "partial", "units_unavailable", "failed":
			return decoded
		}
		time.Sleep(5 * time.Millisecond)
	}
	t.Fatalf("job %s did not reach a terminal status within the deadline", id)
	return nil
}

func TestCreateAnalysisRequiresAoiOrPolygon(t *testing.T) {
	server := newTestServer(t)
	resp, _ := postJSON(t, server, "/api/v1/analyses", map[string]any{"startYear": 2019, "endYear": 2024})
	if resp.StatusCode != http.StatusBadRequest {
		t.Errorf("status = %d, want 400", resp.StatusCode)
	}
}

func TestCreateAnalysisIsIdempotent(t *testing.T) {
	server := newTestServer(t)
	body := map[string]any{"aoiId": "RU_TVER_01", "startYear": 2019, "endYear": 2024}

	resp1, decoded1 := postJSON(t, server, "/api/v1/analyses", body)
	if resp1.StatusCode != http.StatusAccepted {
		t.Fatalf("first POST status = %d, want 202", resp1.StatusCode)
	}
	id1 := decoded1["id"].(string)
	if decoded1["idempotent"].(bool) {
		t.Error("first POST idempotent = true, want false")
	}

	resp2, decoded2 := postJSON(t, server, "/api/v1/analyses", body)
	if resp2.StatusCode != http.StatusOK {
		t.Errorf("second POST status = %d, want 200", resp2.StatusCode)
	}
	id2 := decoded2["id"].(string)
	if id1 != id2 {
		t.Errorf("second POST id = %q, want same as first %q", id2, id1)
	}
	if !decoded2["idempotent"].(bool) {
		t.Error("second POST idempotent = false, want true")
	}
}

func TestFullJobLifecycle(t *testing.T) {
	server := newTestServer(t)
	_, created := postJSON(t, server, "/api/v1/analyses", map[string]any{
		"aoiId": "RU_TVER_01", "startYear": 2019, "endYear": 2024,
	})
	id := created["id"].(string)

	status := waitForTerminalStatus(t, server, id)
	if status["status"] != "complete" {
		t.Fatalf("status = %v, want complete", status["status"])
	}

	summaryResp, err := http.Get(server.URL + "/api/v1/analyses/" + id + "/summary")
	if err != nil {
		t.Fatalf("GET summary: %v", err)
	}
	defer summaryResp.Body.Close()
	if summaryResp.StatusCode != http.StatusOK {
		t.Errorf("summary status = %d, want 200", summaryResp.StatusCode)
	}

	changesResp, err := http.Get(server.URL + "/api/v1/analyses/" + id + "/changes")
	if err != nil {
		t.Fatalf("GET changes: %v", err)
	}
	defer changesResp.Body.Close()
	var changes map[string]any
	json.NewDecoder(changesResp.Body).Decode(&changes)
	if changes["type"] != "FeatureCollection" {
		t.Errorf("changes.type = %v, want FeatureCollection", changes["type"])
	}

	layersResp, err := http.Get(server.URL + "/api/v1/analyses/" + id + "/layers")
	if err != nil {
		t.Fatalf("GET layers: %v", err)
	}
	defer layersResp.Body.Close()
	if layersResp.StatusCode != http.StatusOK {
		t.Errorf("layers status = %d, want 200", layersResp.StatusCode)
	}

	cciResp, err := http.Get(server.URL + "/api/v1/analyses/" + id + "/layers/cci_change")
	if err != nil {
		t.Fatalf("GET layers/cci_change: %v", err)
	}
	defer cciResp.Body.Close()
	if cciResp.StatusCode != http.StatusNotFound {
		t.Errorf("layers/cci_change status = %d, want 404 (job period is 2019-2024, not 2019-2020)", cciResp.StatusCode)
	}

	reportResp, err := http.Post(server.URL+"/api/v1/analyses/"+id+"/reports", "", nil)
	if err != nil {
		t.Fatalf("POST reports: %v", err)
	}
	defer reportResp.Body.Close()
	if reportResp.StatusCode != http.StatusOK {
		t.Errorf("reports status = %d, want 200", reportResp.StatusCode)
	}
	if reportResp.Header.Get("Content-Type") != "application/pdf" {
		t.Errorf("reports Content-Type = %q, want application/pdf", reportResp.Header.Get("Content-Type"))
	}
}

func TestGetSourcesReportsVerification(t *testing.T) {
	server := newTestServer(t)
	resp, err := http.Get(server.URL + "/api/v1/sources")
	if err != nil {
		t.Fatalf("GET sources: %v", err)
	}
	defer resp.Body.Close()
	if resp.StatusCode != http.StatusOK {
		t.Fatalf("status = %d, want 200", resp.StatusCode)
	}

	var sources []map[string]any
	if err := json.NewDecoder(resp.Body).Decode(&sources); err != nil {
		t.Fatalf("decode: %v", err)
	}
	if len(sources) != 1 {
		t.Fatalf("len(sources) = %d, want 1", len(sources))
	}
	files, ok := sources[0]["files"].([]any)
	if !ok || len(files) != 1 {
		t.Fatalf("sources[0].files = %v, want 1 entry", sources[0]["files"])
	}
	file := files[0].(map[string]any)
	if file["verified"] != "verified" {
		t.Errorf("verified = %v, want verified (fixture file hash matches catalog)", file["verified"])
	}
}

func TestGetStatusUnknownID(t *testing.T) {
	server := newTestServer(t)
	resp, err := http.Get(server.URL + "/api/v1/analyses/does-not-exist/status")
	if err != nil {
		t.Fatalf("GET status: %v", err)
	}
	defer resp.Body.Close()
	if resp.StatusCode != http.StatusNotFound {
		t.Errorf("status = %d, want 404", resp.StatusCode)
	}
}
