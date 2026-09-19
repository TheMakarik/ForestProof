package stac

import (
	"os"
	"path/filepath"
)

// Cache is where STAC-fallback assets are stored on disk. Its Dir is always
// distinct from any AOI's curated Dataset/{aoi}/Sentinel2/ folder — this is
// the concrete mechanism behind the "fallback, don't silently delete"
// dataset rule: nothing in this package ever writes into, or removes
// anything from, the curated dataset tree.
type Cache struct {
	Dir string
}

// NewCache returns a Cache rooted at dir.
func NewCache(dir string) *Cache {
	return &Cache{Dir: dir}
}

// Path returns the deterministic cache location for one asset of one item
// of one AOI.
func (c *Cache) Path(aoiID, itemID, assetKey, ext string) string {
	return filepath.Join(c.Dir, aoiID, itemID+"_"+assetKey+ext)
}

// Has reports whether an asset is already cached at the path Path would
// return for the same arguments.
func (c *Cache) Has(aoiID, itemID, assetKey, ext string) bool {
	_, err := os.Stat(c.Path(aoiID, itemID, assetKey, ext))
	return err == nil
}
