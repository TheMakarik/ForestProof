package catalog

import (
	"path/filepath"
	"time"
)

// Scene is one row of scenes.csv — a single Sentinel-2 observation already
// selected and catalogued for an AOI.
type Scene struct {
	SceneKey        string
	AoiID           string
	ItemID          string
	DatetimeUTC     time.Time
	Year            int
	ReflectancePath string
	SclPath         string
}

// ScenePair is a before/after pair of scenes chosen for a given analysis
// period.
type ScenePair struct {
	Before Scene
	After  Scene
}

const scenesCSVFileName = "scenes.csv"

// summerTarget is the day-of-year ForestProof's scene selection anchors to
// (matches C#'s Services/Spectral/SentinelSceneSelector, which picks the
// scene closest to July 15th of the target year).
const summerMonth = time.July
const summerDay = 15

// LoadScenes parses <dataRoot>/scenes.csv.
func LoadScenes(dataRoot string) ([]Scene, error) {
	table, err := ReadCSV(filepath.Join(dataRoot, scenesCSVFileName))
	if err != nil {
		return nil, err
	}

	scenes := make([]Scene, 0, len(table.Rows()))
	for _, row := range table.Rows() {
		var s Scene
		var ferr error
		if s.SceneKey, ferr = table.String(row, "scene_key"); ferr != nil {
			return nil, ferr
		}
		if s.AoiID, ferr = table.String(row, "aoi_id"); ferr != nil {
			return nil, ferr
		}
		if s.ItemID, ferr = table.String(row, "item_id"); ferr != nil {
			return nil, ferr
		}
		if s.Year, ferr = table.Int(row, "year"); ferr != nil {
			return nil, ferr
		}
		if s.ReflectancePath, ferr = table.String(row, "reflectance_path"); ferr != nil {
			return nil, ferr
		}
		if s.SclPath, ferr = table.String(row, "scl_path"); ferr != nil {
			return nil, ferr
		}

		datetimeRaw, ferr := table.String(row, "datetime_utc")
		if ferr != nil {
			return nil, ferr
		}
		if datetimeRaw != "" {
			t, err := time.Parse(time.RFC3339, datetimeRaw)
			if err != nil {
				return nil, err
			}
			s.DatetimeUTC = t
		}

		scenes = append(scenes, s)
	}
	return scenes, nil
}

// SelectScenePair reimplements C#'s SentinelSceneSelector date-proximity
// pick (closest to July 15 of startYear for "before", closest to July 15 of
// endYear for "after") using scenes.csv's already-parsed datetime_utc
// column instead of re-deriving the date from the filename. It returns
// false if the AOI has no scenes at all, or if the two closest scenes
// resolve to the exact same one (no usable pair).
func SelectScenePair(scenes []Scene, aoiID string, startYear, endYear int) (ScenePair, bool) {
	var aoiScenes []Scene
	for _, s := range scenes {
		if s.AoiID == aoiID {
			aoiScenes = append(aoiScenes, s)
		}
	}
	if len(aoiScenes) == 0 {
		return ScenePair{}, false
	}

	before := closestTo(aoiScenes, time.Date(startYear, summerMonth, summerDay, 0, 0, 0, 0, time.UTC))
	after := closestTo(aoiScenes, time.Date(endYear, summerMonth, summerDay, 0, 0, 0, 0, time.UTC))
	if before.SceneKey == after.SceneKey {
		return ScenePair{}, false
	}

	return ScenePair{Before: before, After: after}, true
}

func closestTo(scenes []Scene, target time.Time) Scene {
	best := scenes[0]
	bestDiff := absDuration(best.DatetimeUTC.Sub(target))
	for _, s := range scenes[1:] {
		diff := absDuration(s.DatetimeUTC.Sub(target))
		if diff < bestDiff {
			best, bestDiff = s, diff
		}
	}
	return best
}

func absDuration(d time.Duration) time.Duration {
	if d < 0 {
		return -d
	}
	return d
}
