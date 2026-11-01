using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Translation.Exceptions;
using Translation.Models;
using Translation.Providers.Web;

namespace Translation.Tests.Providers
{
    public class WebSessionTranslatorTests
    {
        [Test]
        public async Task TranslateComReusesSessionAndDetectsAutoLanguage()
        {
            var handler = new Handler(async (request, call) =>
            {
                if (call == 1) return Reply("page");
                if (request.RequestUri.AbsolutePath.EndsWith("ajax_lang_auto_detect")) return Reply("{\"language\":\"ja\"}");
                Assert.That(await request.Content.ReadAsStringAsync(), Does.Contain("source_lang=ja"));
                return Reply("{\"translated_text\":\"a &amp; b\"}");
            });
            using var client = new HttpClient(handler);
            var provider = new TranslateComTranslator(NullLogger.Instance, client);
            Assert.That(await provider.TranslateAsync("ありがとう", "auto", "ru", CancellationToken.None), Is.EqualTo("a & b"));
            Assert.That(await provider.TranslateAsync("ありがとう", "ja", "ru", CancellationToken.None), Is.EqualTo("a & b"));
            Assert.That(handler.Calls, Is.EqualTo(4));
        }

        [Test]
        public async Task ModernMtDoesNotRequireTheUnavailableWarmupPage()
        {
            var text = "A \"crystal\" & a friend.\nありがとう";
            var handler = new Handler(async (request, call) =>
            {
                Assert.That(request.RequestUri.Host, Is.EqualTo("webapi.modernmt.com"));
                var payload = JObject.Parse(await request.Content.ReadAsStringAsync());
                Assert.That((string)payload["q"], Is.EqualTo(text));
                Assert.That((string)payload["source"], Is.Empty);
                Assert.That((string)payload["verify"], Has.Length.EqualTo(32));
                return Reply("{\"data\":{\"translation\":\"translated\"}}");
            });
            using var client = new HttpClient(handler);
            Assert.That(await new ModernMtTranslator(NullLogger.Instance, client).TranslateAsync(text, "auto", "ru", CancellationToken.None), Is.EqualTo("translated"));
            Assert.That(handler.Calls, Is.EqualTo(1));
        }

        [Test]
        public async Task AlibabaReusesCsrfAndEscapesQueryText()
        {
            var handler = new Handler((request, call) =>
            {
                if (call == 1) return Task.FromResult(Reply("page"));
                if (call == 2) return Task.FromResult(Reply("{\"token\":\"csrf-test\"}"));
                Assert.That(request.RequestUri.Query, Does.Contain("query=a%26b%3Fc"));
                return Task.FromResult(Reply("{\"data\":{\"translateText\":\"a &amp; b\"}}"));
            });
            using var client = new HttpClient(handler);
            var provider = new AlibabaTranslator(NullLogger.Instance, client);
            for (var i = 0; i < 2; i++)
                Assert.That(await provider.TranslateAsync("a&b?c", "en", "ru", CancellationToken.None), Is.EqualTo("a & b"));
            Assert.That(handler.Calls, Is.EqualTo(4));
        }

        [Test]
        public async Task VolcengineOmitsSourceForAutodetection()
        {
            var handler = new Handler(async (request, call) =>
            {
                var body = JObject.Parse(await request.Content.ReadAsStringAsync());
                Assert.That(body.Property("source_language"), Is.Null);
                Assert.That((string)body["target_language"], Is.EqualTo("ru"));
                return Reply("{\"translation\":\"translated\"}");
            });
            using var client = new HttpClient(handler);
            Assert.That(await new VolcengineTranslator(NullLogger.Instance, client).TranslateAsync("Hello", "auto", "RU", CancellationToken.None), Is.EqualTo("translated"));
        }

        [Test]
        public async Task TencentUsesVerifiedCurrentSiteAndJoinsTextParts()
        {
            var handler = new Handler((request, call) =>
            {
                Assert.That(request.RequestUri.Host, Is.EqualTo("yi.qq.com"));
                return Task.FromResult(Reply("{\"auto_translation\":[\"a\",\"b\"]}"));
            });
            using var client = new HttpClient(handler);
            Assert.That(await new TencentTranslator(NullLogger.Instance, client).TranslateAsync("Hello", "en", "ru", CancellationToken.None), Is.EqualTo("ab"));
        }

        [TestCase(429)]
        [TestCase(403)]
        [TestCase(503)]
        public async Task RefusalsRespectRetryAfterAndRecoverWithoutExtendingPause(int status)
        {
            var now = DateTime.UtcNow;
            var handler = new Handler((request, call) =>
            {
                var response = Reply("ok", call == 1 ? status : 200);
                if (call == 1) response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(120));
                return Task.FromResult(response);
            });
            using var client = new HttpClient(handler);
            var provider = new Probe(client, () => now);
            Assert.ThrowsAsync<QuotaExceededException>(() => provider.TranslateAsync("x", "en", "ru", CancellationToken.None));
            now = now.AddSeconds(61);
            Assert.ThrowsAsync<QuotaExceededException>(() => provider.TranslateAsync("x", "en", "ru", CancellationToken.None));
            Assert.That(handler.Calls, Is.EqualTo(1));
            now = now.AddSeconds(60);
            Assert.That(await provider.TranslateAsync("x", "en", "ru", CancellationToken.None), Is.EqualTo("ok"));
        }

        [Test]
        public void CancellationDoesNotBecomeATranslationFailure()
        {
            using var client = new HttpClient(new Handler((r, n) => throw new Exception("must not send")));
            var provider = new Probe(client, () => DateTime.UtcNow);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Assert.That(async () => await provider.TranslateAsync("x", "en", "ru", cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
        }

        [TestCase(TranslationEngineName.TranslateCom, true)]
        [TestCase(TranslationEngineName.ModernMT, true)]
        [TestCase(TranslationEngineName.Tencent, false)]
        [TestCase(TranslationEngineName.Volcengine, false)]
        [TestCase(TranslationEngineName.Alibaba, false)]
        public void LowerQualityReservesRequireOptIn(TranslationEngineName engine, bool enabled)
            => Assert.That(TranslationEngineDefaults.IsOnByDefault(engine), Is.EqualTo(enabled));

        private sealed class Probe : WebSessionTranslator
        {
            public Probe(HttpClient client, Func<DateTime> now) : base(NullLogger.Instance, client, now) { }
            public override TranslationEngineName EngineName => TranslationEngineName.TranslateCom;
            protected override Task<string> TranslateCoreAsync(string text, string source, string target, CancellationToken token)
                => SendAsync(new HttpRequestMessage(HttpMethod.Post, "https://example.test/"), token);
        }
        private static HttpResponseMessage Reply(string body, int status = 200)
            => new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body) };
        private sealed class Handler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, int, Task<HttpResponseMessage>> _reply;
            public int Calls;
            public Handler(Func<HttpRequestMessage, int, Task<HttpResponseMessage>> reply) { _reply = reply; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => _reply(request, ++Calls);
        }
    }
}
