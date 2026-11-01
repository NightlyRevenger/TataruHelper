// Package main implements a small local relay that lets TataruHelper reach
// DeepL's free web endpoint without a browser.
//
// The endpoint behind Cloudflare turns away hand-written HTTP clients: it
// classifies the sender by the TLS handshake fingerprint and by whether the
// headers agree with that fingerprint. Mimicking the DeepL iOS *application*
// (the x-app-* headers) is exactly what gets rejected; a browser's TLS
// fingerprint with plain browser headers goes through, no cookies and no JS
// challenge. Go cannot change .NET's handshake from inside the app, so the
// relay sits beside it and speaks DeepLX on localhost.
package main

import (
	"context"
	cryptotls "crypto/tls"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"math/rand"
	"net"
	"net/http"
	"strings"
	"sync/atomic"
	"time"

	utls "github.com/refraction-networking/utls"
)

const maxRefusalExcerpt = 200

// deeplRelay forwards DeepLX-shaped translations to DeepL's jsonrpc endpoint.
type deeplRelay struct {
	endpoint    string
	client      *http.Client
	userAgent   string
	id          int64
	logRequests bool
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
		endpoint:  endpoint,
		userAgent: p.userAgent,
		id:        int64(rand.Intn(100_000)+8_300_000) * 1000,
	}

	if strings.HasPrefix(endpoint, "https://") {
		dialTLS := func(ctx context.Context, network, addr string) (net.Conn, error) {
			d := net.Dialer{}
			raw, err := d.DialContext(ctx, network, addr)
			if err != nil {
				return nil, err
			}
			u := utls.UClient(raw, &utls.Config{
				ServerName:         "www2.deepl.com",
				InsecureSkipVerify: true,
				NextProtos:         []string{"h2", "http/1.1"},
			}, p.helloID)
			if err := u.Handshake(); err != nil {
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
				DialTLSContext:     dialTLS,
				ForceAttemptHTTP2:  true,
				DisableCompression: true,
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

// translate returns the translation, the HTTP status DeepL answered with, and
// an error when the answer carries no translation.
func (r *deeplRelay) translate(text, sourceLang, targetLang string) (string, int, error) {
	source := strings.ToLower(strings.TrimSpace(sourceLang))
	if source == "" {
		source = "auto"
	}
	target := strings.ToUpper(strings.TrimSpace(targetLang))
	if target == "" {
		target = "EN"
	}

	id := atomic.AddInt64(&r.id, 1)
	body := buildRequestBody(text, source, target, id, time.Now().UnixMilli())

	req, err := http.NewRequest("POST", r.endpoint, strings.NewReader(body))
	if err != nil {
		return "", 0, err
	}
	// Browser headers only. The x-app-* application headers are what the
	// endpoint's protection classifies as a bot.
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("Accept", "*/*")
	req.Header.Set("Accept-Language", "en-US,en;q=0.9")
	req.Header.Set("User-Agent", r.userAgent)

	resp, err := r.client.Do(req)
	if err != nil {
		return "", 0, err
	}
	defer resp.Body.Close()

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
	return translation, resp.StatusCode, nil
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

		if req.Method != http.MethodPost {
			w.WriteHeader(http.StatusMethodNotAllowed)
			fmt.Fprint(w, `{"code":405,"message":"POST only"}`)
			return
		}

		var in struct {
			Text       string `json:"text"`
			SourceLang string `json:"source_lang"`
			TargetLang string `json:"target_lang"`
		}
		body, err := io.ReadAll(io.LimitReader(req.Body, 1<<20))
		if err != nil || json.Unmarshal(body, &in) != nil || strings.TrimSpace(in.Text) == "" {
			w.WriteHeader(http.StatusBadRequest)
			fmt.Fprint(w, `{"code":400,"message":"text is required"}`)
			return
		}

		translation, status, err := r.translate(in.Text, in.SourceLang, in.TargetLang)
		if err != nil {
			if r.logRequests {
				log.Printf("deepl-relay: refused with %d (%s)", status, excerpt(err.Error()))
			}
			if status == http.StatusTooManyRequests || status == http.StatusUnavailableForLegalReasons {
				// Passed through as-is so the app's cooldown logic still works.
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
