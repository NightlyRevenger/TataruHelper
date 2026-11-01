using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Translation.Exceptions;
using Translation.Providers.Bing;

namespace Translation.Tests.Providers
{
    public class BingTranslatorTests
    {
        private const string Page = "<div id=\"tta_outGDCont\" data-iid=\"translator.5024\"> IG:\"ABC123\"; var params_AbusePreventionHelper = [123456,\"session-token\",86400000];";
        private const string Answer = "[{\"translations\":[{\"text\":\"Привет\",\"to\":\"ru\"}]}]";

        [Test]
        public async Task PageSessionIsReusedAndPostedWithCorrectParameters()
        {
            var handler = new Handler(async (request, index) =>
            {
                if (index == 1) return Reply(Page);
                Assert.That(request.RequestUri.Query, Does.Contain("IG=ABC123").And.Contain("IID=translator.5024"));
                var body = await request.Content.ReadAsStringAsync();
                Assert.That(body, Does.Contain("token=session-token").And.Contain("key=123456"));
                Assert.That(body, Does.Contain("fromLang=auto-detect").And.Contain("to=ru"));
                return Reply(Answer);
            });
            using var client = new HttpClient(handler);
            var provider = new BingTranslator(NullLogger.Instance, client);
            Assert.That(await provider.TranslateAsync("Hello", "auto", "ru", CancellationToken.None), Is.EqualTo("Привет"));
            Assert.That(await provider.TranslateAsync("Hello again", "auto", "ru", CancellationToken.None), Is.EqualTo("Привет"));
            Assert.That(handler.Calls, Is.EqualTo(3), "two translations must share one page session");
        }

        [TestCase(200, "{\"ShowCaptcha\":false}")]
        [TestCase(429, "{}")]
        [TestCase(403, "{}")]
        public async Task RefusalPausesSubsequentRequests(int status, string body)
        {
            var handler = new Handler((request, index) => Task.FromResult(index == 1 ? Reply(Page) : Reply(body, status)));
            using var client = new HttpClient(handler);
            var provider = new BingTranslator(NullLogger.Instance, client);
            try { await provider.TranslateAsync("Hello", "en", "ru", CancellationToken.None); }
            catch (QuotaExceededException) { }
            Assert.ThrowsAsync<QuotaExceededException>(async () => await provider.TranslateAsync("next line", "en", "ru", CancellationToken.None));
            Assert.That(handler.Calls, Is.EqualTo(2));
        }

        [TestCase("https://evil.example/translate")]
        [TestCase("http://www.bing.com/translate")]
        public async Task RedirectCannotSendSessionTokenToAnotherService(string location)
        {
            var handler = new Handler((request, index) =>
            {
                if (index == 1) return Task.FromResult(Reply(Page));
                var redirect = Reply("", 302);
                redirect.Headers.Location = new Uri(location);
                return Task.FromResult(redirect);
            });
            using var client = new HttpClient(handler);
            var provider = new BingTranslator(NullLogger.Instance, client);
            Assert.That(await provider.TranslateAsync("Hello", "en", "ru", CancellationToken.None), Is.Empty);
            Assert.That(handler.Calls, Is.EqualTo(2));
        }

        [Test]
        public async Task BingRegionalRedirectRepostsForm()
        {
            var handler = new Handler(async (request, index) =>
            {
                if (index == 1) return Reply(Page);
                if (index == 2)
                {
                    var redirect = Reply("", 302);
                    redirect.Headers.Location = new Uri("https://cn.bing.com/ttranslatev3");
                    return redirect;
                }
                Assert.That(request.RequestUri.Host, Is.EqualTo("cn.bing.com"));
                Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(await request.Content.ReadAsStringAsync(), Does.Contain("token=session-token"));
                return Reply(Answer);
            });
            using var client = new HttpClient(handler);
            var provider = new BingTranslator(NullLogger.Instance, client);
            Assert.That(await provider.TranslateAsync("Hello", "en", "ru", CancellationToken.None), Is.EqualTo("Привет"));
        }

        [Test]
        public async Task RetryAfterIsHonoredWithoutExtendingPauseForEachLine()
        {
            var now = DateTime.UtcNow;
            var handler = new Handler((request, index) =>
            {
                if (index == 1) return Task.FromResult(Reply(Page));
                if (index == 2)
                {
                    var refusal = Reply("", 429);
                    refusal.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(120));
                    return Task.FromResult(refusal);
                }
                return Task.FromResult(Reply(Answer));
            });
            using var client = new HttpClient(handler);
            var provider = new BingTranslator(NullLogger.Instance, client, () => now);
            Assert.ThrowsAsync<QuotaExceededException>(async () => await provider.TranslateAsync("Hello", "en", "ru", CancellationToken.None));
            now = now.AddSeconds(61);
            Assert.ThrowsAsync<QuotaExceededException>(async () => await provider.TranslateAsync("next", "en", "ru", CancellationToken.None));
            Assert.That(handler.Calls, Is.EqualTo(2));
            now = now.AddSeconds(60);
            Assert.That(await provider.TranslateAsync("recovered", "en", "ru", CancellationToken.None), Is.EqualTo("Привет"));
        }

        [Test]
        public async Task InitialPageCanRedirectToTheRegionalBingSite()
        {
            var handler = new Handler((request, call) =>
            {
                if (call == 1)
                {
                    var response = Reply("", 302);
                    response.Headers.Location = new Uri("https://cn.bing.com/Translator");
                    return Task.FromResult(response);
                }
                Assert.That(request.RequestUri.Host, Is.EqualTo("cn.bing.com"));
                return Task.FromResult(Reply(call == 2 ? Page : Answer));
            });
            using var client = new HttpClient(handler);
            var provider = new BingTranslator(NullLogger.Instance, client);
            Assert.That(await provider.TranslateAsync("Hello", "en", "ru", CancellationToken.None), Is.EqualTo("Привет"));
            Assert.That(handler.Calls, Is.EqualTo(3));
        }

        [TestCase("auto", true, "auto-detect")]
        [TestCase("zh-CN", false, "zh-Hans")]
        [TestCase("zh-TW", false, "zh-Hant")]
        public void LanguageAliasesMatchBing(string input, bool source, string expected)
            => Assert.That(BingTranslator.NormalizeLanguage(input, source), Is.EqualTo(expected));

        private static HttpResponseMessage Reply(string body, int status = 200)
            => new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body) };

        private sealed class Handler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, int, Task<HttpResponseMessage>> _respond;
            public int Calls;
            public Handler(Func<HttpRequestMessage, int, Task<HttpResponseMessage>> respond) { _respond = respond; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
                => _respond(request, ++Calls);
        }
    }
}
