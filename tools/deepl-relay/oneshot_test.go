package main

import (
	"bytes"
	"compress/gzip"
	"context"
	"encoding/json"
	"io"
	"net/http"
	"net/http/httptest"
	"regexp"
	"strings"
	"testing"
	"time"
)

// The app route answers through DeepL's own oneshot endpoint with a coherent
// iOS client profile - the free web jsonrpc route has been answering the
// client-update notice since 2 November 2026, so this is the route that
// translates again.

var uuidPattern = regexp.MustCompile(`^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$`)

func gzipBody(s string) []byte {
	var buf bytes.Buffer
	w := gzip.NewWriter(&buf)
	_, _ = io.WriteString(w, s)
	_ = w.Close()
	return buf.Bytes()
}

// webServer answers the jsonrpc route; counting knocks proves which route the
// relay used.
func webServer(t *testing.T, answer string) (*httptest.Server, *int) {
	knocks := 0
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		knocks++
		w.Header().Set("Content-Type", "application/json")
		io.WriteString(w, answer)
	}))
	t.Cleanup(srv.Close)
	return srv, &knocks
}

func appServer(t *testing.T, handler http.HandlerFunc) (*httptest.Server, *[]http.Header, *[]string) {
	var headers []http.Header
	var bodies []string
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		body, _ := io.ReadAll(r.Body)
		headers = append(headers, r.Header.Clone())
		bodies = append(bodies, string(body))
		handler(w, r)
	}))
	t.Cleanup(srv.Close)
	return srv, &headers, &bodies
}

func translatingApp() http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		w.Header().Set("Content-Encoding", "gzip")
		_, _ = w.Write(gzipBody(`{"translations":[{"detected_source_language":"en","text":"Привет, друг!"}]}`))
	}
}

func newAppRelay(t *testing.T, webURL, appURL string) *deeplRelay {
	r := newRelay(webURL, "ios")
	r.oneshotURL = appURL
	return r
}

func TestOneshotRouteAnswersThroughTheAppEndpoint(t *testing.T) {
	web, webKnocks := webServer(t, `{"jsonrpc":"2.0","result":{"texts":[{"text":"not this one"}]}}`)
	app, headers, bodies := appServer(t, translatingApp())
	r := newAppRelay(t, web.URL, app.URL)

	got, status, err := r.translate(context.Background(), "Hello, friend!", "AUTO", "ru")
	if err != nil || status != http.StatusOK || got != "Привет, друг!" {
		t.Fatalf("got %q/%d err %v", got, status, err)
	}
	if *webKnocks != 0 {
		t.Errorf("the app route answered; the jsonrpc route should not have been knocked: %d knocks", *webKnocks)
	}

	h := (*headers)[0]
	if ua := h.Get("User-Agent"); !strings.HasPrefix(ua, "DeepL/") || !strings.Contains(ua, "CFNetwork/") {
		t.Errorf("User-Agent is not the iOS app's: %q", ua)
	}
	if v := h.Get("x-app-os-version"); v != iosOSVersion {
		t.Errorf("x-app-os-version %q, want %q", v, iosOSVersion)
	}
	if v := h.Get("Authorization"); v != "None" {
		t.Errorf("Authorization %q, want None", v)
	}
	if v := h.Get("x-app-instance-id"); !uuidPattern.MatchString(v) {
		t.Errorf("x-app-instance-id is not a UUID: %q", v)
	}
	if v := h.Get("x-app-session-id"); !uuidPattern.MatchString(v) {
		t.Errorf("x-app-session-id is not a UUID: %q", v)
	}

	body := (*bodies)[0]
	for _, want := range []string{
		`"text":["Hello, friend!"]`,
		`"target_lang":"ru"`,
		`"usage_type":"translate"`,
		`"os":"iOS"`,
		`"os_version":"` + iosOSVersion + `"`,
		`"app_version":"` + iosAppVersion + `"`,
		`"app_build":"` + iosAppBuild + `"`,
	} {
		if !strings.Contains(body, want) {
			t.Errorf("request body misses %s: %s", want, body)
		}
	}
	if strings.Contains(body, `"source_lang"`) {
		t.Errorf("auto source must not be sent as a source_lang: %s", body)
	}

	// The instance and session read like one app launch across requests.
	_, _, err = r.translate(context.Background(), "again", "auto", "ru")
	if err != nil {
		t.Fatal(err)
	}
	if (*headers)[1].Get("x-app-instance-id") != (*headers)[0].Get("x-app-instance-id") {
		t.Error("the instance id changed between requests")
	}
	if (*headers)[1].Get("x-app-session-id") != (*headers)[0].Get("x-app-session-id") {
		t.Error("the session id changed between requests")
	}
	if bodyInstanceID(*bodies)[1] != (*headers)[0].Get("x-app-instance-id") {
		t.Error("the body's instance_id must be the header's stable one")
	}
}

