package api

import (
	"errors"
	"net/http"

	"forestproof-gateway/internal/upstream"
)

func (d Deps) handleGetAreas(w http.ResponseWriter, r *http.Request) {
	body, err := d.Upstream.GetAreas(r.Context())
	if err != nil {
		writeUpstreamError(w, err)
		return
	}
	w.Header().Set("Content-Type", "application/json")
	w.Write(body)
}

func (d Deps) handleGetProjects(w http.ResponseWriter, r *http.Request) {
	body, err := d.Upstream.GetProjects(r.Context())
	if err != nil {
		writeUpstreamError(w, err)
		return
	}
	w.Header().Set("Content-Type", "application/json")
	w.Write(body)
}

// writeUpstreamError maps a failure calling the C# backend to a response:
// the C#-reported status/message if it rejected the request, or a generic
// 502 if the upstream couldn't be reached at all.
func writeUpstreamError(w http.ResponseWriter, err error) {
	var statusErr *upstream.StatusError
	if errors.As(err, &statusErr) {
		WriteError(w, statusErr.StatusCode, ErrUpstream, statusErr.Message)
		return
	}
	WriteError(w, http.StatusBadGateway, ErrUpstream, "upstream request failed: "+err.Error())
}
