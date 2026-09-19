package config

import "testing"

func TestLoadDefaults(t *testing.T) {
	cfg, err := Load()
	if err != nil {
		t.Fatalf("Load() error = %v", err)
	}
	if cfg.ListenAddr != ":8080" {
		t.Errorf("ListenAddr = %q, want :8080", cfg.ListenAddr)
	}
	if cfg.UpstreamBaseURL != "http://localhost:5126" {
		t.Errorf("UpstreamBaseURL = %q, want http://localhost:5126", cfg.UpstreamBaseURL)
	}
	if cfg.MinAnalysisYear != 2019 || cfg.MaxAnalysisYear != 2024 {
		t.Errorf("year range = [%d,%d], want [2019,2024]", cfg.MinAnalysisYear, cfg.MaxAnalysisYear)
	}
}

func TestLoadOverrides(t *testing.T) {
	t.Setenv("GATEWAY_ADDR", ":9090")
	t.Setenv("CSHARP_TIMEOUT", "5s")
	t.Setenv("GATEWAY_MIN_YEAR", "2000")

	cfg, err := Load()
	if err != nil {
		t.Fatalf("Load() error = %v", err)
	}
	if cfg.ListenAddr != ":9090" {
		t.Errorf("ListenAddr = %q, want :9090", cfg.ListenAddr)
	}
	if cfg.UpstreamTimeout.String() != "5s" {
		t.Errorf("UpstreamTimeout = %v, want 5s", cfg.UpstreamTimeout)
	}
	if cfg.MinAnalysisYear != 2000 {
		t.Errorf("MinAnalysisYear = %d, want 2000", cfg.MinAnalysisYear)
	}
}

func TestLoadInvalidDuration(t *testing.T) {
	t.Setenv("CSHARP_TIMEOUT", "not-a-duration")

	_, err := Load()
	if err == nil {
		t.Fatal("Load() error = nil, want error naming CSHARP_TIMEOUT")
	}
}

func TestLoadInvalidInt(t *testing.T) {
	t.Setenv("GATEWAY_MIN_YEAR", "not-a-number")

	_, err := Load()
	if err == nil {
		t.Fatal("Load() error = nil, want error naming GATEWAY_MIN_YEAR")
	}
}
