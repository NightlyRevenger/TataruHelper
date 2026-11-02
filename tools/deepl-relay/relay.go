// Package main implements a small local relay that lets TataruHelper reach
// DeepL's free translation without a browser.
//
// Two routes reach DeepL. The app route posts to the oneshot endpoint DeepL's
// own clients use, under a pinned iOS profile; it translates today. The web
// route speaks the legacy jsonrpc protocol with a browser TLS fingerprint;
// since 2 November 2026 the free endpoint answers that protocol with a
// client-update notice instead of translations, so it stands as the fallback.
// These are unofficial endpoints; the observations behind them do not
// establish which signals DeepL's filter checks.
package main

import (
	"compress/flate"
	"compress/gzip"
	"context"
	crand "crypto/rand"
	cryptotls "crypto/tls"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"log"
	"math/rand"
	"mime"
	"net"
	"net/http"
	"strconv"
	"strings"
	"sync/atomic"
	"time"
	"unicode/utf16"

	"github.com/andybalholm/brotli"
	utls "github.com/refraction-networking/utls"
)

const maxRefusalExcerpt = 200

// The emulated iOS profile is pinned to the shipping DeepL app (26.42), the
// same one OwO-Network/DLX and un-ts/deeplx pin. DeepL rate-limits clients
// whose TLS fingerprint, User-Agent and app_information tell inconsistent
// stories, so every value stays on this one profile - and a claimed version
// beyond a shipping iOS major earns HTTP 429 and can temporarily ban the IP.
const (
	oneshotEndpoint     = "https://oneshot-free.www.deepl.com/v1/translate"
	iosAppVersion       = "26.42"
	iosAppBuild         = "5443737"
	iosOSVersion        = "26.0"
	iosCFNetworkVersion = "3826.600.41"
	iosDarwinVersion    = "25.0.0"

	// The anonymous oneshot endpoint caps the sum of the texts at 1500 UTF-16
	// code units; an astral character counts two.
	maxOneshotTextUnits = 1500
)

// deeplRelay serves the DeepLX protocol and picks the upstream route.
type deeplRelay struct {
	endpoint    string
	oneshotURL  string
	client      *http.Client // the web route: browser TLS fingerprint
	apiClient   *http.Client // the app route: ordinary TLS, iOS headers
	userAgent   string
	id          int64
	logRequests bool
	gate        chan struct{}
	refusal     *refusalError // protected by gate, including upstream requests
	webRefusal  *refusalError // jsonrpc notice: pauses the web route only
	now         func() time.Time
	instanceID  string // stable for the process, like one app launch
	sessionID   string
	skipOneshot bool // the app route refused this client; retire it
}

// browserProfile pairs a TLS hello with the user-agent that belongs to it.
type browserProfile struct {
	helloID   utls.ClientHelloID
	userAgent string
}

var profiles = map[string]browserProfile{
	"ios": {
		helloID:   utls.HelloIOS_14,
		userAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0 Mobile/15E148 Safari/604.1",
	},
	"safari": {
		helloID:   utls.HelloSafari_16_0,
		userAgent: "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Safari/605.1.15",
	},
}

