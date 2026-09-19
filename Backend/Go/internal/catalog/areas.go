package catalog

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
)

// Area is one row of areas.csv / one Feature of areas.geojson.
type Area struct {
	ID                string
	Name              string
	Region            string
	SelectionRole     string
	ProjectStatus     string
	BaselineID        string
	AnalysisStartYear int
	AnalysisEndYear   int
	AreaHectares      float64
	BBoxWest          float64
	BBoxSouth         float64
	BBoxEast          float64
	BBoxNorth         float64
}

const areasCSVFileName = "areas.csv"
const areasGeoJSONFileName = "areas.geojson"

// LoadAreas parses <dataRoot>/areas.csv into the AOI registry.
func LoadAreas(dataRoot string) ([]Area, error) {
	table, err := ReadCSV(filepath.Join(dataRoot, areasCSVFileName))
	if err != nil {
		return nil, err
	}

	areas := make([]Area, 0, len(table.Rows()))
	for _, row := range table.Rows() {
		area, err := parseAreaRow(table, row)
		if err != nil {
			return nil, err
		}
		areas = append(areas, area)
	}
	return areas, nil
}

func parseAreaRow(table *Table, row []string) (Area, error) {
	var area Area
	var err error

	if area.ID, err = table.String(row, "aoi_id"); err != nil {
		return Area{}, err
	}
	if area.Name, err = table.String(row, "name"); err != nil {
		return Area{}, err
	}
	if area.Region, err = table.String(row, "region"); err != nil {
		return Area{}, err
	}
	if area.SelectionRole, err = table.String(row, "selection_role"); err != nil {
		return Area{}, err
	}
	if area.ProjectStatus, err = table.String(row, "project_status"); err != nil {
		return Area{}, err
	}
	if area.BaselineID, err = table.String(row, "baseline_id"); err != nil {
		return Area{}, err
	}
	if area.AnalysisStartYear, err = table.Int(row, "analysis_start_year"); err != nil {
		return Area{}, err
	}
	if area.AnalysisEndYear, err = table.Int(row, "analysis_end_year"); err != nil {
		return Area{}, err
	}
	if area.AreaHectares, err = table.Float(row, "area_ha"); err != nil {
		return Area{}, err
	}
	if area.BBoxWest, err = table.Float(row, "bbox_west"); err != nil {
		return Area{}, err
	}
	if area.BBoxSouth, err = table.Float(row, "bbox_south"); err != nil {
		return Area{}, err
	}
	if area.BBoxEast, err = table.Float(row, "bbox_east"); err != nil {
		return Area{}, err
	}
	if area.BBoxNorth, err = table.Float(row, "bbox_north"); err != nil {
		return Area{}, err
	}

	return area, nil
}

// FindArea returns the Area whose ID matches aoiID.
func FindArea(areas []Area, aoiID string) (Area, bool) {
	for _, area := range areas {
		if area.ID == aoiID {
			return area, true
		}
	}
	return Area{}, false
}

// geoJSONFeatureCollection and geoJSONFeature only decode the fields
// LoadAreaFeature needs (id/aoi_id) — the geometry and remaining properties
// are kept as raw JSON so the returned Feature is served byte-identical to
// what's on disk instead of being re-modeled and re-encoded.
type geoJSONFeatureCollection struct {
	Type     string            `json:"type"`
	Features []json.RawMessage `json:"features"`
}

type geoJSONFeatureID struct {
	ID         string `json:"id"`
	Properties struct {
		AoiID string `json:"aoi_id"`
	} `json:"properties"`
}

// LoadAreaFeature returns the raw GeoJSON Feature for aoiID out of
// <dataRoot>/areas.geojson, matched by the feature's top-level "id" or its
// "properties.aoi_id" (both are set to the AOI id in the dataset, checked
// defensively since only "id" is guaranteed present in strict GeoJSON).
func LoadAreaFeature(dataRoot, aoiID string) (json.RawMessage, error) {
	path := filepath.Join(dataRoot, areasGeoJSONFileName)
	data, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("catalog: read %s: %w", path, err)
	}

	var collection geoJSONFeatureCollection
	if err := json.Unmarshal(data, &collection); err != nil {
		return nil, fmt.Errorf("catalog: parse %s: %w", path, err)
	}

	for _, raw := range collection.Features {
		var idFields geoJSONFeatureID
		if err := json.Unmarshal(raw, &idFields); err != nil {
			continue
		}
		if idFields.ID == aoiID || idFields.Properties.AoiID == aoiID {
			return raw, nil
		}
	}

	return nil, fmt.Errorf("catalog: no feature for aoi_id %q in %s", aoiID, areasGeoJSONFileName)
}
