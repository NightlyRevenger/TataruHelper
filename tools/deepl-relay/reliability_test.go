package main

import (
	"context"
	"encoding/pem"
	"fmt"
	"io"
	"net"
	"net/http"
	"net/http/httptest"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
	"strconv"
	"strings"
	"sync/atomic"
	"testing"
	"time"
)

func TestRejectUntrustedTLS(t *testing.T) {
	srv := httptest.NewUnstartedServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Write([]byte(`{"result":{"texts":[{"text":"accepted untrusted server"}]}}`))
	}))
	srv.EnableHTTP2 = true
	srv.StartTLS()
	defer srv.Close()
	relay := newRelay(srv.URL, "ios")
	got, status, err := relay.translate(context.Background(), "hello", "auto", "EN")
	if err == nil {
		t.Fatalf("untrusted certificate accepted: status=%d translation=%q", status, got)
	}
}
func TestHandshakeHonorsContext(t *testing.T) {
	listener, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		t.Fatal(err)
	}
	defer listener.Close()
	accepted := make(chan net.Conn, 1)
	go func() {
		conn, e := listener.Accept()
		if e == nil {
			accepted <- conn
		}
	}()
	relay := newRelay("https://www2.deepl.com/jsonrpc", "ios")
	transport := relay.client.Transport.(*http.Transport)
	ctx, cancel := context.WithTimeout(context.Background(), 50*time.Millisecond)
	defer cancel()
	done := make(chan error, 1)
	go func() {
		conn, e := transport.DialTLSContext(ctx, "tcp", listener.Addr().String())
		if conn != nil {
			conn.Close()
		}
		done <- e
	}()
	conn := <-accepted
	defer conn.Close()
	select {
	case <-done:
	case <-time.After(300 * time.Millisecond):
		conn.Close()
		<-done
		t.Fatal("TLS handshake stayed blocked after context deadline")
	}
}

func TestRefusalPauseAndRecovery(t *testing.T) {
	for _, status := range []int{429, 451} {
		t.Run(strconv.Itoa(status), func(t *testing.T) {
			var calls atomic.Int32
			now := time.Now()
			srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
				if calls.Add(1) == 1 {
					w.Header().Set("Retry-After", "120")
					w.WriteHeader(status)
					return
				}
				io.WriteString(w, `{"result":{"texts":[{"text":"ok"}]}}`)
			}))
			defer srv.Close()
			relay := newRelay(srv.URL, "ios")
			relay.now = func() time.Time { return now }
			server := httptest.NewServer(relay.handler())
			defer server.Close()
			for i := 0; i < 2; i++ {
				resp, err := http.Post(server.URL, "application/json", strings.NewReader(`{"text":"hello"}`))
				if err != nil {
					t.Fatal(err)
				}
				resp.Body.Close()
				if resp.StatusCode != status {
					t.Fatalf("status %d", resp.StatusCode)
				}
				retry, _ := strconv.Atoi(resp.Header.Get("Retry-After"))
				if retry < 120 {
					t.Fatalf("Retry-After %q", resp.Header.Get("Retry-After"))
				}
			}
			if calls.Load() != 1 {
				t.Fatalf("pause still made %d upstream calls", calls.Load())
			}
			now = now.Add(121 * time.Second)
			got, _, err := relay.translate(context.Background(), "hello", "auto", "EN")
			if err != nil || got != "ok" || calls.Load() != 2 {
				t.Fatalf("recovery %q %v calls=%d", got, err, calls.Load())
			}
		})
	}
}
func TestRefusalDelay(t *testing.T) {
	now := time.Date(2026, 11, 1, 0, 0, 0, 0, time.UTC)
	for _, tc := range []struct {
		header string
		want   time.Duration
	}{
		{"", time.Minute}, {"garbage", time.Minute}, {"-1", time.Minute}, {"10", time.Minute}, {"120", 2 * time.Minute},
		{now.Add(3 * time.Minute).Format(http.TimeFormat), 3 * time.Minute},
		{now.Add(-time.Minute).Format(http.TimeFormat), time.Minute},
	} {
		if got := refusalDelay(tc.header, now); got != tc.want {
			t.Errorf("%q: %v, want %v", tc.header, got, tc.want)
		}
	}
}
func TestQueuedRequestHonorsCancellation(t *testing.T) {
	relay := newRelay("http://127.0.0.1:1", "ios")
	relay.gate <- struct{}{}
	defer func() { <-relay.gate }()
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Millisecond)
	defer cancel()
	_, _, err := relay.translate(ctx, "hello", "auto", "EN")
	if err != context.DeadlineExceeded {
		t.Fatalf("got %v", err)
	}
}
func TestHandlerCancelsUpstream(t *testing.T) {
	started := make(chan struct{})
	canceled := make(chan struct{})
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		io.Copy(io.Discard, r.Body)
		close(started)
		<-r.Context().Done()
		close(canceled)
	}))
	defer srv.Close()
	relay := newRelay(srv.URL, "ios")
	server := httptest.NewServer(relay.handler())
	defer server.Close()
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()
	req, _ := http.NewRequestWithContext(ctx, "POST", server.URL, strings.NewReader(`{"text":"hello"}`))
	req.Header.Set("Content-Type", "application/json")
	finished := make(chan struct{})
	go func() {
		resp, _ := http.DefaultClient.Do(req)
		if resp != nil {
			resp.Body.Close()
		}
		close(finished)
	}()
	select {
	case <-started:
	case <-time.After(2 * time.Second):
		t.Fatal("upstream did not start")
	}
	cancel()
	select {
	case <-canceled:
	case <-time.After(2 * time.Second):
		t.Fatal("upstream did not receive cancellation")
	}
	<-finished
}
func TestOversizedRequestRejected(t *testing.T) {
	relay := newRelay("http://127.0.0.1:1", "ios")
	rec := httptest.NewRecorder()
	request := httptest.NewRequest("POST", "/", strings.NewReader(`{"text":"`+strings.Repeat("a", 1<<20)+`"}`))
	request.Host = "127.0.0.1:2285"
	request.Header.Set("Content-Type", "application/json")
	relay.handler().ServeHTTP(rec, request)
	if rec.Code != 413 {
		t.Fatalf("got %d", rec.Code)
	}
}