func newRelay(endpoint, profile string) *deeplRelay {
	p, ok := profiles[profile]
	if !ok {
		p = profiles["ios"]
	}

	r := &deeplRelay{
		endpoint:   endpoint,
		gate:       make(chan struct{}, 1),
		now:        time.Now,
		userAgent:  p.userAgent,
		id:         int64(rand.Intn(100_000)+8_300_000) * 1000,
		apiClient:  &http.Client{Timeout: 20 * time.Second},
		instanceID: newUUID(),
		sessionID:  newUUID(),
	}

	if strings.HasPrefix(endpoint, "https://") {
		dialTLS := func(ctx context.Context, network, addr string) (net.Conn, error) {
			ctx, cancel := context.WithTimeout(ctx, 10*time.Second)
			defer cancel()
			d := net.Dialer{Timeout: 10 * time.Second}
			raw, err := d.DialContext(ctx, network, addr)
			if err != nil {
				return nil, err
			}
			host, _, err := net.SplitHostPort(addr)
			if err != nil {
				raw.Close()
				return nil, err
			}
			u := utls.UClient(raw, &utls.Config{
				ServerName: host,
				NextProtos: []string{"h2", "http/1.1"},
			}, p.helloID)
			if err := u.HandshakeContext(ctx); err != nil {
				raw.Close()
				return nil, err
			}
			// net/http only upgrades to HTTP/2 when the dialed connection
			// reports a crypto/tls state; uTLS reports its own. The wrapper
			// hands over the negotiated protocol so the relay speaks HTTP/2
			// like a real browser instead of HTTP/1.1 over an h2 connection.
			return browserConn{u}, nil
		}
		r.client = &http.Client{
			Transport: &http.Transport{
				DialTLSContext:        dialTLS,
				ForceAttemptHTTP2:     true,
				DisableCompression:    true,
				IdleConnTimeout:       90 * time.Second,
				MaxIdleConnsPerHost:   2,
				ResponseHeaderTimeout: 15 * time.Second,
			},
			Timeout: 20 * time.Second,
		}
	} else {
		r.client = &http.Client{Timeout: 20 * time.Second}
	}
	return r
}

// browserConn makes a uTLS connection look like a crypto/tls one to net/http.
type browserConn struct{ *utls.UConn }

func (c browserConn) ConnectionState() cryptotls.ConnectionState {
	st := c.UConn.ConnectionState()
	proto := ""
	if st.NegotiatedProtocol == "h2" {
		proto = "h2"
	}
	return cryptotls.ConnectionState{
		Version:            st.Version,
		NegotiatedProtocol: proto,
		ServerName:         st.ServerName,
	}
}

// adjustTimestamp reproduces the rule the endpoint checks: the timestamp must
// divide evenly by the number of "i" in the text plus one. Same as
// DeepLTranslator.AdjustTimestamp in the app.
func adjustTimestamp(text string, nowMilliseconds int64) int64 {
	iCount := strings.Count(text, "i")
	if iCount == 0 {
		return nowMilliseconds
	}
	k := int64(iCount + 1)
	return nowMilliseconds - nowMilliseconds%k + k
}

// buildRequestBody spells the request the way the endpoint expects, including
// the "method" key spacing that is checked against the request id. Same rule
// as DeepLTranslator.BuildRequestBody in the app.
func buildRequestBody(text, sourceLang, targetLang string, id int64, nowMilliseconds int64) string {
	method := `"method": "`
	if (id+5)%29 == 0 || (id+3)%13 == 0 {
		method = `"method" : "`
	}
	return `{"jsonrpc":"2.0",` + method + `LMT_handle_texts","id":` + fmt.Sprint(id) +
		`,"params":{"texts":[{"text":` + jsonText(text) + `,"requestAlternatives":0}],` +
		`"splitting":"newlines","lang":{"source_lang_user_selected":` + jsonText(sourceLang) +
		`,"target_lang":` + jsonText(targetLang) +
		`},"timestamp":` + fmt.Sprint(adjustTimestamp(text, nowMilliseconds)) +
		`,"commonJobParams":{"wasSpoken":false,"transcribe_as":""}}}`
}

// jsonText encodes a string as JSON, keeping non-ASCII readable the way
// Newtonsoft does; either spelling is valid JSON, DeepL accepts both.
func jsonText(s string) string {
	b, err := json.Marshal(s)
	if err != nil {
		return `""`
	}
	return string(b)
}

