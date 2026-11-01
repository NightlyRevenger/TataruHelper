# deepl-relay

A small local relay that makes DeepL's free web endpoint answer TataruHelper
without a browser.

`www2.deepl.com` stands behind a bot filter that judges the sender by the TLS
handshake fingerprint and by whether the headers agree with it. Requests that
look like the DeepL mobile app (`x-app-*` headers) are refused with HTTP 429;
requests with a browser's TLS fingerprint and plain browser headers go through
- no cookies, no JavaScript challenge. .NET cannot change its own handshake,
so this relay does it instead and listens on localhost.

## Build

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

Then in TataruHelper: settings → DeepL → **Advanced**, enter

```
http://127.0.0.1:2285
```

and press *Check*. The app speaks the DeepLX protocol to the relay; the relay
speaks DeepL's web protocol upstream. Under Wine the app reaches the relay
the same way: localhost inside Wine is localhost of the host.

The order of routes stays as it is: browser → DeepLX address (this relay
included) → plain web → next engine. Refusals (`429`, `451`) are passed
through so the app's cooldown still works.

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