// Use a fresh root pool to trust the temporary CA without disabling verification.
func TestTrustedTLSAndHTTP2(t *testing.T) {
	if runtime.GOOS != "linux" {
		t.Skip("temporary CA uses Linux SSL_CERT_FILE root loading")
	}
	if endpoint := os.Getenv("DEEPL_RELAY_TEST_TLS_ENDPOINT"); endpoint != "" {
		relay := newRelay(endpoint, "ios")
		got, status, err := relay.translate(context.Background(), "hello", "auto", "EN")
		if err != nil || status != 200 || got != "HTTP/2.0" {
			t.Fatalf("got %q/%d, err %v", got, status, err)
		}
		return
	}
	srv := httptest.NewUnstartedServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		fmt.Fprintf(w, `{"result":{"texts":[{"text":%q}]}}`, r.Proto)
	}))
	srv.EnableHTTP2 = true
	srv.StartTLS()
	defer srv.Close()
	cert := pem.EncodeToMemory(&pem.Block{Type: "CERTIFICATE", Bytes: srv.Certificate().Raw})
	certPath := filepath.Join(t.TempDir(), "ca.pem")
	if err := os.WriteFile(certPath, cert, 0600); err != nil {
		t.Fatal(err)
	}
	cmd := exec.Command(os.Args[0], "-test.run=^TestTrustedTLSAndHTTP2$", "-test.timeout=10s")
	cmd.Env = append(os.Environ(), "DEEPL_RELAY_TEST_TLS_ENDPOINT="+srv.URL, "SSL_CERT_FILE="+certPath, "SSL_CERT_DIR="+t.TempDir())
	if out, err := cmd.CombinedOutput(); err != nil {
		t.Fatalf("TLS child: %v\n%s", err, out)
	}
}

func TestHealthDoesNotContactUpstream(t *testing.T) {
	relay := newRelay("http://127.0.0.1:1", "ios")
	rec := httptest.NewRecorder()
	health := httptest.NewRequest("GET", "/health", nil)
	health.Host = "127.0.0.1:2285"
	relay.handler().ServeHTTP(rec, health)
	if rec.Code != 200 || rec.Body.String() != `{"service":"tataruhelper-deepl-relay"}` {
		t.Fatalf("health: %d %s", rec.Code, rec.Body.String())
	}
}

func TestRejectsBrowserSimplePost(t *testing.T) {
	relay := newRelay("http://127.0.0.1:1", "ios")
	rec := httptest.NewRecorder()
	request := httptest.NewRequest("POST", "/", strings.NewReader(`{"text":"hello"}`))
	request.Host = "127.0.0.1:2285"
	request.Header.Set("Content-Type", "text/plain")
	relay.handler().ServeHTTP(rec, request)
	if rec.Code != 415 {
		t.Fatalf("got %d", rec.Code)
	}
}