// parseTranslation takes the translated text out of a jsonrpc answer.
func parseTranslation(body string) (string, error) {
	var answer struct {
		Result struct {
			Texts []struct {
				Text string `json:"text"`
			} `json:"texts"`
		} `json:"result"`
		Error *json.RawMessage `json:"error"`
	}
	if err := json.Unmarshal([]byte(body), &answer); err != nil {
		return "", fmt.Errorf("DeepL answered with text we cannot read as JSON")
	}
	if answer.Error != nil {
		return "", fmt.Errorf("DeepL answered with an error: %s", strings.TrimSpace(string(*answer.Error)))
	}
	var sb strings.Builder
	for _, t := range answer.Result.Texts {
		sb.WriteString(t.Text)
	}
	return sb.String(), nil
}

// versionNoticeMarkers are the distinctive fragments of the notice DeepL's
// free web endpoint answers HTTP 200 with in place of a translation since 2
// November 2026, the same in fifteen languages whatever was asked for. The
// request shape behind it is retired, so the notice is a refusal whatever the
// status code says; the app matches the same sentences in
// Translation/Providers/DeepL/DeepLVersionNotice.cs.
var versionNoticeMarkers = []string{
	"older version of deepl",       // EN
	"ältere version von deepl",     // DE
	"старую версию deepl",          // RU
	"ancienne version de deepl",    // FR
	"旧版本的 deepl",                   // zh-CN
	"deeplの古いバージョン",                // ja
	"versión antigua de deepl",     // es
	"стару версію deepl",           // uk
	"versione precedente di deepl", // it
	"deepl'in eski bir sürümünü",   // tr
	"starszej wersji deepl",        // pl
	"versão mais antiga do deepl",  // pt
	"oudere versie van deepl",      // nl
	"deepl versi lama",             // id
	"starší verzi deepl",           // cs
}

func isVersionNotice(text string) bool {
	lowered := strings.ToLower(text)
	for _, marker := range versionNoticeMarkers {
		if strings.Contains(lowered, marker) {
			return true
		}
	}
	return false
}

// translate returns the translation, the HTTP status DeepL answered with, and
// an error when the answer carries no translation.
//
// The app route goes first: the web route's jsonrpc protocol has been
// answered with the client-update notice since 2 November 2026. Whatever the
// app route cannot serve - a language it does not map, a line past its
// length cap, an answer it cannot read - the web route gets its turn;
// whatever the app route refuses pauses upstream as its HTTP 429 does.
func (r *deeplRelay) translate(ctx context.Context, text, sourceLang, targetLang string) (string, int, error) {
	// Serialize upstream requests so queued chat lines observe the first refusal.
	select {
	case r.gate <- struct{}{}:
	case <-ctx.Done():
		return "", 0, ctx.Err()
	}
	defer func() { <-r.gate }()
	if err := ctx.Err(); err != nil {
		return "", 0, err
	}
	if r.refusal != nil && r.now().Before(r.refusal.until) {
		return "", r.refusal.status, r.refusal
	}

	source, target := normalizeLangs(sourceLang, targetLang)

	if r.oneshotURL != "" && !r.skipOneshot {
		targetCode, mapped := oneshotTargets[target]
		sourceCode := ""
		if mapped && source != "auto" {
			sourceCode, mapped = oneshotSources[strings.ToUpper(source)]
		}
		if mapped && utf16Length(text) <= maxOneshotTextUnits {
			translation, status, err, retryOtherRoute := r.oneshotTranslate(ctx, text, sourceCode, targetCode)
			if err == nil {
				if r.logRequests {
					log.Printf("deepl-relay: %d chars translated over the app route", len(translation))
				}
				return translation, status, nil
			}
			if !retryOtherRoute {
				return "", status, err
			}
			if r.logRequests {
				log.Printf("deepl-relay: app route failed with %d (%s); trying the web route",
					status, excerpt(err.Error()))
			}
		}
	}

	return r.translateWeb(ctx, text, source, target)
}

func normalizeLangs(sourceLang, targetLang string) (string, string) {
	source := strings.ToLower(strings.TrimSpace(sourceLang))
	if source == "" {
		source = "auto"
	}
	target := strings.ToUpper(strings.TrimSpace(targetLang))
	if target == "" {
		target = "EN"
	}
	return source, target
}

