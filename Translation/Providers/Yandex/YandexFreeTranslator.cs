using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using Translation.Exceptions;
using Translation.Http;
using Translation.Models;

namespace Translation.Providers.Yandex
{
    /// <summary>
    /// Keyless Yandex translation, using the endpoint the Yandex Translate mobile
    /// app talks to.
    ///
    /// The engine slot named "Yandex" previously mapped to Yandex Cloud, which
    /// needs a paid API key, so users without one saw the engine listed but
    /// getting nothing back. This provider needs no credentials at all and is
    /// noticeably faster than the browser-scraping engines.
    /// </summary>
    internal sealed class YandexFreeTranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.YandexFree;

        private const string ApiUrl = "https://translate.yandex.net/api/v1/tr.json/translate?id={0}-0-0&srv=android";

        private const string BrowserApiUrl =
            "https://browser.translate.yandex.net/api/v1/tr.json/translate?srv=browser_video_translation";

        private const string AppUserAgent = "Yandex Translate/21.11.2";

        private readonly ILogger _logger;

        /// <summary>
        /// The id the app sends is per session, and the service tells sessions
        /// apart: one id used for the whole time the application is open is the
        /// easiest thing to recognise and refuse. A new one every few minutes is
        /// what a real client looks like.
        /// </summary>
        private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(6);

        private readonly EngineRateLimitGuard _rateLimit =
            new EngineRateLimitGuard(TranslationEngineName.YandexFree, TimeSpan.FromSeconds(30));

        private readonly object _sessionSync = new object();
        private string _sessionId = Guid.NewGuid().ToString("N");
        private DateTime _sessionStartedUtc = DateTime.UtcNow;

        private string CurrentSessionId()
        {
            lock (_sessionSync)
            {
                if (DateTime.UtcNow - _sessionStartedUtc > SessionLifetime)
                {
                    StartNewSession();
                }

                return _sessionId;
            }
        }

        private void StartNewSession()
        {
            lock (_sessionSync)
            {
                _sessionId = Guid.NewGuid().ToString("N");
                _sessionStartedUtc = DateTime.UtcNow;
            }
        }

        public YandexFreeTranslator(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                return string.Empty;
            }

            _rateLimit.ThrowIfPaused();

            // Yandex answers "Invalid parameter: lang" to anything but lowercase,
            // and other engines' language lists spell their codes in caps.
            var sourceLang = (string.IsNullOrWhiteSpace(inLang) ? "auto" : inLang).ToLowerInvariant();
            var targetLang = (string.IsNullOrWhiteSpace(outLang) ? "en" : outLang).ToLowerInvariant();

            if (string.Equals(sourceLang, targetLang, StringComparison.OrdinalIgnoreCase))
            {
                return sentence;
            }

            try
            {
                // Passing only the target language lets Yandex detect the source,
                // which is what "Auto" maps to.
                var langPair = string.Equals(sourceLang, "auto", StringComparison.OrdinalIgnoreCase)
                    ? targetLang
                    : sourceLang + "-" + targetLang;

                var primary = await PostAsync(string.Format(ApiUrl, CurrentSessionId()), AppUserAgent,
                    sentence, langPair, cancellationToken).ConfigureAwait(false);
                if (primary.Status == HttpStatusCode.OK)
                {
                    return ParseResponse(primary.Body);
                }

                _logger?.LogInformation("{Message}",
                    "[YANDEX_HTTP_" + (int)primary.Status + "] lang=" + langPair +
                    " len=" + sentence.Length + " body=" + Truncate(primary.Body, 300));

                // Whatever was refused, the session is the one thing that
                // may have been recognised and costs nothing to replace.
                StartNewSession();

                // The browser's built-in translation talks to another host with
                // its own limits, so it often answers when the app's is refusing.
                var second = await PostAsync(BrowserApiUrl, null, sentence, langPair, cancellationToken)
                    .ConfigureAwait(false);
                if (second.Status == HttpStatusCode.OK)
                {
                    return ParseResponse(second.Body);
                }

                _logger?.LogInformation("{Message}",
                    "[YANDEX_BROWSER_HTTP_" + (int)second.Status + "] body=" + Truncate(second.Body, 300));

                if (primary.Status == (HttpStatusCode)429 || second.Status == (HttpStatusCode)429)
                {
                    throw _rateLimit.Refused();
                }

                return string.Empty;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (QuotaExceededException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger?.LogInformation("{Message}", "[YANDEX] " + e.Message);
                return string.Empty;
            }
        }

        private static async Task<(HttpStatusCode Status, string Body)> PostAsync(string url, string userAgent,
            string sentence, string langPair, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                if (userAgent != null)
                {
                    request.Headers.UserAgent.ParseAdd(userAgent);
                }

                request.Content = new FormUrlEncodedContent(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("text", sentence),
                    new System.Collections.Generic.KeyValuePair<string, string>("lang", langPair),
                    new System.Collections.Generic.KeyValuePair<string, string>("options", "4"),
                });

                using (var response = await ApiHttpClient.SendAsync(request, cancellationToken)
                           .ConfigureAwait(false))
                {
                    // The status alone says nothing about which of the
                    // language pair, the text or the session was rejected.
                    var body = await response.Content.ReadAsStringAsync(cancellationToken)
                        .ConfigureAwait(false);
                    return (response.StatusCode, body);
                }
            }
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            value = value.Replace('\n', ' ').Replace('\r', ' ');
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary>Parses {"code":200,"lang":"en-ru","text":["...","..."]}.</summary>
        internal static string ParseResponse(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            var root = JObject.Parse(body);

            if ((int?)root["code"] != 200)
            {
                return string.Empty;
            }

            if (!(root["text"] is JArray parts))
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (var part in parts)
            {
                if (part == null || part.Type == JTokenType.Null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append((string)part);
            }

            return builder.ToString();
        }
    }
}
