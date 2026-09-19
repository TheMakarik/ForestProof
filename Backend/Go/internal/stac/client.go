// Package stac is a minimal client for the STAC API (Earth Search), used
// only as a fallback when an AOI/period's Sentinel-2 assets are not already
// present in the curated local dataset. It is never on the primary path for
// the four bundled demo AOIs, whose Sentinel-2 data is already local.
package stac

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"path/filepath"
	"time"
)

// Asset is one downloadable file attached to a STAC Item (e.g. a band or a
// composite visual).
type Asset struct {
	Href  string `json:"href"`
	Type  string `json:"type"`
	Title string `json:"title"`
}

// Item is one STAC Item (a single satellite scene) as returned by /search.
type Item struct {
	ID         string
	Collection string
	DateTime   time.Time
	Assets     map[string]Asset
}

// SearchRequest describes a STAC item search.
type SearchRequest struct {
	Collections              []string
	BBoxWest, BBoxSouth      float64
	BBoxEast, BBoxNorth      float64
	DateTimeFrom, DateTimeTo time.Time
	Limit                    int
}

// Client is a small STAC API client bound to a single catalog base URL.
type Client struct {
	BaseURL    string
	HTTPClient *http.Client
}

// NewClient returns a Client. If httpClient is nil, http.DefaultClient is
// used.
func NewClient(baseURL string, httpClient *http.Client) *Client {
	if httpClient == nil {
		httpClient = http.DefaultClient
	}
	return &Client{BaseURL: baseURL, HTTPClient: httpClient}
}

type stacSearchBody struct {
	Collections []string   `json:"collections"`
	BBox        [4]float64 `json:"bbox"`
	Datetime    string     `json:"datetime"`
	Limit       int        `json:"limit,omitempty"`
}

type stacItemCollection struct {
	Features []stacFeature `json:"features"`
}

type stacFeature struct {
	ID         string                `json:"id"`
	Collection string                `json:"collection"`
	Properties stacFeatureProperties `json:"properties"`
	Assets     map[string]Asset      `json:"assets"`
}

type stacFeatureProperties struct {
	Datetime string `json:"datetime"`
}

// Search runs a STAC item search and returns the matching items.
func (c *Client) Search(ctx context.Context, req SearchRequest) ([]Item, error) {
	limit := req.Limit
	if limit <= 0 {
		limit = 10
	}

	body := stacSearchBody{
		Collections: req.Collections,
		BBox:        [4]float64{req.BBoxWest, req.BBoxSouth, req.BBoxEast, req.BBoxNorth},
		Datetime:    fmt.Sprintf("%s/%s", req.DateTimeFrom.Format(time.RFC3339), req.DateTimeTo.Format(time.RFC3339)),
		Limit:       limit,
	}

	payload, err := json.Marshal(body)
	if err != nil {
		return nil, fmt.Errorf("stac: encode search request: %w", err)
	}

	httpReq, err := http.NewRequestWithContext(ctx, http.MethodPost, c.BaseURL+"/search", bytes.NewReader(payload))
	if err != nil {
		return nil, fmt.Errorf("stac: build search request: %w", err)
	}
	httpReq.Header.Set("Content-Type", "application/json")
	httpReq.Header.Set("Accept", "application/geo+json")

	resp, err := c.HTTPClient.Do(httpReq)
	if err != nil {
		return nil, fmt.Errorf("stac: search request failed: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		respBody, _ := io.ReadAll(io.LimitReader(resp.Body, 4096))
		return nil, fmt.Errorf("stac: search returned %d: %s", resp.StatusCode, string(respBody))
	}

	var collection stacItemCollection
	if err := json.NewDecoder(resp.Body).Decode(&collection); err != nil {
		return nil, fmt.Errorf("stac: decode search response: %w", err)
	}

	items := make([]Item, 0, len(collection.Features))
	for _, f := range collection.Features {
		item := Item{ID: f.ID, Collection: f.Collection, Assets: f.Assets}
		if f.Properties.Datetime != "" {
			if t, err := time.Parse(time.RFC3339, f.Properties.Datetime); err == nil {
				item.DateTime = t
			}
		}
		items = append(items, item)
	}
	return items, nil
}

// DownloadAsset streams href to destPath, writing to a temporary "*.part"
// file first and atomically renaming it into place on success — so a
// canceled or failed download never leaves a half-written file that could
// be mistaken for a complete, cached asset.
func (c *Client) DownloadAsset(ctx context.Context, href, destPath string) error {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, href, nil)
	if err != nil {
		return fmt.Errorf("stac: build download request: %w", err)
	}

	resp, err := c.HTTPClient.Do(req)
	if err != nil {
		return fmt.Errorf("stac: download %s: %w", href, err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return fmt.Errorf("stac: download %s returned %d", href, resp.StatusCode)
	}

	if err := os.MkdirAll(filepath.Dir(destPath), 0o755); err != nil {
		return fmt.Errorf("stac: create cache dir for %s: %w", destPath, err)
	}

	partPath := destPath + ".part"
	f, err := os.Create(partPath)
	if err != nil {
		return fmt.Errorf("stac: create %s: %w", partPath, err)
	}

	if _, err := io.Copy(f, resp.Body); err != nil {
		f.Close()
		os.Remove(partPath)
		return fmt.Errorf("stac: write %s: %w", partPath, err)
	}
	if err := f.Close(); err != nil {
		os.Remove(partPath)
		return fmt.Errorf("stac: close %s: %w", partPath, err)
	}

	if err := os.Rename(partPath, destPath); err != nil {
		os.Remove(partPath)
		return fmt.Errorf("stac: finalize %s: %w", destPath, err)
	}
	return nil
}