// translateWeb is the legacy jsonrpc route, browser fingerprint and all.
func (r *deeplRelay) translateWeb(ctx context.Context, text, source, target string) (string, int, error) {
	// The client-update notice is about this protocol, not about the
	// network; it stands the web route down alone, so lines that fit the
	// app route keep translating while this one rests.
	if r.webRefusal != nil && r.now().Before(r.webRefusal.until) {
		return "", r.webRefusal.status, r.webRefusal
	}

	id := atomic.AddInt64(&r.id, 1)
	body := buildRequestBody(text, source, target, id, time.Now().UnixMilli())

	req, err := http.NewRequestWithContext(ctx, "POST", r.endpoint, strings.NewReader(body))
	if err != nil {
		return "", 0, err
	}
	// Keep headers consistent with the selected browser profile.
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("Accept", "*/*")
	req.Header.Set("Accept-Language", "en-US,en;q=0.9")
	req.Header.Set("User-Agent", r.userAgent)

	resp, err := r.client.Do(req)
	if err != nil {
		return "", 0, err
	}
	defer resp.Body.Close()
	if resp.StatusCode == http.StatusTooManyRequests || resp.StatusCode == http.StatusUnavailableForLegalReasons {
		r.refusal = &refusalError{status: resp.StatusCode, until: r.now().Add(refusalDelay(resp.Header.Get("Retry-After"), r.now())), message: fmt.Sprintf("DeepL answered HTTP %d; upstream requests paused", resp.StatusCode)}
		return "", resp.StatusCode, r.refusal
	}

	raw, err := io.ReadAll(io.LimitReader(resp.Body, 1<<20))
	if err != nil {
		return "", resp.StatusCode, err
	}

	if resp.StatusCode != http.StatusOK {
		if r.logRequests {
			log.Printf("DeepL answered %d over %s", resp.StatusCode, resp.Proto)
		}
		return "", resp.StatusCode, fmt.Errorf("DeepL answered HTTP %d: %s", resp.StatusCode, excerpt(string(raw)))
	}

	if r.logRequests {
		log.Printf("DeepL answered %d over %s", resp.StatusCode, resp.Proto)
	}

	translation, err := parseTranslation(string(raw))
	if err != nil {
		return "", resp.StatusCode, err
	}

	if isVersionNotice(translation) {
		// HTTP 200 carrying the client-update notice instead of a
		// translation. Retrying the same request shape brings the same
		// text back, so the web route stands down for the same minute an
		// HTTP 429 buys - but only this route: the notice speaks of the
		// jsonrpc protocol, and the app route answers under a different
		// profile. The refusal keeps the notice off the player's screen
		// and away from the engine the window follows. The app reads the
		// same notice from someone else's relay and refuses it the same
		// way.
		r.webRefusal = &refusalError{
			status:  http.StatusTooManyRequests,
			until:   r.now().Add(refusalDelay("", r.now())),
			message: "DeepL answered with its client-update notice; upstream requests paused",
		}
		return "", r.webRefusal.status, r.webRefusal
	}

	return translation, resp.StatusCode, nil
}

