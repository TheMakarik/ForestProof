package stac

import (
	"context"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"testing"
	"time"
)

func TestClientSearch(t *testing.T) {
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost || r.URL.Path != "/search" {
			t.Errorf("unexpected request: %s %s", r.Method, r.URL.Path)
		}
		var body stacSearchBody
		if err := json.NewDecoder(r.Body).Decode(&body); err != nil {
			t.Fatalf("decode request body: %v", err)
		}
		if len(body.Collections) != 1 || body.Collections[0] != "sentinel-2-l2a" {
			t.Errorf("Collections = %v, want [sentinel-2-l2a]", body.Collections)
		}

		w.Header().Set("Content-Type", "application/geo+json")
		json.NewEncoder(w).Encode(stacItemCollection{
			Features: []stacFeature{
				{
					ID:         "S2A_TEST_ITEM",
					Collection: "sentinel-2-l2a",
					Properties: stacFeatureProperties{Datetime: "2024-07-10T08:25:00Z"},
					Assets: map[string]Asset{
						"visual": {Href: "https://example.org/visual.tif", Type: "image/tiff"},
					},
				},
			},
		})
	}))
	defer server.Close()

	client := NewClient(server.URL, server.Client())
	items, err := client.Search(context.Background(), SearchRequest{
		Collections: []string{"sentinel-2-l2a"},
		BBoxWest:    32.91, BBoxSouth: 56.59, BBoxEast: 32.974, BBoxNorth: 56.63,
		DateTimeFrom: time.Date(2024, 6, 1, 0, 0, 0, 0, time.UTC),
		DateTimeTo:   time.Date(2024, 8, 1, 0, 0, 0, 0, time.UTC),
	})
	if err != nil {
		t.Fatalf("Search() error = %v", err)
	}
	if len(items) != 1 {
		t.Fatalf("len(items) = %d, want 1", len(items))
	}
	if items[0].ID != "S2A_TEST_ITEM" {
		t.Errorf("ID = %q, want S2A_TEST_ITEM", items[0].ID)
	}
	if _, ok := items[0].Assets["visual"]; !ok {
		t.Error("Assets[visual] missing")
	}
}

func TestClientSearchNonOKStatus(t *testing.T) {
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusInternalServerError)
		w.Write([]byte("boom"))
	}))
	defer server.Close()

	client := NewClient(server.URL, server.Client())
	if _, err := client.Search(context.Background(), SearchRequest{Collections: []string{"sentinel-2-l2a"}}); err == nil {
		t.Fatal("Search() error = nil, want error for 500 response")
	}
}

func TestClientDownloadAssetAtomicAndClean(t *testing.T) {
	want := []byte("fake geotiff bytes")
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Write(want)
	}))
	defer server.Close()

	dir := t.TempDir()
	dest := filepath.Join(dir, "nested", "asset.tif")

	client := NewClient(server.URL, server.Client())
	if err := client.DownloadAsset(context.Background(), server.URL+"/asset.tif", dest); err != nil {
		t.Fatalf("DownloadAsset() error = %v", err)
	}

	got, err := os.ReadFile(dest)
	if err != nil {
		t.Fatalf("ReadFile(dest): %v", err)
	}
	if string(got) != string(want) {
		t.Errorf("downloaded content = %q, want %q", got, want)
	}

	if _, err := os.Stat(dest + ".part"); !os.IsNotExist(err) {
		t.Error("leftover .part file after successful download")
	}
}

func TestClientDownloadAssetFailureLeavesNoPartialFile(t *testing.T) {
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusNotFound)
	}))
	defer server.Close()

	dir := t.TempDir()
	dest := filepath.Join(dir, "asset.tif")

	client := NewClient(server.URL, server.Client())
	err := client.DownloadAsset(context.Background(), server.URL+"/missing.tif", dest)
	if err == nil {
		t.Fatal("DownloadAsset() error = nil, want error for 404")
	}
	if _, statErr := os.Stat(dest); !os.IsNotExist(statErr) {
		t.Error("dest file exists after a failed download")
	}
	if _, statErr := os.Stat(dest + ".part"); !os.IsNotExist(statErr) {
		t.Error("leftover .part file after a failed download")
	}
}
