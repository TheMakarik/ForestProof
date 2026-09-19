// Package hashutil provides SHA-256 helpers shared by the source-catalog
// verifier and the job-request idempotency hash.
package hashutil

import (
	"crypto/sha256"
	"encoding/hex"
	"fmt"
	"io"
	"os"
	"strings"
)

// partSeparator joins StringSHA256's parts with the ASCII unit separator
// (0x1F) rather than a printable character such as "|", since a polygon
// GeoJSON string can legitimately contain almost any printable character
// and a naive join could let two different part sets hash identically.
const partSeparator = "\x1f"

// FileSHA256 streams path's contents through SHA-256 without buffering the
// whole file in memory, returning the digest as lowercase hex.
func FileSHA256(path string) (string, error) {
	f, err := os.Open(path)
	if err != nil {
		return "", fmt.Errorf("hashutil: open %s: %w", path, err)
	}
	defer f.Close()

	h := sha256.New()
	if _, err := io.Copy(h, f); err != nil {
		return "", fmt.Errorf("hashutil: hash %s: %w", path, err)
	}
	return hex.EncodeToString(h.Sum(nil)), nil
}

// StringSHA256 returns the lowercase-hex SHA-256 digest of parts joined by
// an unambiguous separator, so callers get a stable, collision-resistant
// hash of several independent fields (used for the analysis-request
// idempotency key).
func StringSHA256(parts ...string) string {
	sum := sha256.Sum256([]byte(strings.Join(parts, partSeparator)))
	return hex.EncodeToString(sum[:])
}