func bodyInstanceID(bodies []string) []string {
	ids := make([]string, len(bodies))
	for i, body := range bodies {
		var req struct {
			AppInformation struct {
				InstanceID string `json:"instance_id"`
			} `json:"app_information"`
		}
		if err := json.Unmarshal([]byte(body), &req); err != nil {
			continue
		}
		ids[i] = req.AppInformation.InstanceID
	}
	return ids
}

func TestOneshotLanguageMapping(t *testing.T) {
	cases := []struct{ send, wantTarget, wantSource string }{
		{"EN", "en-US", ""},
		{"RU", "ru", ""},
		{"PT", "pt-BR", ""},
		{"PT-PT", "pt-PT", ""},
		{"ZH", "zh-Hans", ""},
		{"ZH-HANT", "zh-Hant", ""},
		{"JA", "ja", ""},
	}
	for _, c := range cases {
		web, webKnocks := webServer(t, `{"result":{"texts":[{"text":"web fallback"}]}}`)
		app, _, bodies := appServer(t, translatingApp())
		r := newAppRelay(t, web.URL, app.URL)
		if _, _, err := r.translate(context.Background(), "hi", "auto", c.send); err != nil {
			t.Errorf("%s: %v", c.send, err)
			continue
		}
		if *webKnocks != 0 {
			t.Errorf("target %s should map to the app route", c.send)
		}
		if !strings.Contains((*bodies)[0], `"target_lang":"`+c.wantTarget+`"`) {
			t.Errorf("target %s: want %s in body: %s", c.send, c.wantTarget, (*bodies)[0])
		}
	}

	// Sources are sent in their generic forms; auto is omitted.
	for source, want := range map[string]string{"EN": "en", "PT": "pt", "ZH": "zh"} {
		web, _ := webServer(t, `{"result":{"texts":[{"text":"web fallback"}]}}`)
		app, _, bodies := appServer(t, translatingApp())
		r := newAppRelay(t, web.URL, app.URL)
		if _, _, err := r.translate(context.Background(), "hi", source, "EN"); err != nil {
			t.Errorf("source %s: %v", source, err)
			continue
		}
		if !strings.Contains((*bodies)[0], `"source_lang":"`+want+`"`) {
			t.Errorf("source %s: want %s in body: %s", source, want, (*bodies)[0])
		}
	}
}

func TestOneshotTooManyRequestsPausesUpstream(t *testing.T) {
	app, appHeaders, _ := appServer(t, func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Retry-After", "120")
		w.WriteHeader(http.StatusTooManyRequests)
		io.WriteString(w, `{"code":429,"message":"Too many requests."}`)
	})
	web, webKnocks := webServer(t, `{"result":{"texts":[{"text":"no"}]}}`)
	r := newAppRelay(t, web.URL, app.URL)
	now := time.Date(2026, 11, 2, 21, 0, 0, 0, time.UTC)
	r.now = func() time.Time { return now }

	_, status, err := r.translate(context.Background(), "hello", "auto", "RU")
	if err == nil || status != http.StatusTooManyRequests {
		t.Fatalf("want 429, got %d err %v", status, err)
	}

	// The pause answers from the relay; neither route is knocked.
	_, status, err = r.translate(context.Background(), "hello again", "auto", "RU")
	if err == nil || status != http.StatusTooManyRequests {
		t.Errorf("during the pause want 429, got %d err %v", status, err)
	}
	if len(*appHeaders) != 1 {
		t.Errorf("the pause must spare the app route too, saw %d knocks", len(*appHeaders))
	}
	if *webKnocks != 0 {
		t.Errorf("the pause must spare the jsonrpc route, saw %d knocks", *webKnocks)
	}

	// After the Retry-After, the app route is knocked again.
	now = now.Add(2 * time.Minute)
	if _, _, err := r.translate(context.Background(), "hello later", "auto", "RU"); err == nil {
		t.Error("want the 429 again after the pause")
	}
	if len(*appHeaders) != 2 {
		t.Errorf("want a fresh app knock after the pause, saw %d", len(*appHeaders))
	}
}

func TestOneshotForbiddenStandsDownFromTheAppRoute(t *testing.T) {
	appKnocks := 0
	app, _, _ := appServer(t, func(w http.ResponseWriter, r *http.Request) {
		appKnocks++
		w.WriteHeader(http.StatusForbidden)
		io.WriteString(w, `{"title":"OutdatedClient"}`)
	})
	web, webKnocks := webServer(t, `{"jsonrpc":"2.0","result":{"texts":[{"text":"ok from web"}]}}`)
	r := newAppRelay(t, web.URL, app.URL)
	now := time.Date(2026, 11, 2, 21, 0, 0, 0, time.UTC)
	r.now = func() time.Time { return now }

	_, status, err := r.translate(context.Background(), "hello", "auto", "RU")
	if err == nil || status != http.StatusTooManyRequests {
		t.Fatalf("a refused client should pause upstream like a 429, got %d err %v", status, err)
	}
	if !strings.Contains(err.Error(), "client") {
		t.Errorf("the refusal should name the client problem: %v", err)
	}

	// Once the pause stands down, the relay stops knocking the app route and
	// the jsonrpc route carries the line.
	now = now.Add(time.Minute + time.Second)
	got, status, err := r.translate(context.Background(), "hello once more", "auto", "RU")
	if err != nil || status != http.StatusOK || got != "ok from web" {
		t.Fatalf("after the pause want the web route to answer, got %q/%d err %v", got, status, err)
	}
	if appKnocks != 1 {
		t.Errorf("the app route should be retired after its 403, saw %d knocks", appKnocks)
	}
	if *webKnocks != 1 {
		t.Errorf("want one jsonrpc knock, saw %d", *webKnocks)
	}
}

