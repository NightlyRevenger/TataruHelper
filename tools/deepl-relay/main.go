package main

import (
	"context"
	"flag"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/signal"
	"time"
)

func main() {
	addr := flag.String("addr", "127.0.0.1:2285", "address to listen on (localhost only is enough for the app)")
	profile := flag.String("profile", "ios", `TLS fingerprint to use: "ios" or "safari"`)
	endpoint := flag.String("deepl", "https://www2.deepl.com/jsonrpc", "DeepL endpoint to forward to")
	verbose := flag.Bool("v", false, "log every request")
	flag.Parse()

	r := newRelay(*endpoint, *profile)
	r.logRequests = *verbose
	server := &http.Server{Addr: *addr, Handler: r.handler()}

	stopped := make(chan struct{})
	go func() {
		sig := make(chan os.Signal, 1)
		signal.Notify(sig, os.Interrupt)
		<-sig
		ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		defer cancel()
		_ = server.Shutdown(ctx)
		close(stopped)
	}()

	log.Printf("deepl-relay on http://%s forwarding %s (profile %s)", *addr, *endpoint, *profile)
	if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		log.Fatalf("deepl-relay: %v", err)
	}
	<-stopped
	fmt.Println("deepl-relay stopped")
}
