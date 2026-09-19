// Package layers resolves the gateway's map-layer keys (aoi, agb_start,
// agb_end, gfc, cci_change, modis_burn, sentinel2_before, sentinel2_after)
// to actual bytes on disk (or, for Sentinel-2 as a last resort, a STAC
// fallback download), each annotated with provenance/legend metadata drawn
// from the local dataset catalog.
package layers

import (
	"context"
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"
	"time"

	"forestproof-gateway/internal/catalog"
	"forestproof-gateway/internal/stac"
)

const (
	biomassFileNamePattern = "CCI_Biomass_%d.tif"
	changeFileName         = "CCI_Change_2019_2020.tif"
	gfcFileName            = "GFC_2025_v1_13.tif"
	modisDirName           = "MODIS"
)

// LayerFile is a resolved layer: either a file on disk (Path) or in-memory
// bytes (Bytes, e.g. the AOI's GeoJSON feature) — exactly one is set.
type LayerFile struct {
	Path        string
	Bytes       []byte
	ContentType string
	Source      *catalog.FileEntry // nil if this file isn't in file_catalog.csv
}

// LayerInfo describes one layer key's availability for a given AOI/period,
// for the GET .../layers listing endpoint.
type LayerInfo struct {
	Key       string `json:"key"`
	Available bool   `json:"available"`
	Reason    string `json:"reason,omitempty"`
	SourceID  string `json:"sourceId,omitempty"`
	License   string `json:"licenseUrl,omitempty"`
}

// Deps bundles everything ResolveLayer/ListLayers need.
type Deps struct {
	DataRoot    string
	Areas       []catalog.Area
	Sources     []catalog.Source
	FileCatalog []catalog.FileEntry
	Scenes      []catalog.Scene
	StacClient  *stac.Client
	StacCache   *stac.Cache
}

var allLayerKeys = []string{
	"aoi", "agb_start", "agb_end", "gfc", "cci_change", "modis_burn",
	"sentinel2_before", "sentinel2_after",
}

// ListLayers reports, for every known layer key, whether it can currently
// be resolved for aoiID/startYear/endYear. It never performs network I/O:
// the two Sentinel-2 keys are reported available only if a local scene
// pair exists — actually falling back to STAC only happens when the layer
// is fetched via ResolveLayer/ResolveSentinelPair, not while listing.
func ListLayers(deps Deps, aoiID string, startYear, endYear int) []LayerInfo {
	infos := make([]LayerInfo, 0, len(allLayerKeys))
	for _, key := range allLayerKeys {
		if key == "sentinel2_before" || key == "sentinel2_after" {
			infos = append(infos, listSentinelInfo(deps, aoiID, startYear, endYear, key))
			continue
		}

		info := LayerInfo{Key: key}
		file, _, err := ResolveLayer(context.Background(), deps, aoiID, startYear, endYear, key)
		if err != nil {
			info.Available = false
			info.Reason = err.Error()
		} else {
			info.Available = true
			if file.Source != nil {
				info.SourceID = strings.Join(file.Source.SourceIDs, ";")
				info.License = firstLicenseURL(deps.Sources, file.Source.SourceIDs)
			}
		}
		infos = append(infos, info)
	}
	return infos
}

// firstLicenseURL returns the license URL of the first of sourceIDs found
// in sources, or "" if none match.
func firstLicenseURL(sources []catalog.Source, sourceIDs []string) string {
	for _, id := range sourceIDs {
		for _, source := range sources {
			if source.SourceID == id {
				return source.LicenseURL
			}
		}
	}
	return ""
}

func listSentinelInfo(deps Deps, aoiID string, startYear, endYear int, key string) LayerInfo {
	if _, ok := catalog.SelectScenePair(deps.Scenes, aoiID, startYear, endYear); ok {
		return LayerInfo{Key: key, Available: true}
	}
	reason := "no local Sentinel-2 scene pair for this AOI/period"
	if deps.StacClient != nil {
		reason += " (a STAC fallback fetch will be attempted when this layer is requested directly)"
	}
	return LayerInfo{Key: key, Available: false, Reason: reason}
}

