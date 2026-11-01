using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Translation.Credentials;
using Translation.Models;
using Translation.Providers.DeepL;
using Translation.Settings;

namespace Translation.Tests.Providers
{
    public class DeepLRouteTests
    {
        [Test]
        public async Task RelaySuccessDoesNotCallOptionalBrowser()
        {
            var calls = new List<string>();
            var provider = Create(calls, "relay", "browser", browser: true);
            Assert.That(await provider.TranslateAsync("Hello", "en", "ru", CancellationToken.None), Is.EqualTo("relay"));
            Assert.That(calls, Is.EqualTo(new[] { "relay" }));
        }

        [Test]
        public async Task RelayFailureUsesBrowserOnlyWhenEnabled()
        {
            var calls = new List<string>();
            var provider = Create(calls, null, "browser", browser: true);
            Assert.That(await provider.TranslateAsync("Hello", "en", "ru", CancellationToken.None), Is.EqualTo("browser"));
            Assert.That(calls, Is.EqualTo(new[] { "relay", "browser" }));
        }

        [Test]
        public async Task RelayFailureWithoutBrowserReturnsToOtherEngines()
        {
            var calls = new List<string>();
            var provider = Create(calls, null, "browser", browser: false);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Assert.That(await provider.TranslateAsync("Hello", "en", "ru", timeout.Token), Is.Empty);
            Assert.That(calls, Is.EqualTo(new[] { "relay" }));
        }

        [Test]
        public async Task BothRoutesFailWithoutTryingLegacyWeb()
        {
            var calls = new List<string>();
            var provider = Create(calls, null, null, browser: true);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Assert.That(await provider.TranslateAsync("Hello", "en", "ru", timeout.Token), Is.Empty);
            Assert.That(calls, Is.EqualTo(new[] { "relay", "browser" }));
        }

        [Test]
        public async Task WaitingForBrowserVerificationCannotBlockOtherEngines()
        {
            var provider = new DeepLTranslator(NullLogger.Instance, new TranslationSettings(),
                new Credentials { Browser = true },
                (endpoint, text, source, target, token) => Task.FromResult<string>(null),
                async (text, source, target, token) => { await Task.Delay(Timeout.Infinite, token); return "never"; },
                routeTimeout: TimeSpan.FromMilliseconds(20));
            Assert.That(await provider.TranslateAsync("Hello", "en", "ru", CancellationToken.None), Is.Empty);
        }

        private static DeepLTranslator Create(List<string> calls, string relayResult, string browserResult, bool browser)
        {
            var credentials = new Credentials { Browser = browser };
            return new DeepLTranslator(NullLogger.Instance, new TranslationSettings(), credentials,
                (endpoint, text, source, target, token) =>
                {
                    calls.Add("relay");
                    if (relayResult == null) throw new InvalidOperationException("relay refused");
                    return Task.FromResult(relayResult);
                },
                (text, source, target, token) =>
                {
                    calls.Add("browser");
                    if (browserResult == null) throw new InvalidOperationException("browser refused");
                    return Task.FromResult(browserResult);
                });
        }

        private sealed class Credentials : ITranslationCredentialStore
        {
            public bool Browser;
            public string GetApiKey(TranslationEngineName engine) => "";
            public string GetRegion(TranslationEngineName engine) => "";
            public string GetModel(TranslationEngineName engine) => Browser ? "browser" : "";
            public string GetEndpoint(TranslationEngineName engine) => "http://127.0.0.1:2285";
            public bool IsEngineEnabled(TranslationEngineName engine) => true;
            public void SetApiKey(TranslationEngineName engine, string value) { }
            public void SetRegion(TranslationEngineName engine, string value) { }
            public void SetModel(TranslationEngineName engine, string value) { }
            public void SetEndpoint(TranslationEngineName engine, string value) { }
            public void SetEngineEnabled(TranslationEngineName engine, bool value) { }
            public void Save() { }
        }
    }
}
