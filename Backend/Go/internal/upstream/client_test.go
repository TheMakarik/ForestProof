package upstream

import (
	"context"
	"encoding/json"
	"errors"
	"net/http"
	"net/http/httptest"
	"testing"
	"time"
)

func newTestServer(t *testing.T, handler http.HandlerFunc) *httptest.Server {
	t.Helper()
	server := httptest.NewServer(handler)
	t.Cleanup(server.Close)
	return server
}

func TestCreateAnalysisSuccessPreservesRawBodyAndExtractsStatus(t *testing.T) {
	rawBody := `{"runId":"abc123","status":"Complete","aoiId":"RU_TVER_01","polygonAreaHectares":1750.47}`
	server := newTestServer(t, func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost || r.URL.Path != "/api/v1/analyses" {
			t.Errorf("unexpected request: %s %s", r.Method, r.URL.Path)
		}
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(rawBody))
	})

	client := NewClient(server.URL, server.Client())
	body, status, err := client.CreateAnalysis(context.Background(), CreateAnalysisRequest{
		AoiID: "RU_TVER_01", StartYear: 2019, EndYear: 2024,
	})
	if err != nil {
		t.Fatalf("CreateAnalysis() error = %v", err)
	}
	if status != "Complete" {
		t.Errorf("status = %q, want Complete", status)
	}
	if string(body) != rawBody {
		t.Errorf("body = %q, want raw upstream body preserved verbatim %q", body, rawBody)
	}
}

func TestCreateAnalysis400ReturnsStatusError(t *testing.T) {
	server := newTestServer(t, func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusBadRequest)
		json.NewEncoder(w).Encode(map[string]string{"message": "Площадь запроса больше допустимой."})
	})

	client := NewClient(server.URL, server.Client())
	_, _, err := client.CreateAnalysis(context.Background(), CreateAnalysisRequest{AoiID: "X", StartYear: 2019, EndYear: 2024})
	if err == nil {
		t.Fatal("CreateAnalysis() error = nil, want *StatusError")
	}
	var statusErr *StatusError
	if !errors.As(err, &statusErr) {
		t.Fatalf("error = %v, want *StatusError", err)
	}
	if statusErr.StatusCode != 400 {
		t.Errorf("StatusCode = %d, want 400", statusErr.StatusCode)
	}
	if statusErr.Message != "Площадь запроса больше допустимой." {
		t.Errorf("Message = %q, want the upstream message preserved", statusErr.Message)
	}
}

func TestGetChangesUsesAoiIDInPath(t *testing.T) {
	server := newTestServer(t, func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/api/v1/analyses/RU_TVER_01/changes" {
			t.Errorf("path = %q, want /api/v1/analyses/RU_TVER_01/changes", r.URL.Path)
		}
		w.Write([]byte(`{"type":"FeatureCollection","features":[]}`))
	})

	client := NewClient(server.URL, server.Client())
	body, err := client.GetChanges(context.Background(), "RU_TVER_01", 2019, 2024)
	if err != nil {
		t.Fatalf("GetChanges() error = %v", err)
	}
	if string(body) != `{"type":"FeatureCollection","features":[]}` {
		t.Errorf("body = %q, unexpected", body)
	}
}

func TestGenerateReportReturnsRawPDFBytes(t *testing.T) {
	pdfBytes := []byte("%PDF-1.4 fake report bytes")
	server := newTestServer(t, func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			t.Errorf("method = %s, want POST", r.Method)
		}
		w.Header().Set("Content-Type", "application/pdf")
		w.Write(pdfBytes)
	})

	client := NewClient(server.URL, server.Client())
	got, err := client.GenerateReport(context.Background(), "RU_TVER_01", 2019, 2024)
	if err != nil {
		t.Fatalf("GenerateReport() error = %v", err)
	}
	if string(got) != string(pdfBytes) {
		t.Errorf("got %q, want %q", got, pdfBytes)
	}
}

func TestContextTimeoutPropagates(t *testing.T) {
	release := make(chan struct{})
	server := newTestServer(t, func(w http.ResponseWriter, r *http.Request) {
		<-release
		w.Write([]byte(`{}`))
	})
	t.Cleanup(func() { close(release) })

	client := NewClient(server.URL, server.Client())
	ctx, cancel := context.WithTimeout(context.Background(), 20*time.Millisecond)
	defer cancel()

	_, _, err := client.CreateAnalysis(ctx, CreateAnalysisRequest{AoiID: "X", StartYear: 2019, EndYear: 2024})
	if err == nil {
		t.Fatal("CreateAnalysis() error = nil, want context deadline exceeded")
	}
	if !errors.Is(err, context.DeadlineExceeded) {
		t.Errorf("error = %v, want to wrap context.DeadlineExceeded", err)
	}
}