// ResolveLayer resolves one layer key to its bytes/path plus any warnings
// (e.g. a STAC-fallback substitution). It never deletes or overwrites a
// file under Deps.DataRoot.
func ResolveLayer(ctx context.Context, deps Deps, aoiID string, startYear, endYear int, key string) (LayerFile, []string, error) {
	switch key {
	case "aoi":
		return resolveAOI(deps, aoiID)
	case "agb_start":
		return resolveCatalogedFile(deps, aoiID, fmt.Sprintf(biomassFileNamePattern, startYear))
	case "agb_end":
		return resolveCatalogedFile(deps, aoiID, fmt.Sprintf(biomassFileNamePattern, endYear))
	case "gfc":
		return resolveCatalogedFile(deps, aoiID, gfcFileName)
	case "cci_change":
		return resolveCciChange(deps, aoiID, startYear, endYear)
	case "modis_burn":
		return resolveModisBurn(deps, aoiID)
	case "sentinel2_before":
		before, _, status, warnings, err := ResolveSentinelPair(ctx, deps, aoiID, startYear, endYear)
		if err != nil {
			return LayerFile{}, nil, err
		}
		return before, annotateStatus(warnings, status), nil
	case "sentinel2_after":
		_, after, status, warnings, err := ResolveSentinelPair(ctx, deps, aoiID, startYear, endYear)
		if err != nil {
			return LayerFile{}, nil, err
		}
		return after, annotateStatus(warnings, status), nil
	default:
		return LayerFile{}, nil, fmt.Errorf("layers: unknown layer key %q", key)
	}
}

func annotateStatus(warnings []string, status string) []string {
	if status == "partial" {
		return warnings
	}
	return nil
}

func resolveAOI(deps Deps, aoiID string) (LayerFile, []string, error) {
	raw, err := catalog.LoadAreaFeature(deps.DataRoot, aoiID)
	if err != nil {
		return LayerFile{}, nil, err
	}
	return LayerFile{Bytes: raw, ContentType: "application/geo+json"}, nil, nil
}

// resolveCatalogedFile resolves a layer backed by a fixed dataset file,
// looking the file up through file_catalog.csv first — so a filename-scheme
// drift in the dataset surfaces as a clear "not in catalog" error instead
// of silently serving a wrong or stale path.
func resolveCatalogedFile(deps Deps, aoiID, fileName string) (LayerFile, []string, error) {
	relativePath := filepath.Join(aoiID, fileName)

	entry := findCatalogEntry(deps.FileCatalog, relativePath)
	if entry == nil {
		return LayerFile{}, nil, fmt.Errorf("layers: %s is not in file_catalog.csv for %s", fileName, aoiID)
	}

	path := filepath.Join(deps.DataRoot, relativePath)
	if _, err := os.Stat(path); err != nil {
		return LayerFile{}, nil, fmt.Errorf("layers: %s is cataloged but missing on disk: %w", relativePath, err)
	}

	return LayerFile{Path: path, ContentType: "image/tiff", Source: entry}, nil, nil
}

func findCatalogEntry(entries []catalog.FileEntry, relativePath string) *catalog.FileEntry {
	// Dataset relative paths use "/" regardless of OS; normalize for
	// comparison against filepath.Join's OS-native separator.
	want := filepath.ToSlash(relativePath)
	for i := range entries {
		if filepath.ToSlash(entries[i].RelativePath) == want {
			return &entries[i]
		}
	}
	return nil
}

func resolveCciChange(deps Deps, aoiID string, startYear, endYear int) (LayerFile, []string, error) {
	if startYear != 2019 || endYear != 2020 {
		return LayerFile{}, nil, fmt.Errorf(
			"layers: cci_change only covers the fixed 2019-2020 period, not %d-%d", startYear, endYear)
	}
	return resolveCatalogedFile(deps, aoiID, changeFileName)
}

func resolveModisBurn(deps Deps, aoiID string) (LayerFile, []string, error) {
	dir := filepath.Join(deps.DataRoot, aoiID, modisDirName)
	matches, err := filepath.Glob(filepath.Join(dir, "*_Burn_Date.tif"))
	if err != nil || len(matches) == 0 {
		return LayerFile{}, nil, fmt.Errorf("layers: no MODIS burn-date raster available for %s", aoiID)
	}
	sort.Strings(matches)
	path := matches[0]

	relativePath, _ := filepath.Rel(deps.DataRoot, path)
	entry := findCatalogEntry(deps.FileCatalog, relativePath)

	return LayerFile{Path: path, ContentType: "image/tiff", Source: entry}, nil, nil
}