// oneshotTranslate asks the oneshot endpoint DeepL's own clients use. It
// returns the translation, the status, an error, and whether the line should
// be handed to the web route rather than reported.
func (r *deeplRelay) oneshotTranslate(ctx context.Context, text, source, target string) (string, int, error, bool) {
	req, err := http.NewRequestWithContext(ctx, "POST", r.oneshotURL,
		strings.NewReader(buildOneshotRequestBody(text, source, target, r.instanceID)))
	if err != nil {
		return "", 0, err, true
	}
	// One coherent iOS story: headers, body and version agree (see the
	// profile constants). Ordinary TLS - DeepL's edge resets browser
	// fingerprints at this address, and the iOS ones would contradict the
	// claimed version anyway.
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("Accept", "*/*")
	req.Header.Set("Accept-Encoding", "gzip, deflate, br")
	req.Header.Set("Accept-Language", "en-US,en;q=0.9")
	req.Header.Set("Authorization", "None")
	req.Header.Set("User-Agent", fmt.Sprintf("DeepL/%s CFNetwork/%s Darwin/%s",
		iosAppVersion, iosCFNetworkVersion, iosDarwinVersion))
	req.Header.Set("x-app-os-version", iosOSVersion)
	req.Header.Set("x-app-instance-id", r.instanceID)
	req.Header.Set("x-app-session-id", r.sessionID)

	resp, err := r.apiClient.Do(req)
	if err != nil {
		return "", 0, err, true
	}
	defer resp.Body.Close()

	if resp.StatusCode == http.StatusTooManyRequests || resp.StatusCode == http.StatusUnavailableForLegalReasons {
		r.refusal = &refusalError{status: resp.StatusCode, until: r.now().Add(refusalDelay(resp.Header.Get("Retry-After"), r.now())), message: fmt.Sprintf("DeepL answered HTTP %d; upstream requests paused", resp.StatusCode)}
		return "", r.refusal.status, r.refusal, false
	}
	if resp.StatusCode == http.StatusForbidden {
		// A refused client (deeplcom answers OutdatedClient among others)
		// does not clear by waiting. Pause like a 429 so the current burst
		// backs off, and retire the app route for the rest of the process;
		// un-pin the version constants only when the emulated profile is
		// deliberately updated.
		r.skipOneshot = true
		r.refusal = &refusalError{
			status:  http.StatusTooManyRequests,
			until:   r.now().Add(refusalDelay("", r.now())),
			message: "DeepL refused the emulated iOS client (HTTP 403); upstream requests paused",
		}
		return "", r.refusal.status, r.refusal, false
	}
	if resp.StatusCode != http.StatusOK {
		raw, _ := io.ReadAll(io.LimitReader(resp.Body, 1<<16))
		return "", resp.StatusCode,
			fmt.Errorf("the app endpoint answered HTTP %d: %s", resp.StatusCode, excerpt(string(raw))), true
	}

	raw, err := decodeAnswerBody(resp)
	if err != nil {
		return "", resp.StatusCode, err, true
	}
	var answer struct {
		Translations []struct {
			Text string `json:"text"`
		} `json:"translations"`
	}
	if err := json.Unmarshal(raw, &answer); err != nil {
		return "", resp.StatusCode, fmt.Errorf("the app endpoint answered with text we cannot read as JSON"), true
	}
	if len(answer.Translations) == 0 || answer.Translations[0].Text == "" {
		return "", resp.StatusCode, fmt.Errorf("the app endpoint answered without a translation"), true
	}
	return answer.Translations[0].Text, resp.StatusCode, nil, false
}

// buildOneshotRequestBody spells the oneshot request: a text array, the
// languages in the endpoint's own spelling (auto omitted), and the iOS
// app_information the clients send - the profile is checked against the
// headers, so both must name the same app.
func buildOneshotRequestBody(text, source, target, instanceID string) string {
	sourceField := ""
	if source != "" {
		sourceField = `"source_lang":` + jsonText(source) + `,`
	}
	return `{"text":[` + jsonText(text) + `],` + sourceField +
		`"target_lang":` + jsonText(target) +
		`,"usage_type":"translate","app_information":{"os":"iOS","os_version":` + jsonText(iosOSVersion) +
		`,"app_version":` + jsonText(iosAppVersion) +
		`,"app_build":` + jsonText(iosAppBuild) +
		`,"instance_id":` + jsonText(instanceID) + `}}`
}

// decodeAnswerBody gives back the wire JSON whatever encoding the endpoint
// chose. The Accept-Encoding header is set by hand, so net/http leaves the
// body undecoded.
func decodeAnswerBody(resp *http.Response) ([]byte, error) {
	var reader io.Reader = resp.Body
	switch resp.Header.Get("Content-Encoding") {
	case "gzip":
		gz, err := gzip.NewReader(resp.Body)
		if err != nil {
			return nil, err
		}
		reader = gz
	case "deflate":
		reader = flate.NewReader(resp.Body)
	case "br":
		reader = brotli.NewReader(resp.Body)
	}
	return io.ReadAll(io.LimitReader(reader, 1<<20))
}

