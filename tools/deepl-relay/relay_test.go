package main

import (
	"context"
	"encoding/json"
	"io"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"
)

func TestAdjustTimestamp(t *testing.T) {
	const now int64 = 1_760_000_000_123

	// No "i": the timestamp goes out untouched, like in the app.
	if got := adjustTimestamp("hello world", now); got != now {
		t.Errorf("no-i: got %d, want %d", got, now)
	}

	// One "i": the timestamp must be a multiple of 2 and at least now.
	got := adjustTimestamp("hello there, my friend.", now)
	if got%2 != 0 || got < now {
		t.Errorf("one-i: got %d, want multiple of 2 at or after %d", got, now)
	}

	// Two "i": multiple of 3.
	got = adjustTimestamp("visiting", now)
	if got%3 != 0 || got < now {
		t.Errorf("two-i: got %d, want multiple of 3 at or after %d", got, now)
	}

	if got := adjustTimestamp("", now); got != now {
		t.Errorf("empty: got %d, want %d", got, now)
	}
}

func TestBuildRequestBodySpacing(t *testing.T) {
	// The endpoint checks how the method key is spaced against the request id.
	// Both arms are pinned, exactly as the app pins them.
	for _, id := range []int64{8_345_678_024, 8_345_678_000} { // (id+5)%29==0, (id+3)%13==0
		body := buildRequestBody("x", "auto", "EN", id, 1_760_000_000_000)
		if !strings.Contains(body, `"method" : "LMT_handle_texts"`) {
			t.Errorf("id %d: want wide spacing, got %s", id, body)
		}
	}

	id := int64(8_345_678_001)
	body := buildRequestBody("x", "auto", "EN", id, 1_760_000_000_000)
	if !strings.Contains(body, `"method": "LMT_handle_texts"`) {
		t.Errorf("id %d: want narrow spacing, got %s", id, body)
	}
}

func TestBuildRequestBodyFields(t *testing.T) {
	body := buildRequestBody("Привет!", "auto", "RU", 8_345_678_001, 1_760_000_000_001)
	var payload struct {
		JSONRPC string `json:"jsonrpc"`
		Method  string `json:"method"`
		ID      int64  `json:"id"`
		Params  struct {
			Texts []struct {
				Text string `json:"text"`
			} `json:"texts"`
			Lang struct {
				Source string `json:"source_lang_user_selected"`
				Target string `json:"target_lang"`
			} `json:"lang"`
			Timestamp int64 `json:"timestamp"`
		} `json:"params"`
	}
	if err := json.Unmarshal([]byte(body), &payload); err != nil {
		t.Fatalf("body is not valid JSON: %v (%s)", err, body)
	}
	if payload.JSONRPC != "2.0" || payload.Method != "LMT_handle_texts" || payload.ID != 8_345_678_001 {
		t.Errorf("envelope wrong: %+v", payload)
	}
	if payload.Params.Texts[0].Text != "Привет!" {
		t.Errorf("text wrong: %q", payload.Params.Texts[0].Text)
	}
	if payload.Params.Lang.Source != "auto" || payload.Params.Lang.Target != "RU" {
		t.Errorf("langs wrong: %+v", payload.Params.Lang)
	}
	if payload.Params.Timestamp != 1_760_000_000_001 {
		t.Errorf("timestamp wrong: %d", payload.Params.Timestamp)
	}
}

func TestParseTranslation(t *testing.T) {
	ok := `{"jsonrpc":"2.0","id":1,"result":{"texts":[{"text":"Привет","alternatives":[]}],"lang":"RU"}}`
	got, err := parseTranslation(ok)
	if err != nil || got != "Привет" {
		t.Errorf("got %q, err %v", got, err)
	}

	multi := `{"jsonrpc":"2.0","result":{"texts":[{"text":"a"},{"text":"b"}]}}`
	got, err = parseTranslation(multi)
	if err != nil || got != "ab" {
		t.Errorf("multi: got %q, err %v", got, err)
	}

	if got, err := parseTranslation(`{"jsonrpc":"2.0","error":{"code":-32602}}`); err == nil || got != "" {
		t.Errorf("error payload: got %q, err %v", got, err)
	}

	if got, err := parseTranslation(`{"result":{"texts":[]}}`); err != nil || got != "" {
		t.Errorf("empty texts: got %q, err %v", got, err)
	}
}