// ResolveSentinelPair implements the fallback chain: (1) try the curated
// local Sentinel2/ folder via catalog.SelectScenePair — no network; (2)
// only on a local miss, search+download via STAC, caching outside
// Deps.DataRoot; (3) if both fail, return an error (never an empty/zeroed
// success).
//
// Known gap: a STAC-fallback asset is served as-is for map display only —
// it is never reprocessed into C#'s multi-band "{item}_reflectance.tif"
// composite (that would require GDAL-equivalent band-stacking in Go), so it
// is never fed into the C# compute pipeline.
func ResolveSentinelPair(ctx context.Context, deps Deps, aoiID string, startYear, endYear int) (before, after LayerFile, status string, warnings []string, err error) {
	pair, ok := catalog.SelectScenePair(deps.Scenes, aoiID, startYear, endYear)
	if ok {
		before = sceneToLayerFile(deps, pair.Before)
		after = sceneToLayerFile(deps, pair.After)
		return before, after, "complete", nil, nil
	}

	if deps.StacClient == nil || deps.StacCache == nil {
		return LayerFile{}, LayerFile{}, "", nil,
			fmt.Errorf("layers: no local Sentinel-2 scenes for %s and STAC fallback is not configured", aoiID)
	}

	area, ok := areaByID(deps.Areas, aoiID)
	if !ok {
		return LayerFile{}, LayerFile{}, "", nil, fmt.Errorf("layers: unknown aoi_id %q", aoiID)
	}

	before, beforeErr := fetchStacFallback(ctx, deps, area, startYear)
	after, afterErr := fetchStacFallback(ctx, deps, area, endYear)
	if beforeErr != nil || afterErr != nil {
		return LayerFile{}, LayerFile{}, "", nil, fmt.Errorf(
			"layers: no local Sentinel-2 scenes for %s and STAC fallback failed (before: %v, after: %v)",
			aoiID, beforeErr, afterErr)
	}

	warnings = []string{fmt.Sprintf(
		"Sentinel-2 scenes for %s were not in the local dataset; fetched via STAC fallback (display only, not used in the C# computation)",
		aoiID)}
	return before, after, "partial", warnings, nil
}

func sceneToLayerFile(deps Deps, scene catalog.Scene) LayerFile {
	entry := findCatalogEntry(deps.FileCatalog, scene.ReflectancePath)
	return LayerFile{
		Path:        filepath.Join(deps.DataRoot, scene.ReflectancePath),
		ContentType: "image/tiff",
		Source:      entry,
	}
}

func areaByID(areas []catalog.Area, aoiID string) (catalog.Area, bool) {
	return catalog.FindArea(areas, aoiID)
}

// stacSearchWindow is how far around July 15 of the target year the STAC
// fallback search looks, matching the local selector's summer-scene
// assumption.
const stacSearchWindow = 45 * 24 * time.Hour

func fetchStacFallback(ctx context.Context, deps Deps, area catalog.Area, year int) (LayerFile, error) {
	target := time.Date(year, summerMonth(), summerDay(), 0, 0, 0, 0, time.UTC)

	items, err := deps.StacClient.Search(ctx, stac.SearchRequest{
		Collections: []string{"sentinel-2-l2a"},
		BBoxWest:    area.BBoxWest, BBoxSouth: area.BBoxSouth,
		BBoxEast: area.BBoxEast, BBoxNorth: area.BBoxNorth,
		DateTimeFrom: target.Add(-stacSearchWindow),
		DateTimeTo:   target.Add(stacSearchWindow),
		Limit:        1,
	})
	if err != nil {
		return LayerFile{}, err
	}
	if len(items) == 0 {
		return LayerFile{}, fmt.Errorf("no STAC items found near %s", target.Format("2006-01-02"))
	}

	item := items[0]
	asset, assetKey, ok := pickDisplayAsset(item)
	if !ok {
		return LayerFile{}, fmt.Errorf("STAC item %s has no usable asset", item.ID)
	}

	destPath := deps.StacCache.Path(area.ID, item.ID, assetKey, filepath.Ext(asset.Href))
	if !deps.StacCache.Has(area.ID, item.ID, assetKey, filepath.Ext(asset.Href)) {
		if err := deps.StacClient.DownloadAsset(ctx, asset.Href, destPath); err != nil {
			return LayerFile{}, err
		}
	}

	return LayerFile{Path: destPath, ContentType: "image/tiff"}, nil
}

func pickDisplayAsset(item stac.Item) (stac.Asset, string, bool) {
	for _, key := range []string{"visual", "thumbnail"} {
		if asset, ok := item.Assets[key]; ok {
			return asset, key, true
		}
	}
	for key, asset := range item.Assets {
		return asset, key, true
	}
	return stac.Asset{}, "", false
}

func summerMonth() time.Month { return time.July }
func summerDay() int          { return 15 }