// utf16Length counts UTF-16 code units, the unit the oneshot endpoint's
// 1500-character cap is measured in; an astral character costs two.
func utf16Length(text string) int {
	return len(utf16.Encode([]rune(text)))
}

// newUUID is the v4 identifier the iOS app's instance and session ids have.
func newUUID() string {
	b := make([]byte, 16)
	if _, err := crand.Read(b); err != nil {
		// crypto/rand does not fail in practice; a fixed id is better than
		// a panic over the player's chat lines.
		for i := range b {
			b[i] = byte(rand.Intn(256))
		}
		b[6], b[8] = 0x40, 0x80
	}
	b[6] = (b[6] & 0x0f) | 0x40
	b[8] = (b[8] & 0x3f) | 0x80
	return fmt.Sprintf("%x-%x-%x-%x-%x", b[0:4], b[4:6], b[6:8], b[8:10], b[10:16])
}

// The oneshot endpoint takes lowercase BCP-47-ish codes, and EN/PT/ZH are
// regional there: plain EN means en-US as a target, generic en as a source;
// ZH is zh-Hans as a target and generic zh as a source. The codes are the
// ones the application's DeeplLanguages.json offers, so every line the app
// can pick maps.
var oneshotTargets = map[string]string{
	"AR": "ar", "BG": "bg", "CS": "cs", "DA": "da", "DE": "de", "EL": "el",
	"EN": "en-US", "EN-US": "en-US", "EN-GB": "en-GB",
	"ES": "es", "ES-419": "es-419", "ET": "et", "FI": "fi", "FR": "fr",
	"HE": "he", "HU": "hu", "ID": "id", "IT": "it", "JA": "ja", "KO": "ko",
	"LT": "lt", "LV": "lv", "NB": "nb", "NL": "nl", "PL": "pl",
	"PT": "pt-BR", "PT-BR": "pt-BR", "PT-PT": "pt-PT",
	"RO": "ro", "RU": "ru", "SK": "sk", "SL": "sl", "SV": "sv",
	"TR": "tr", "UK": "uk", "VI": "vi",
	"ZH": "zh-Hans", "ZH-HANS": "zh-Hans", "ZH-HANT": "zh-Hant",
}

var oneshotSources = map[string]string{
	"AR": "ar", "BG": "bg", "CS": "cs", "DA": "da", "DE": "de", "EL": "el",
	"EN": "en", "EN-US": "en-US", "EN-GB": "en-GB",
	"ES": "es", "ES-419": "es-419", "ET": "et", "FI": "fi", "FR": "fr",
	"HE": "he", "HU": "hu", "ID": "id", "IT": "it", "JA": "ja", "KO": "ko",
	"LT": "lt", "LV": "lv", "NB": "nb", "NL": "nl", "PL": "pl",
	"PT": "pt", "PT-BR": "pt-BR", "PT-PT": "pt-PT",
	"RO": "ro", "RU": "ru", "SK": "sk", "SL": "sl", "SV": "sv",
	"TR": "tr", "UK": "uk", "VI": "vi",
	"ZH": "zh", "ZH-HANS": "zh", "ZH-HANT": "zh",
}

func excerpt(s string) string {
	s = strings.TrimSpace(s)
	if len(s) <= maxRefusalExcerpt {
		return s
	}
	return s[:maxRefusalExcerpt] + "…"
}

