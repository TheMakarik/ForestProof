// Package config loads the gateway's runtime configuration from environment
// variables, with sane localhost defaults so the service runs unconfigured
// during local development.
package config

import (
	"fmt"
	"os"
	"path/filepath"
	"strconv"
	"time"
)

// Config holds every tunable the gateway needs at startup.
type Config struct {
	ListenAddr           string
	UpstreamBaseURL      string
	UpstreamTimeout      time.Duration
	JobTimeout           time.Duration
	DataRoot             string
	CacheDir             string
	StacBaseURL          string
	StacTimeout          time.Duration
	MethodProfileVersion string
	MinAnalysisYear      int
	MaxAnalysisYear      int
	MaxRequestBodyBytes  int64
}

// Load reads Config from the environment, applying defaults for anything
// unset. A malformed value (e.g. an unparsable duration) is reported as an
// error naming the offending variable rather than silently falling back.
func Load() (Config, error) {
	cfg := Config{
		ListenAddr:           getenv("GATEWAY_ADDR", ":8080"),
		UpstreamBaseURL:      getenv("CSHARP_BASE_URL", "http://localhost:5126"),
		DataRoot:             getenv("DATA_ROOT", "../Dotnet/ForestProof.Backend/Dataset"),
		CacheDir:             getenv("GATEWAY_CACHE_DIR", "./.cache"),
		StacBaseURL:          getenv("STAC_BASE_URL", "https://earth-search.aws.element84.com/v1"),
		MethodProfileVersion: getenv("GATEWAY_METHOD_PROFILE_VERSION", "v1"),
	}

	var err error
	if cfg.UpstreamTimeout, err = getenvDuration("CSHARP_TIMEOUT", 20*time.Second); err != nil {
		return Config{}, err
	}
	if cfg.JobTimeout, err = getenvDuration("JOB_TIMEOUT", 120*time.Second); err != nil {
		return Config{}, err
	}
	if cfg.StacTimeout, err = getenvDuration("STAC_TIMEOUT", 15*time.Second); err != nil {
		return Config{}, err
	}
	if cfg.MinAnalysisYear, err = getenvInt("GATEWAY_MIN_YEAR", 2019); err != nil {
		return Config{}, err
	}
	if cfg.MaxAnalysisYear, err = getenvInt("GATEWAY_MAX_YEAR", 2024); err != nil {
		return Config{}, err
	}
	if cfg.MaxRequestBodyBytes, err = getenvInt64("GATEWAY_MAX_BODY_BYTES", 2<<20); err != nil {
		return Config{}, err
	}

	cfg.DataRoot, err = filepath.Abs(cfg.DataRoot)
	if err != nil {
		return Config{}, fmt.Errorf("config: resolving DATA_ROOT: %w", err)
	}
	cfg.CacheDir, err = filepath.Abs(cfg.CacheDir)
	if err != nil {
		return Config{}, fmt.Errorf("config: resolving GATEWAY_CACHE_DIR: %w", err)
	}

	return cfg, nil
}

// JobsCacheDir is where the job store persists its on-disk cache.
func (c Config) JobsCacheDir() string { return filepath.Join(c.CacheDir, "jobs") }

// StacCacheDir is where STAC-fallback assets are cached, kept separate from
// the curated Dataset/ folder so a fallback download can never collide with
// or overwrite real dataset files.
func (c Config) StacCacheDir() string { return filepath.Join(c.CacheDir, "stac") }

func getenv(key, def string) string {
	if v, ok := os.LookupEnv(key); ok && v != "" {
		return v
	}
	return def
}

func getenvDuration(key string, def time.Duration) (time.Duration, error) {
	v, ok := os.LookupEnv(key)
	if !ok || v == "" {
		return def, nil
	}
	d, err := time.ParseDuration(v)
	if err != nil {
		return 0, fmt.Errorf("config: invalid %s=%q: %w", key, v, err)
	}
	return d, nil
}

func getenvInt(key string, def int) (int, error) {
	v, ok := os.LookupEnv(key)
	if !ok || v == "" {
		return def, nil
	}
	n, err := strconv.Atoi(v)
	if err != nil {
		return 0, fmt.Errorf("config: invalid %s=%q: %w", key, v, err)
	}
	return n, nil
}

func getenvInt64(key string, def int64) (int64, error) {
	v, ok := os.LookupEnv(key)
	if !ok || v == "" {
		return def, nil
	}
	n, err := strconv.ParseInt(v, 10, 64)
	if err != nil {
		return 0, fmt.Errorf("config: invalid %s=%q: %w", key, v, err)
	}
	return n, nil
}
