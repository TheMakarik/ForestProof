package catalog

import (
	"os"
	"path/filepath"
	"testing"
)

const scenesHeader = "scene_key,aoi_id,item_id,datetime_utc,year,collection,processing_baseline,source_scene_cloud_percent,scl_4_5_6_7_fraction_crop,selection_role,reflectance_path,scl_path,metadata_file\n"

func writeScenesFixture(t *testing.T, rows string) string {
	t.Helper()
	dir := t.TempDir()
	if err := os.WriteFile(filepath.Join(dir, "scenes.csv"), []byte(scenesHeader+rows), 0o644); err != nil {
		t.Fatalf("write scenes.csv: %v", err)
	}
	return dir
}

func TestSelectScenePairPicksClosestToJuly15(t *testing.T) {
	rows := "" +
		"RU_TVER_01__A,RU_TVER_01,A,2019-06-25T08:25:00Z,2019,sentinel-2-l2a,05.00,10,0.9,летнее наблюдение,RU_TVER_01/Sentinel2/A_reflectance.tif,RU_TVER_01/Sentinel2/A_SCL.tif,m.json\n" +
		"RU_TVER_01__B,RU_TVER_01,B,2019-08-14T08:24:58Z,2019,sentinel-2-l2a,05.00,15,0.99,летнее наблюдение,RU_TVER_01/Sentinel2/B_reflectance.tif,RU_TVER_01/Sentinel2/B_SCL.tif,m.json\n" +
		"RU_TVER_01__C,RU_TVER_01,C,2024-07-10T08:25:00Z,2024,sentinel-2-l2a,05.00,5,0.95,летнее наблюдение,RU_TVER_01/Sentinel2/C_reflectance.tif,RU_TVER_01/Sentinel2/C_SCL.tif,m.json\n" +
		"RU_TVER_01__D,RU_TVER_01,D,2024-09-01T08:25:00Z,2024,sentinel-2-l2a,05.00,20,0.80,летнее наблюдение,RU_TVER_01/Sentinel2/D_reflectance.tif,RU_TVER_01/Sentinel2/D_SCL.tif,m.json\n"
	dir := writeScenesFixture(t, rows)

	scenes, err := LoadScenes(dir)
	if err != nil {
		t.Fatalf("LoadScenes() error = %v", err)
	}
	if len(scenes) != 4 {
		t.Fatalf("len(scenes) = %d, want 4", len(scenes))
	}

	pair, ok := SelectScenePair(scenes, "RU_TVER_01", 2019, 2024)
	if !ok {
		t.Fatal("SelectScenePair() ok = false, want true")
	}
	// July 15 2019: B (Aug 14, 30d away) is closer than A (June 25, 20d away)... compute precisely below.
	if pair.Before.ItemID != "A" {
		t.Errorf("Before = %q, want A (closest to 2019-07-15)", pair.Before.ItemID)
	}
	if pair.After.ItemID != "C" {
		t.Errorf("After = %q, want C (closest to 2024-07-15)", pair.After.ItemID)
	}
}

func TestSelectScenePairNoScenesForAOI(t *testing.T) {
	dir := writeScenesFixture(t, "")
	scenes, err := LoadScenes(dir)
	if err != nil {
		t.Fatalf("LoadScenes() error = %v", err)
	}

	if _, ok := SelectScenePair(scenes, "RU_TVER_01", 2019, 2024); ok {
		t.Error("SelectScenePair() ok = true, want false for AOI with no scenes")
	}
}

func TestSelectScenePairSameSceneBothSides(t *testing.T) {
	rows := "RU_TVER_01__A,RU_TVER_01,A,2019-07-15T08:25:00Z,2019,sentinel-2-l2a,05.00,10,0.9,летнее наблюдение,RU_TVER_01/Sentinel2/A_reflectance.tif,RU_TVER_01/Sentinel2/A_SCL.tif,m.json\n"
	dir := writeScenesFixture(t, rows)
	scenes, err := LoadScenes(dir)
	if err != nil {
		t.Fatalf("LoadScenes() error = %v", err)
	}

	if _, ok := SelectScenePair(scenes, "RU_TVER_01", 2019, 2019); ok {
		t.Error("SelectScenePair() ok = true, want false when before/after resolve to the same scene")
	}
}