// handler serves the DeepLX protocol: POST {"text","source_lang","target_lang"}
// and answer {"code":...,"data":...}. Any path is accepted, the way DeepLX
// servers answer whatever address the client was given.
func (r *deeplRelay) handler() http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, req *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		// A web page can point its own domain at 127.0.0.1 (DNS rebinding) and
		// reach this server from the player's browser; only a loopback Host is ours.
		if !loopbackHost(req.Host) {
			w.WriteHeader(http.StatusForbidden)
			fmt.Fprint(w, `{"code":403,"message":"loopback host required"}`)
			return
		}
		if req.Method == http.MethodGet && req.URL.Path == "/health" {
			fmt.Fprint(w, `{"service":"tataruhelper-deepl-relay"}`)
			return
		}

		if req.Method != http.MethodPost {
			w.WriteHeader(http.StatusMethodNotAllowed)
			fmt.Fprint(w, `{"code":405,"message":"POST only"}`)
			return
		}

		mediaType, _, mediaErr := mime.ParseMediaType(req.Header.Get("Content-Type"))
		if mediaErr != nil || mediaType != "application/json" {
			w.WriteHeader(http.StatusUnsupportedMediaType)
			fmt.Fprint(w, `{"code":415,"message":"application/json required"}`)
			return
		}

		var in struct {
			Text       string `json:"text"`
			SourceLang string `json:"source_lang"`
			TargetLang string `json:"target_lang"`
		}
		body, err := io.ReadAll(http.MaxBytesReader(w, req.Body, 1<<20))
		var tooLarge *http.MaxBytesError
		if errors.As(err, &tooLarge) {
			w.WriteHeader(http.StatusRequestEntityTooLarge)
			fmt.Fprint(w, `{"code":413,"message":"request body too large"}`)
			return
		}
		if err != nil || json.Unmarshal(body, &in) != nil || strings.TrimSpace(in.Text) == "" {
			w.WriteHeader(http.StatusBadRequest)
			fmt.Fprint(w, `{"code":400,"message":"text is required"}`)
			return
		}

		ctx, cancel := context.WithTimeout(req.Context(), 20*time.Second)
		defer cancel()
		translation, status, err := r.translate(ctx, in.Text, in.SourceLang, in.TargetLang)
		if err != nil {
			if r.logRequests {
				log.Printf("deepl-relay: refused with %d (%s)", status, excerpt(err.Error()))
			}
			if status == http.StatusTooManyRequests || status == http.StatusUnavailableForLegalReasons {
				// The relay owns this pause; the application's DeepLX client
				// does not maintain a cooldown of its own.
				var refusal *refusalError
				if errors.As(err, &refusal) {
					seconds := int64(refusal.until.Sub(r.now()).Seconds()) + 1
					if seconds < 1 {
						seconds = 1
					}
					w.Header().Set("Retry-After", strconv.FormatInt(seconds, 10))
				}
				w.WriteHeader(status)
				fmt.Fprintf(w, `{"code":%d,"message":%s}`, status, jsonText(err.Error()))
				return
			}
			w.WriteHeader(http.StatusBadGateway)
			fmt.Fprintf(w, `{"code":502,"message":%s}`, jsonText(err.Error()))
			return
		}

		if r.logRequests {
			log.Printf("deepl-relay: %d chars translated", len(translation))
		}
		w.WriteHeader(http.StatusOK)
		fmt.Fprintf(w, `{"code":200,"data":%s}`, jsonText(translation))
	})
}

// refusalError is retained until the upstream pause expires.
type refusalError struct {
	status  int
	until   time.Time
	message string
}

func (e *refusalError) Error() string { return e.message }

func refusalDelay(value string, now time.Time) time.Duration {
	const minimum = time.Minute
	value = strings.TrimSpace(value)
	if seconds, err := strconv.ParseInt(value, 10, 32); err == nil && seconds > 60 {
		return time.Duration(seconds) * time.Second
	}
	if date, err := http.ParseTime(value); err == nil && date.Sub(now) > minimum {
		return date.Sub(now)
	}
	return minimum
}

// loopbackHost reports whether a Host header names this machine itself.
func loopbackHost(host string) bool {
	if h, _, err := net.SplitHostPort(host); err == nil {
		host = h
	}
	host = strings.Trim(host, "[]")
	return host == "localhost" || host == "127.0.0.1" || host == "::1"
}
