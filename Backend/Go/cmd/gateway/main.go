// Command gateway runs the ForestProof Go API gateway: the public /api/v1
// surface, source-catalog/SHA-256 verification, map-layer serving and the
// async analysis-job lifecycle, fronting the C# backend that does the
// actual computation.
package main

import (
	"context"
	"errors"
	"log"
	"net/http"
	"os/signal"
	"syscall"
	"time"

	"forestproof-gateway/internal/api"
	"forestproof-gateway/internal/catalog"
	"forestproof-gateway/internal/config"
	"forestproof-gateway/internal/jobs"
	"forestproof-gateway/internal/stac"
	"forestproof-gateway/internal/upstream"
	"forestproof-gateway/internal/verify"
)

func main() {
	cfg, err := config.Load()
	if err != nil {
		log.Fatalf("config: %v", err)
	}

	ctx, stop := signal.NotifyContext(context.Background(), syscall.SIGINT, syscall.SIGTERM)
	defer stop()

	areas, err := catalog.LoadAreas(cfg.DataRoot)
	if err != nil {
		log.Printf("warning: failed to load areas.csv: %v", err)
	}
	sources, err := catalog.LoadSources(cfg.DataRoot)
	if err != nil {
		log.Printf("warning: failed to load sources.csv: %v", err)
	}
	fileCatalog, err := catalog.LoadFileCatalog(cfg.DataRoot)
	if err != nil {
		log.Printf("warning: failed to load file_catalog.csv: %v", err)
	}
	scenes, err := catalog.LoadScenes(cfg.DataRoot)
	if err != nil {
		log.Printf("warning: failed to load scenes.csv: %v", err)
	}

	scanner := verify.NewScanner()
	go scanner.Run(ctx, cfg.DataRoot, fileCatalog)

	stacClient := stac.NewClient(cfg.StacBaseURL, &http.Client{Timeout: cfg.StacTimeout})
	stacCache := stac.NewCache(cfg.StacCacheDir())

	store, err := jobs.NewStore(cfg.JobsCacheDir())
	if err != nil {
		log.Fatalf("jobs: %v", err)
	}
	upstreamClient := upstream.NewClient(cfg.UpstreamBaseURL, &http.Client{Timeout: cfg.UpstreamTimeout})
	executor := jobs.NewExecutor(store, upstreamClient, cfg.JobTimeout)

	router := api.NewRouter(api.Deps{
		Store:                store,
		Executor:             executor,
		Upstream:             upstreamClient,
		Areas:                areas,
		Sources:              sources,
		FileCatalog:          fileCatalog,
		Scenes:               scenes,
		Verifier:             scanner,
		StacClient:           stacClient,
		StacCache:            stacCache,
		DataRoot:             cfg.DataRoot,
		MethodProfileVersion: cfg.MethodProfileVersion,
		MinAnalysisYear:      cfg.MinAnalysisYear,
		MaxAnalysisYear:      cfg.MaxAnalysisYear,
		MaxRequestBodyBytes:  cfg.MaxRequestBodyBytes,
	})

	server := &http.Server{
		Addr:              cfg.ListenAddr,
		Handler:           router,
		ReadHeaderTimeout: 5 * time.Second,
	}

	go func() {
		<-ctx.Done()
		shutdownCtx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
		defer cancel()
		if err := server.Shutdown(shutdownCtx); err != nil {
			log.Printf("shutdown: %v", err)
		}
	}()

	log.Printf("forestproof-gateway listening on %s (upstream=%s dataRoot=%s)",
		cfg.ListenAddr, cfg.UpstreamBaseURL, cfg.DataRoot)
	if err := server.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
		log.Fatal(err)
	}
}
