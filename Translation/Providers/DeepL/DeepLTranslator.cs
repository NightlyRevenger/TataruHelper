using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Translation.Credentials;
using Translation.Exceptions;
using Translation.Providers.DeepL.Browser;
using Translation.Http;
using Translation.Models;
using Translation.Settings;

namespace Translation.Providers.DeepL
{
    internal sealed class DeepLTranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.DeepL;

        private const string Endpoint = "https://www2.deepl.com/jsonrpc";

        private static long _requestId = InitializeRequestId();

        /// <summary>
        /// How long to leave the endpoint alone after it asks us to slow down.
        /// It says the refusal clears by itself shortly; this is our reading of
        /// "shortly", and it is a floor rather than a promise - one line every
        /// minute is nothing, and it stops a cutscene turning into a run of
        /// refusals.
        /// </summary>
        private static readonly TimeSpan CooldownAfterRefusal = TimeSpan.FromSeconds(60);

        private readonly ILogger _logger;
        private readonly TranslationSettings _settings;

        private readonly RefusalCooldown _cooldown = new RefusalCooldown(CooldownAfterRefusal);

        /// <summary>The value of the DeepL "model" setting that switches the browser route on.</summary>
        public const string BrowserModel = "browser";

        private const string ConnectHint = " Connect a browser under DeepL Advanced settings.";

        private readonly ITranslationCredentialStore _credentials;

        private readonly TimeSpan _routeTimeout;
        private readonly Func<string, string, string, string, CancellationToken, Task<string>> _relayTranslate;
        private readonly Func<string, string, string, CancellationToken, Task<string>> _browserTranslate;

        public DeepLTranslator(ILogger logger, TranslationSettings settings,
            ITranslationCredentialStore credentials = null)
            : this(logger, settings, credentials, DeepLXClient.TranslateAsync,
                (text, source, target, token) => DeepLBrowserBridge.Shared.TranslateAsync(text, source, target, token), true)
        {
        }

        internal DeepLTranslator(ILogger logger, TranslationSettings settings,
            ITranslationCredentialStore credentials,
            Func<string, string, string, string, CancellationToken, Task<string>> relayTranslate,
            Func<string, string, string, CancellationToken, Task<string>> browserTranslate,
            bool warmBrowser = false, TimeSpan? routeTimeout = null)
        {
            _logger = logger;
            _settings = settings ?? new TranslationSettings();
            _credentials = credentials;
            _relayTranslate = relayTranslate;
            _browserTranslate = browserTranslate;
            _routeTimeout = routeTimeout ?? TimeSpan.FromSeconds(8);

            if (warmBrowser && BrowserWanted)
                _ = DeepLBrowserBridge.Shared.EnsureConnectedAsync(CancellationToken.None);
        }

        private bool BrowserWanted => _credentials?.GetModel(TranslationEngineName.DeepL) == BrowserModel;

        private string DeepLXAddress => _credentials?.GetEndpoint(TranslationEngineName.DeepL);