func TestTranslateMapsLanguages(t *testing.T) {
	var seen string
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		body, _ := io.ReadAll(r.Body)
		seen = string(body)
		w.Header().Set("Content-Type", "application/json")
		io.WriteString(w, `{"jsonrpc":"2.0","result":{"texts":[{"text":"ok"}]}}`)
	}))
	defer srv.Close()

	r := newRelay(srv.URL, "safari")
	got, status, err := r.translate(context.Background(), "test", "JA", "ru")
	if err != nil || status != 200 || got != "ok" {
		t.Fatalf("got %q/%d, err %v", got, status, err)
	}
	if !strings.Contains(seen, `"source_lang_user_selected":"ja"`) {
		t.Errorf("source should be lowercased, body: %s", seen)
	}
	if !strings.Contains(seen, `"target_lang":"RU"`) {
		t.Errorf("target should be uppercased, body: %s", seen)
	}
	if !strings.Contains(seen, `"method": "LMT_handle_texts"`) && !strings.Contains(seen, `"method" : "`) {
		t.Errorf("method key missing spacing rule: %s", seen)
	}
}

func TestTranslatePassesRefusalStatus(t *testing.T) {
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(429)
		io.WriteString(w, `{ "code": 429, "message": "Too many requests." }`)
	}))
	defer srv.Close()

	r := newRelay(srv.URL, "safari")
	_, status, err := r.translate(context.Background(), "test", "auto", "EN")
	if status != 429 || err == nil {
		t.Fatalf("want 429 and an error, got %d, %v", status, err)
	}
}

func TestHandlerDeepLXProtocol(t *testing.T) {
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		io.WriteString(w, `{"jsonrpc":"2.0","result":{"texts":[{"text":"Привет, друг"}]}}`)
	}))
	defer srv.Close()

	r := newRelay(srv.URL, "safari")
	server := httptest.NewServer(r.handler())
	defer server.Close()

	resp, err := http.Post(server.URL, "application/json", strings.NewReader(`{"text":"Hello, friend","source_lang":"auto","target_lang":"ru"}`))
	if err != nil {
		t.Fatal(err)
	}
	defer resp.Body.Close()
	body, _ := io.ReadAll(resp.Body)

	var out struct {
		Code int    `json:"code"`
		Data string `json:"data"`
	}
	if err := json.Unmarshal(body, &out); err != nil {
		t.Fatalf("not JSON: %s", body)
	}
	if resp.StatusCode != 200 || out.Code != 200 || out.Data != "Привет, друг" {
		t.Errorf("got %d/%+v, body %s", resp.StatusCode, out, body)
	}
}

func TestHandlerRejectsEmptyText(t *testing.T) {
	r := newRelay("http://127.0.0.1:1", "safari")
	server := httptest.NewServer(r.handler())
	defer server.Close()

	resp, err := http.Post(server.URL, "application/json", strings.NewReader(`{"text":"","target_lang":"ru"}`))
	if err != nil {
		t.Fatal(err)
	}
	defer resp.Body.Close()
	if resp.StatusCode != 400 {
		t.Errorf("want 400, got %d", resp.StatusCode)
	}
}

func TestHandlerPassesRefusalToCaller(t *testing.T) {
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(429)
		io.WriteString(w, `Too many requests.`)
	}))
	defer srv.Close()

	r := newRelay(srv.URL, "safari")
	server := httptest.NewServer(r.handler())
	defer server.Close()

	resp, err := http.Post(server.URL, "application/json", strings.NewReader(`{"text":"x","target_lang":"ru"}`))
	if err != nil {
		t.Fatal(err)
	}
	defer resp.Body.Close()
	body, _ := io.ReadAll(resp.Body)
	if resp.StatusCode != 429 {
		t.Errorf("want 429, got %d (%s)", resp.StatusCode, body)
	}
	if !strings.Contains(string(body), "upstream requests paused") {
		t.Errorf("message not passed through: %s", body)
	}
}

func TestHandlerRejectsForeignHost(t *testing.T) {
	r := newRelay("http://127.0.0.1:1", "safari")
	h := r.handler()
	for host, want := range map[string]int{
		"evil.example":      http.StatusForbidden,
		"evil.example:2285": http.StatusForbidden,
		"127.0.0.1.evil.io": http.StatusForbidden,
		"127.0.0.1:2285":    http.StatusOK,
		"localhost:2285":    http.StatusOK,
		"[::1]:2285":        http.StatusOK,
	} {
		req := httptest.NewRequest(http.MethodGet, "/health", nil)
		req.Host = host
		rec := httptest.NewRecorder()
		h.ServeHTTP(rec, req)
		if rec.Code != want {
			t.Errorf("Host %q: status %d, want %d", host, rec.Code, want)
		}
	}
}

