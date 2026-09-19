package api

import (
	"encoding/json"
	"net/http"
)

// ErrorCode is a stable machine-readable error identifier, distinct from
// the human-readable message, so API consumers can branch on it without
// string-matching the message.
type ErrorCode string

const (
	ErrInvalidRequest ErrorCode = "invalid_request" // 400
	ErrNotFound       ErrorCode = "not_found"       // 404
	ErrConflict       ErrorCode = "conflict"        // 409
	ErrUpstream       ErrorCode = "upstream_error"  // 502
	ErrTimeout        ErrorCode = "timeout"         // 504
	ErrInternal       ErrorCode = "internal"        // 500
)

type errorEnvelope struct {
	Error errorBody `json:"error"`
}

type errorBody struct {
	Code    ErrorCode `json:"code"`
	Message string    `json:"message"`
}

// WriteError writes the gateway's single consistent error envelope:
// {"error": {"code": "...", "message": "..."}}.
func WriteError(w http.ResponseWriter, status int, code ErrorCode, message string) {
	WriteJSON(w, status, errorEnvelope{Error: errorBody{Code: code, Message: message}})
}

// WriteJSON writes v as a JSON response with the given status code.
func WriteJSON(w http.ResponseWriter, status int, v any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(v)
}