func TestWebNoticeDoesNotPauseTheAppRoute(t *testing.T) {
	// The client-update notice is about the jsonrpc protocol; the app route
	// answers under a different profile and must keep working while the web
	// route stands down.
	web, webKnocks := webServer(t, `{"jsonrpc":"2.0","result":{"texts":[{"text":`+jsonText(versionNotice)+`}]}}`)
	app, appHeaders, _ := appServer(t, translatingApp())
	r := newAppRelay(t, web.URL, app.URL)

	// A line past the app route's cap goes to the web route and is refused.
	long := strings.Repeat("汉", maxOneshotTextUnits+1)
	if _, status, err := r.translate(context.Background(), long, "auto", "RU"); err == nil || status != http.StatusTooManyRequests {
		t.Fatalf("want the web notice refused as 429, got %d err %v", status, err)
	}

	// A short line still translates through the app route.
	got, status, err := r.translate(context.Background(), "Hello, friend!", "auto", "RU")
	if err != nil || status != http.StatusOK || got != "Привет, друг!" {
		t.Fatalf("the app route must not pause with the web route, got %q/%d err %v", got, status, err)
	}
	if len(*appHeaders) != 1 || *webKnocks != 1 {
		t.Errorf("want one knock each: app %d, web %d", len(*appHeaders), *webKnocks)
	}

	// The next over-long line is refused from the pause, without a second
	// knock on the web route.
	if _, status, err := r.translate(context.Background(), long, "auto", "RU"); err == nil || status != http.StatusTooManyRequests {
		t.Errorf("during the web pause want 429 without a knock, got %d err %v", status, err)
	}
	if *webKnocks != 1 {
		t.Errorf("the web pause must spare the web route repeats, saw %d", *webKnocks)
	}
}

func TestOneshotFallsBackToTheWebRoute(t *testing.T) {
	answers := []struct {
		name    string
		handler http.HandlerFunc
	}{
		{"no translations", func(w http.ResponseWriter, r *http.Request) {
			w.Header().Set("Content-Type", "application/json")
			io.WriteString(w, `{"translations":[]}`)
		}},
		{"empty text", func(w http.ResponseWriter, r *http.Request) {
			w.Header().Set("Content-Type", "application/json")
			io.WriteString(w, `{"translations":[{"text":""}]}`)
		}},
		{"server error", func(w http.ResponseWriter, r *http.Request) {
			w.WriteHeader(http.StatusInternalServerError)
			io.WriteString(w, `oops`)
		}},
	}
	for _, a := range answers {
		web, webKnocks := webServer(t, `{"jsonrpc":"2.0","result":{"texts":[{"text":"web answered"}]}}`)
		app, _, _ := appServer(t, a.handler)
		r := newAppRelay(t, web.URL, app.URL)
		got, status, err := r.translate(context.Background(), "hello", "auto", "RU")
		if err != nil || status != http.StatusOK || got != "web answered" {
			t.Errorf("%s: want the web route to cover the app route, got %q/%d err %v", a.name, got, status, err)
		}
		if *webKnocks != 1 {
			t.Errorf("%s: want one jsonrpc knock, saw %d", a.name, *webKnocks)
		}
	}
}

func TestOneshotSkipsLanguagesAndLengthsItCannotServe(t *testing.T) {
	web, webKnocks := webServer(t, `{"jsonrpc":"2.0","result":{"texts":[{"text":"web answered"}]}}`)
	app, appHeaders, _ := appServer(t, translatingApp())
	r := newAppRelay(t, web.URL, app.URL)

	for _, text := range []string{
		strings.Repeat("汉", maxOneshotTextUnits+1),   // 1501 UTF-16 units
		strings.Repeat("😀", maxOneshotTextUnits/2+1), // 1502 units: an astral emoji counts two
	} {
		if _, _, err := r.translate(context.Background(), text, "auto", "RU"); err != nil {
			t.Fatal(err)
		}
	}
	// An unmapped target language also goes to the web route.
	if _, _, err := r.translate(context.Background(), strings.Repeat("汉", maxOneshotTextUnits), "auto", "LA"); err != nil {
		t.Fatal(err)
	}
	if len(*appHeaders) != 0 {
		t.Errorf("the app route must not see over-long lines or unmapped languages, saw %d knocks", len(*appHeaders))
	}
	if *webKnocks != 3 {
		t.Errorf("want 3 jsonrpc knocks, saw %d", *webKnocks)
	}
}
