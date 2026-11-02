# deepl-relay

A small local relay that makes DeepL translate for TataruHelper without a
browser. It serves the DeepLX protocol locally and reaches DeepL over two
routes:

- **App route** (primary): the `oneshot-free.www.deepl.com` endpoint DeepL's
  own clients use, under a pinned iOS profile (User-Agent, `x-app-*` headers
  and `app_information` telling one coherent story, ordinary TLS). It caps a
  request at 1500 UTF-16 units, so longer lines go to the web route.
- **Web route** (fallback): the legacy `www2.deepl.com/jsonrpc` protocol with
  a browser TLS fingerprint. Since 2 November 2026 the free endpoint answers
  it with a client-update notice instead of translations, so today it is the
  fallback rather than the way through.

These are unofficial routes: the observations behind them describe the
configurations tested, not guaranteed filtering rules. The relay verifies the
upstream TLS certificate and hostname.

## Build

Requires Go 1.27 or newer: the uTLS connection wrapper relies on the
standard HTTP transport accepting its TLS state for HTTP/2 negotiation.

```sh
go build -o deepl-relay .
```

Cross-compile for the other system with `GOOS=windows go build` or
`GOOS=linux go build`.

## Run

```sh
./deepl-relay            # listens on http://127.0.0.1:2285
./deepl-relay -v         # log every request (shows the HTTP version used)
```

Release packages include the Windows and Linux binaries. On Windows the app
starts and checks its helper automatically; the Linux desktop launcher does
that for Wine. Both stop only their own helper when the app exits. No Go
installation or manual address entry is needed. Custom DeepLX endpoints and
engine enable/disable choices are preserved. The browser remains an optional
backup under Advanced.

For a standalone relay, in TataruHelper: settings → DeepL → **Advanced**, enter

```
http://127.0.0.1:2285
```

and press *Check*. The app speaks the DeepLX protocol to the relay; the relay
speaks DeepL's app and web protocols upstream. Under Wine the app reaches the
relay the same way: localhost inside Wine is localhost of the host.

The route order is DeepLX address (this relay included) → optional browser
backup → another enabled translation engine. The browser is off by default
and can be connected under DeepL → Advanced. Its selection survives app
restarts. When a relay or browser is configured, the legacy direct web
request is skipped after their failure. Without configured routes, older
installations still try the legacy web endpoint.

The relay serializes upstream requests. After `429` or `451` from either
route, it pauses upstream requests for at least 60 seconds, honoring a
longer `Retry-After` (delta seconds or an HTTP date). During that pause, it
returns the same status immediately with a `Retry-After` header. The app's
DeepLX client has no cooldown of its own and continues to its remaining
fallback routes.

Since 2 November 2026 the web endpoint answers `200` with "You're using an
older version of DeepL…" in place of translations. That notice is refused
like a `429` - the same status handed to the app, so it never reaches the
player's screen - but it pauses the web route only: the notice speaks of the
jsonrpc protocol, and the app route answers under a different profile and
keeps translating. A `403` from the app endpoint means the emulated client
is refused; the relay retires the app route for the process and the web
route carries the lines.

## Options

| Flag | Default | Meaning |
|---|---|---|
| `-addr` | `127.0.0.1:2285` | where to listen |
| `-profile` | `ios` | TLS fingerprint for the web route: `ios` or `safari` |
| `-oneshot` | `https://oneshot-free.www.deepl.com/v1/translate` | app route endpoint (empty disables it) |
| `-deepl` | `https://www2.deepl.com/jsonrpc` | web route fallback endpoint |
| `-v` | off | log each request |

## Tests

```sh
go test ./...
```

The tests pin the endpoint's quirks the same way the app does: the timestamp
rule (divisible by the number of `i` + 1) and the `"method"` key spacing
checked against the request id, and they cover the app route: its iOS
request profile, language mapping, length cap, refusals, and the fact that
the web route's notice pause leaves the app route working.

## Caveats

These are unofficial routes: they go against the spirit of DeepL's terms, the
addresses can still be rate-limited or blocked, and DeepL may change the
filter at any time - then the relay starts getting refusals and the overlay
falls back to other engines by itself.

The app route's iOS profile is pinned to a shipping app version (see the
constants in `relay.go`). DeepL answers a mismatched or future version with
`429`/`403`, and a future claim can temporarily ban the IP; update the
constants only when deliberately moving to a newer app profile.