        /// <summary>Try the relay first, then the browser only if explicitly enabled.</summary>
        private async Task<string> TryConnectedRoutesAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            var source = string.IsNullOrWhiteSpace(inLang) ? "auto" : inLang.ToLowerInvariant();
            var target = string.IsNullOrWhiteSpace(outLang) ? "en" : outLang.ToLowerInvariant();
            var deepLX = DeepLXAddress;
            if (!string.IsNullOrWhiteSpace(deepLX))
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(_routeTimeout);
                    var translated = await _relayTranslate(deepLX, sentence, source, target,
                        timeout.Token).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(translated))
                    {
                        // Someone else's DeepLX server can pass DeepL's
                        // client-update notice through as its translation.
                        // It is a refusal, not an answer; do not adopt it.
                        if (DeepLVersionNotice.IsPresent(translated))
                        {
                            _logger?.LogInformation("{Message}",
                                "[DEEPL_DEEPLX] The relay answered with DeepL's client-update notice, " +
                                "not a translation.");
                        }
                        else
                        {
                            return translated;
                        }
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogInformation("{Message}", "[DEEPL_DEEPLX] " + ex.Message);
                }
            }

            if (BrowserWanted)
            {
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(_routeTimeout);
                    var translated = await _browserTranslate(sentence, source, target, timeout.Token)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(translated))
                    {
                        if (DeepLVersionNotice.IsPresent(translated))
                        {
                            _logger?.LogInformation("{Message}",
                                "[DEEPL_BROWSER] The browser came back with DeepL's client-update notice, " +
                                "not a translation.");
                        }
                        else
                        {
                            return translated;
                        }
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogInformation("{Message}", "[DEEPL_BROWSER] " + ex.Message);
                }
            }
            return null;
        }

        public async Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sentence))
                return string.Empty;

            var connected = await TryConnectedRoutesAsync(sentence, inLang, outLang, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(connected))
            {
                return connected;
            }

            // A configured route already tried DeepL. Do not make another,
            // usually refused, direct request before the next engine can help.
            if (BrowserWanted || !string.IsNullOrWhiteSpace(DeepLXAddress))
                return string.Empty;

            // Asked to slow down, so slow down. Every line used to go on
            // knocking after the first refusal, which kept the rate exactly
            // where the endpoint had objected to it and made each line wait out
            // a round trip before falling back to another engine.
            if (_cooldown.IsActiveAt(DateTime.UtcNow, out var refusedUntilUtc))
            {
                throw new QuotaExceededException(TranslationEngineName.DeepL,
                    "DeepL asked for fewer requests; leaving it alone until " +
                    refusedUntilUtc.ToLocalTime().ToString("HH:mm:ss") + ".");
            }

            var source = string.IsNullOrWhiteSpace(inLang) ? "auto" : inLang;
            var target = string.IsNullOrWhiteSpace(outLang) ? "EN" : outLang.ToUpperInvariant();

            var id = Interlocked.Increment(ref _requestId);
            var timestamp = AdjustTimestamp(sentence, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var requestBody = BuildRequestBody(sentence, source, target, id, timestamp);

            try
            {
                var responseBody = await TranslationHttpPolicy.ExecuteHttpRequestWithRetryAsync(
                    () => PostJsonRpcAsync(requestBody, cancellationToken),
                    _settings,
                    _logger,
                    "DeepL web translate",
                    cancellationToken).ConfigureAwait(false);

                if (responseBody == null)
                    return string.Empty;

                var translated = ParseTranslation(responseBody);

                // HTTP 200 carrying the client-update notice instead of a
                // translation. Retrying the same request shape brings the
                // same text back, so the endpoint stands down exactly as it
                // does after its HTTP 429, and the line goes to another
                // engine rather than onto the screen as the notice itself.
                if (DeepLVersionNotice.IsPresent(translated))
                {
                    _cooldown.Record(DateTime.UtcNow);
                    _logger?.LogInformation("{Message}",
                        "[DEEPL_VERSION_NOTICE] HTTP 200 carrying the client-update notice; standing down " +
                        "for " + (int)CooldownAfterRefusal.TotalSeconds + "s");

                    throw new QuotaExceededException(TranslationEngineName.DeepL,
                        "DeepL's free endpoint no longer answers this request shape and asks for a newer " +
                        "client. Translation continues on another engine until it answers again." + ConnectHint);
                }

                return translated;
            }
            catch (QuotaExceededException)
            {
                throw;
            }
            catch (ServiceRefusedException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogInformation("{Message}", "[DEEPL_EXCEPTION] " + ex);
                return string.Empty;
            }
        }

        private async Task<string> PostJsonRpcAsync(string requestBody, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, Endpoint))
            {
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                // Настройки заголовков для мимикрии под DeepL iOS App
                request.Headers.TryAddWithoutValidation("Accept", "*/*");
                request.Headers.TryAddWithoutValidation("x-app-os-name", "iOS");
                request.Headers.TryAddWithoutValidation("x-app-os-version", "16.3.0");
                request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
                request.Headers.TryAddWithoutValidation("x-app-device", "iPhone13,2");
                request.Headers.TryAddWithoutValidation("User-Agent", "DeepL-iOS/2.9.1 iOS 16.3.0 (iPhone13,2)");
                request.Headers.TryAddWithoutValidation("x-app-build", "510265");
                request.Headers.TryAddWithoutValidation("x-app-version", "2.9.1");

                using (var response = await ApiHttpClient.SendAsync(request, cancellationToken)
                           .ConfigureAwait(false))
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (response.StatusCode == (HttpStatusCode)429)
                    {
                        _cooldown.Record(DateTime.UtcNow);

                        // The body is the only thing that says which refusal this
                        // is: the endpoint's own, or one from something standing
                        // in front of it. Logged rather than thrown, because the
                        // exception carries a sentence for the user and this is
                        // for whoever reads the log afterwards.
                        _logger?.LogInformation("{Message}",
                            "[DEEPL_HTTP_429] " + DescribeRefusal(responseBody) +
                            " | standing down for " + (int)CooldownAfterRefusal.TotalSeconds + "s");

                        throw new QuotaExceededException(TranslationEngineName.DeepL,
                            "DeepL is receiving requests faster than it allows. It clears on its own; " +
                            "translation continues on another engine in the meantime." + ConnectHint);
                    }

                    if (response.StatusCode == HttpStatusCode.UnavailableForLegalReasons)
                    {
                        // A region block does not clear by waiting, but the player may
                        // change the route, so the pause is the same short one.
                        _cooldown.Record(DateTime.UtcNow);
                        _logger?.LogInformation("{Message}",
                            "[DEEPL_HTTP_451] " + DescribeRefusal(responseBody));

                        throw new ServiceRefusedException(TranslationEngineName.DeepL, 451,
                            "DeepL does not serve this network (HTTP 451, unavailable in your region). " +
                            "Use a VPN, another engine, or connect the browser." + ConnectHint);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger?.LogInformation("{Message}",
                            "[DEEPL_HTTP_" + (int)response.StatusCode + "] " + responseBody);
                        return null;
                    }

                    _cooldown.Clear();
                    return responseBody;
                }
            }
        }

        /// <summary>
        /// Makes a refusal readable in the log.
        ///
        /// A refusal from the endpoint itself is JSON and says what it objects
        /// to. A refusal from something standing in front of it is a web page,
        /// and taking the first few hundred characters of one of those returns
        /// the stylesheet - the sentence naming who refused us sits below it.
        /// So markup is stripped and what a reader would have seen is kept.
        ///
        /// Crude on purpose: this is a log line, not parsing we depend on.
        /// </summary>
        internal static string DescribeRefusal(string body, int limit = 300)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "(empty body)";

            var text = body.Trim();

            if (text.StartsWith("<", StringComparison.Ordinal))
            {
                var title = Regex.Match(text, @"<title[^>]*>(.*?)</title>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                var stripped = Regex.Replace(text, @"<(script|style)[^>]*>.*?</\1>", " ",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                stripped = Regex.Replace(stripped, "<[^>]+>", " ");
                stripped = WebUtility.HtmlDecode(stripped);
                stripped = Regex.Replace(stripped, @"\s+", " ").Trim();

                text = title.Success
                    ? "[html] " + title.Groups[1].Value.Trim() + " :: " + stripped
                    : "[html] " + stripped;
            }

            return text.Length <= limit ? text : text.Substring(0, limit) + "…";
        }

        private static long InitializeRequestId()
        {
            return (long)Random.Shared.Next(8_300_000, 8_399_999) * 1000;
        }

        internal static long AdjustTimestamp(string text, long nowMilliseconds)
        {
            long iCount = 0;
            foreach (var c in text ?? string.Empty)
            {
                if (c == 'i')
                    iCount++;
            }

            if (iCount == 0)
                return nowMilliseconds;

            iCount++;
            return nowMilliseconds - nowMilliseconds % iCount + iCount;
        }

        internal static string BuildRequestBody(string text, string sourceLang, string targetLang, long id,
            long timestamp)
        {
            var payload = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["method"] = "LMT_handle_texts",
                ["id"] = id,
                ["params"] = new JObject
                {
                    ["texts"] = new JArray
                    {
                        new JObject { ["text"] = text ?? string.Empty, ["requestAlternatives"] = 0 },
                    },
                    ["splitting"] = "newlines",
                    ["lang"] = new JObject
                    {
                        ["source_lang_user_selected"] = sourceLang,
                        ["target_lang"] = targetLang,
                    },
                    ["timestamp"] = timestamp,
                    ["commonJobParams"] = new JObject
                    {
                        ["wasSpoken"] = false,
                        ["transcribe_as"] = string.Empty,
                    },
                },
            };

            var body = payload.ToString(Formatting.None);

            // Not arbitrary: the endpoint checks how this one key is spaced
            // against the request id and refuses the request when they disagree.
            // The second clause once read "id % 13", which is not the rule the
            // endpoint applies - the two disagree on 2 ids in 13, so about a
            // sixth of requests went out spaced the wrong way and came back
            // refused. The tests pin both arms by id.
            var spacedMethod = (id + 5) % 29 == 0 || (id + 3) % 13 == 0
                ? "\"method\" : \""
                : "\"method\": \"";

            return body.Replace("\"method\":\"", spacedMethod);
        }

        internal static string ParseTranslation(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            try
            {
                var texts = JToken.Parse(body)?["result"]?["texts"] as JArray;
                if (texts == null || texts.Count == 0)
                    return string.Empty;

                var sb = new StringBuilder();
                foreach (var t in texts)
                {
                    var text = t?["text"]?.ToString();
                    if (!string.IsNullOrEmpty(text))
                        sb.Append(text);
                }

                return sb.ToString();
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }
    }
}