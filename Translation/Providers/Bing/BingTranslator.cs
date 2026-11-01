using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Translation.Exceptions;
using Translation.Models;

namespace Translation.Providers.Bing
{
    /// <summary>Bing's web translator, with a private cookie jar and page session.</summary>
    internal sealed class BingTranslator : ITranslationProvider
    {
        private static readonly Uri Page = new Uri("https://www.bing.com/Translator");
        private static readonly Uri Translate = new Uri("https://www.bing.com/ttranslatev3");
        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Func<DateTime> _now;
        private DateTime _refusedUntilUtc; // protected by _gate
        private PageSession _session;
        public TranslationEngineName EngineName => TranslationEngineName.Bing;

        public BingTranslator(ILogger logger) : this(logger, new HttpClient(new SocketsHttpHandler
        {
            CookieContainer = new CookieContainer(), UseCookies = true, AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        })) { }

        internal BingTranslator(ILogger logger, HttpClient client, Func<DateTime> now = null)
        {
            _logger = logger;
            _now = now ?? (() => DateTime.UtcNow);
            _client = client;
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        }

        public async Task<string> TranslateAsync(string sentence, string inLang, string outLang, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sentence)) return string.Empty;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            var acquired = false;
            try
            {
                await _gate.WaitAsync(timeout.Token).ConfigureAwait(false);
                acquired = true;
                if (_now() < _refusedUntilUtc)
                    throw new QuotaExceededException(EngineName, "Bing is temporarily paused; another translator will take over.");

                if (_session == null || DateTime.UtcNow >= _session.ExpiresUtc)
                {
                    _session = await GetSessionAsync(timeout.Token).ConfigureAwait(false);
                }

                var url = new Uri((_session.TranslateUri ?? Translate) + "?isVertical=1&IG=" + Uri.EscapeDataString(_session.Ig)
                    + "&IID=" + Uri.EscapeDataString(_session.Iid));
                var fields = new Dictionary<string, string>
                {
                    ["text"] = sentence, ["fromLang"] = NormalizeLanguage(inLang, true),
                    ["to"] = NormalizeLanguage(outLang, false),
                    ["key"] = _session.Key, ["token"] = _session.Token,
                    ["tryFetchingGenderDebiasedTranslations"] = "true"
                };
                using var response = await PostAsync(url, fields, timeout.Token).ConfigureAwait(false);
                if (response.StatusCode == (HttpStatusCode)429)
                {
                    var now = _now();
                    var requested = response.Headers.RetryAfter?.Date?.UtcDateTime
                        ?? now.Add(response.Headers.RetryAfter?.Delta ?? TimeSpan.Zero);
                    _refusedUntilUtc = requested > now.AddMinutes(1) ? requested : now.AddMinutes(1);
                    throw new QuotaExceededException(EngineName, "Bing asked for fewer requests.");
                }
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
                var translated = ParseTranslation(body);
                if (string.IsNullOrWhiteSpace(translated))
                    throw new InvalidOperationException("Bing returned no translation.");
                return translated;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (QuotaExceededException)
            {
                // A cached refusal must not extend the pause on every chat line.
                if (_now() >= _refusedUntilUtc) _refusedUntilUtc = _now().AddMinutes(1);
                throw;
            }
            catch (Exception ex)
            {
                if (acquired)
                {
                    _session = null;
                    _refusedUntilUtc = _now().AddMinutes(1);
                }
                _logger?.LogInformation("{Message}", "[BING] " + ex.Message);
                return string.Empty;
            }
            finally { if (acquired) _gate.Release(); }
        }

        private async Task<PageSession> GetSessionAsync(CancellationToken token)
        {
            var url = Page;
            for (var redirects = 0; redirects < 4; redirects++)
            {
                using var response = await _client.GetAsync(url, token).ConfigureAwait(false);
                if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                {
                    url = SafeRedirect(url, response.Headers.Location);
                    continue;
                }
                response.EnsureSuccessStatusCode();
                var session = ParseSession(await response.Content.ReadAsStringAsync(token).ConfigureAwait(false));
                session.TranslateUri = new Uri(url, "/ttranslatev3");
                return session;
            }
            throw new InvalidOperationException("Bing redirected too many times.");
        }

        private static Uri SafeRedirect(Uri source, Uri location)
        {
            if (location == null) throw new InvalidOperationException("Bing returned an empty redirect.");
            var target = new Uri(source, location);
            if (target.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(target.UserInfo)
                || (target.Host != "www.bing.com" && target.Host != "cn.bing.com"))
                throw new InvalidOperationException("Bing returned an unexpected redirect.");
            return target;
        }

        private async Task<HttpResponseMessage> PostAsync(Uri url, Dictionary<string, string> fields, CancellationToken token)
        {
            using var content = new FormUrlEncodedContent(fields);
            var response = await _client.PostAsync(url, content, token).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.Found) return response;
            Uri target;
            try { target = SafeRedirect(url, response.Headers.Location); }
            finally { response.Dispose(); }
            using var redirected = new FormUrlEncodedContent(fields);
            return await _client.PostAsync(target, redirected, token).ConfigureAwait(false);
        }

        internal sealed class PageSession
        {
            public string Key, Token, Ig, Iid;
            public Uri TranslateUri;
            public DateTime ExpiresUtc;
        }

        internal static PageSession ParseSession(string html)
        {
            string Match(string pattern)
            {
                var match = Regex.Match(html, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                if (!match.Success) throw new InvalidOperationException("Bing page session is unavailable.");
                return match.Groups[1].Value;
            }
            var parameters = JArray.Parse(Match(@"params_AbusePreventionHelper\s*=\s*(\[.*?\])\s*;"));
            if (parameters.Count < 2 || string.IsNullOrWhiteSpace((string)parameters[1]))
                throw new InvalidOperationException("Bing page token is missing.");
            return new PageSession
            {
                Key = (string)parameters[0], Token = (string)parameters[1],
                Ig = Match("IG:\\s*\"([^\"]+)\""),
                Iid = Match("<div\\s+id=\"tta_outGDCont\"\\s+data-iid=\"([^\"]+)\""),
                ExpiresUtc = DateTime.UtcNow.AddMinutes(20)
            };
        }

        internal static string ParseTranslation(string body)
        {
            var answer = JToken.Parse(body);
            return answer is JArray array && array.Count > 0
                ? (string)array[0]?["translations"]?[0]?["text"] : null;
        }

        internal static string NormalizeLanguage(string language, bool source)
        {
            if (string.IsNullOrWhiteSpace(language) || language.Equals("auto", StringComparison.OrdinalIgnoreCase))
                return source ? "auto-detect" : "en";
            if (language.Equals("zh-CN", StringComparison.OrdinalIgnoreCase)) return "zh-Hans";
            if (language.Equals("zh-TW", StringComparison.OrdinalIgnoreCase)) return "zh-Hant";
            return language;
        }
    }
}
