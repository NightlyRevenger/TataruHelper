# deepl-relay

A small local relay that makes DeepL's free web endpoint answer TataruHelper
without a browser.

Live probes found that a browser TLS profile and browser headers worked
without cookies or a JavaScript challenge. This is an observation about the
configuration tested, not a guarantee about the service's filtering rules.
The relay verifies the upstream TLS certificate and hostname.

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
speaks DeepL's web protocol upstream. Under Wine the app reaches the relay
the same way: localhost inside Wine is localhost of the host.

The route order is DeepLX address (this relay included) → optional browser
backup → another enabled translation engine. The browser is off by default
and can be connected under DeepL → Advanced. Its selection survives app
restarts. When a relay or browser is configured, the legacy direct web
request is skipped after their failure. Without configured routes, older
installations still try the legacy web endpoint.

The relay serializes upstream requests. After `429` or `451`, it pauses
upstream requests for at least 60 seconds, honoring a longer `Retry-After`
(delta seconds or an HTTP date). During that pause, it returns the same
status immediately with a `Retry-After` header. The app's DeepLX client
has no cooldown of its own and continues to its remaining fallback routes.

Since 2 November 2026 the endpoint also answers `200` with "You're using an
older version of DeepL…" in place of translations. That notice is refused
like a `429`: the same pause, the same status handed to the app, so it
never reaches the player's screen.

## Options

| Flag | Default | Meaning |
|---|---|---|
| `-addr` | `127.0.0.1:2285` | where to listen |
| `-profile` | `ios` | TLS fingerprint: `ios` or `safari` |
| `-deepl` | `https://www2.deepl.com/jsonrpc` | upstream endpoint |
| `-v` | off | log each request |

## Tests

```sh
go test ./...
```

The tests pin the endpoint's quirks the same way the app does: the timestamp
rule (divisible by the number of `i` + 1) and the `"method"` key spacing
checked against the request id.

## Caveats

This is an unofficial route: it goes against the spirit of DeepL's terms, the
address can still be rate-limited or blocked, and DeepL may change the filter
at any time - then the relay starts getting 429 and the overlay falls back to
other engines by itself.