// versionNotice is what DeepL's free endpoint answers HTTP 200 with in place
// of a translation since 2 November 2026 - reproduced live through this relay,
// the same text whatever target language was asked for. It means the request
// shape is retired, so the only honest reading is a refusal; the app matches
// the same sentences in Translation/Providers/DeepL/DeepLVersionNotice.cs.
const versionNotice = "You're using an older version of DeepL. To continue using the app, " +
	"please update to the latest version.\n\n" +
	"Sie verwenden eine ältere Version von DeepL. Bitte aktualisieren Sie die App, " +
	"um sie weiterhin nutzen zu können.\n\n" +
	"Вы используете старую версию DeepL. Чтобы продолжить работу с приложением, " +
	"обновите его до последней версии."

func TestIsVersionNotice(t *testing.T) {
	if !isVersionNotice(versionNotice) {
		t.Error("the multilingual blob must be recognized")
	}

	// Each language on its own, in case DeepL starts answering only in the
	// language that was asked for.
	for _, line := range strings.Split(versionNotice, "\n\n") {
		if !isVersionNotice(line) {
			t.Errorf("single language not recognized: %q", line)
		}
	}

	for _, translation := range []string{"", "Hello, friend", "Привет, друг", "こんにちは世界", "Haritanın eski bir sürümünü buldum"} {
		if isVersionNotice(translation) {
			t.Errorf("ordinary translation read as the notice: %q", translation)
		}
	}
}

func TestTranslateRefusesVersionNoticeLikeTooManyRequests(t *testing.T) {
	requests := 0
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		requests++
		w.Header().Set("Content-Type", "application/json")
		io.WriteString(w, `{"jsonrpc":"2.0","result":{"texts":[{"text":`+jsonText(versionNotice)+`}]}}`)
	}))
	defer srv.Close()

	r := newRelay(srv.URL, "safari")
	now := time.Date(2026, 11, 2, 20, 0, 0, 0, time.UTC)
	r.now = func() time.Time { return now }

	got, status, err := r.translate(context.Background(), "hello", "auto", "EN")
	if err == nil || status != http.StatusTooManyRequests || got != "" {
		t.Fatalf("want the notice refused as 429, got %q/%d, err %v", got, status, err)
	}

	// While the pause stands, the next line is refused without knocking again.
	_, status, err = r.translate(context.Background(), "hello again", "auto", "EN")
	if err == nil || status != http.StatusTooManyRequests {
		t.Errorf("during the pause want 429, got %d, err %v", status, err)
	}
	if requests != 1 {
		t.Errorf("the pause must spare upstream the second request, saw %d", requests)
	}

	// The pause expires like any other refusal's, and the endpoint is tried again.
	now = now.Add(time.Minute + time.Second)
	_, status, err = r.translate(context.Background(), "hello once more", "auto", "EN")
	if err == nil || status != http.StatusTooManyRequests {
		t.Errorf("after the pause want a fresh attempt refused, got %d, err %v", status, err)
	}
	if requests != 2 {
		t.Errorf("want a second upstream attempt after the pause, saw %d", requests)
	}
}

func TestHandlerReportsVersionNoticeAsTooManyRequests(t *testing.T) {
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		io.WriteString(w, `{"jsonrpc":"2.0","result":{"texts":[{"text":`+jsonText(versionNotice)+`}]}}`)
	}))
	defer srv.Close()

	r := newRelay(srv.URL, "safari")
	server := httptest.NewServer(r.handler())
	defer server.Close()

	resp, err := http.Post(server.URL, "application/json", strings.NewReader(`{"text":"Hello, friend","target_lang":"ru"}`))
	if err != nil {
		t.Fatal(err)
	}
	defer resp.Body.Close()
	body, _ := io.ReadAll(resp.Body)

	if resp.StatusCode != http.StatusTooManyRequests {
		t.Errorf("want 429 to the application, got %d (%s)", resp.StatusCode, body)
	}
	if resp.Header.Get("Retry-After") == "" {
		t.Errorf("the refusal should carry a Retry-After, body: %s", body)
	}
	if !strings.Contains(string(body), "client-update notice") {
		t.Errorf("the message should name the notice, body: %s", body)
	}
}
