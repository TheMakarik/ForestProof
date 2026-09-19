package api

import (
	"net/http"

	"forestproof-gateway/internal/verify"
)

func (d Deps) handleGetSources(w http.ResponseWriter, r *http.Request) {
	filesBySource := make(map[string][]sourceFileInfo)
	for _, entry := range d.FileCatalog {
		result := d.verifyResultFor(entry.RelativePath)
		info := sourceFileInfo{
			RelativePath:   entry.RelativePath,
			AoiID:          entry.AoiID,
			Sha256Recorded: entry.Sha256Recorded,
			Sha256Actual:   result.ActualSHA256,
			Verified:       result.Status,
			SizeBytes:      entry.SizeBytes,
		}
		for _, sourceID := range entry.SourceIDs {
			filesBySource[sourceID] = append(filesBySource[sourceID], info)
		}
	}

	responses := make([]sourceResponse, 0, len(d.Sources))
	for _, source := range d.Sources {
		responses = append(responses, sourceResponse{
			SourceID:            source.SourceID,
			Product:             source.Product,
			Version:             source.Version,
			LicenseURL:          source.LicenseURL,
			RequiredAttribution: source.RequiredAttribution,
			Files:               filesBySource[source.SourceID],
		})
	}

	WriteJSON(w, http.StatusOK, responses)
}

func (d Deps) verifyResultFor(relativePath string) verify.Result {
	if d.Verifier == nil {
		return verify.Result{Status: verify.StatusPending}
	}
	return d.Verifier.Result(relativePath)
}
