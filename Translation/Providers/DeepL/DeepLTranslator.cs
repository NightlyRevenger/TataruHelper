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

        /// <summary>The value of the DeepL "region" setting that switches the quick browser mode on.</summary>
        public const string FastBrowserFlag = "fast";

        private const string ConnectHint = " Press \"Connect browser\" in the DeepL settings.";

        private readonly ITranslationCredentialStore _credentials;

        public DeepLTranslator(ILogger logger, TranslationSettings settings,
            ITranslationCredentialStore credentials = null)
        {
            _logger = logger;
            _settings = settings ?? new TranslationSettings();
            _credentials = credentials;

            // Opened ahead of the first line, so that line does not wait for the browser to start.
            if (BrowserWanted)
            {
                _ = DeepLBrowserBridge.Shared.EnsureConnectedAsync(CancellationToken.None);
            }
        }

        private bool BrowserWanted => _credentials?.GetModel(TranslationEngineName.DeepL) == BrowserModel;

        private string DeepLXAddress => _credentials?.GetEndpoint(TranslationEngineName.DeepL);

        /// <summary>
        /// The routes in the order they are tried: the player's browser, then a
        /// DeepLX server, then the plain web endpoint. Null when none of the first
        /// two gave a translation.
        /// </summary>
        private async Task<string> TryConnectedRoutesAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            var source = string.IsNullOrWhiteSpace(inLang) ? "auto" : inLang.ToLowerInvariant();
            var target = string.IsNullOrWhiteSpace(outLang) ? "en" : outLang.ToLowerInvariant();

            if (BrowserWanted)
            {
                try
                {
                    DeepLBrowserBridge.Shared.FastMode = _credentials.GetRegion(TranslationEngineName.DeepL) == FastBrowserFlag;
                    return await DeepLBrowserBridge.Shared.TranslateAsync(sentence, source, target, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger?.LogInformation("{Message}", "[DEEPL_BROWSER] " + ex.Message);
                }
            }

            var deepLX = DeepLXAddress;
            if (!string.IsNullOrWhiteSpace(deepLX))
            {
                try
                {
                    var translated = await DeepLXClient.TranslateAsync(deepLX, sentence, source, target,
                        cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(translated))
                    {
                        return translated;
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger?.LogInformation("{Message}", "[DEEPL_DEEPLX] " + ex.Message);
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

                return ParseTranslation(responseBody);
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