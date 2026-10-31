using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using Translation.Exceptions;
using Translation.Http;
using Translation.Models;
using Translation.Settings;

namespace Translation.Providers.Google
{
    class GoogleTranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.GoogleTranslate;

        private const string BrowserUserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";

        private const string BrowserAccept =
            "text/html,application/xhtml+xml,application/xml;q=0.9,application/json;q=0.8,*/*;q=0.7";

        private static readonly Regex GoogleRxLegacy =
            new Regex("(?<=(<div dir=\"ltr\" class=\"t0\">)).*?(?=(<\\/div>))",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex GoogleRx =
            new Regex("(?<=(<div(.*)class=\"result-container\"(.*)>)).*?(?=(<\\/div>))",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private const string GoogleJsonBaseUrl =
            "https://translate.googleapis.com/translate_a/single?client=dict-chrome-ex&dt=t&dt=bd&dt=qca&dt=rm&dt=ss&dt=at&dt=ex&dt=ld&dt=md&dt=rw&ie=UTF-8&oe=UTF-8&otf=1&ssel=0&tsel=0&kc=7&hl={1}&sl={0}&tl={1}&q={2}";

        private const string GoogleHtmlBaseUrl =
            "https://translate.google.com/m?hl={0}&sl={1}&tl={2}&ie=UTF-8&prev=_m&q={3}";

        private readonly ILogger _logger;
        private readonly TranslationSettings _settings;

        private readonly EngineRateLimitGuard _rateLimit =
            new EngineRateLimitGuard(TranslationEngineName.GoogleTranslate, TimeSpan.FromSeconds(30));

        // A blocked address stays blocked far longer than a rate limit lasts.
        private readonly EngineRateLimitGuard _sorryPause =
            new EngineRateLimitGuard(TranslationEngineName.GoogleTranslate, TimeSpan.FromMinutes(5));

        private readonly GoogleWidgetKey _widgetKey;

        public GoogleTranslator(ILogger logger, TranslationSettings settings)
        {
            _logger = logger;
            _settings = settings ?? new TranslationSettings();
            _widgetKey = new GoogleWidgetKey(logger);
        }


        public async Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            _rateLimit.ThrowIfPaused();
            _sorryPause.ThrowIfPaused();

            try
            {
                var safeInput = sentence ?? string.Empty;
                var sourceLang = string.IsNullOrWhiteSpace(inLang) ? "auto" : inLang;
                var targetLang = string.IsNullOrWhiteSpace(outLang) ? "en" : outLang;

                if (_settings.UseGoogleTranslateHtmlEndpoint)
                {
                    string widgetResult;
                    try
                    {
                        widgetResult = await TryTranslateUsingWidgetEndpointAsync(safeInput, sourceLang, targetLang,
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (QuotaExceededException) when (_settings.UseGoogleJsonEndpoint ||
                                                         _settings.UseGoogleHtmlFallbackEndpoint)
                    {
                        // The older routes are other services and may still answer.
                        widgetResult = string.Empty;
                    }

                    if (IsValidGoogleResult(widgetResult))
                    {
                        return widgetResult;
                    }
                }

                if (_settings.UseGoogleJsonEndpoint)
                {
                    string jsonResult;
                    try
                    {
                        jsonResult = await TryTranslateUsingJsonEndpointAsync(safeInput, sourceLang, targetLang,
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (QuotaExceededException) when (_settings.UseGoogleHtmlFallbackEndpoint)
                    {
                        // The other route may still answer; if it is refused too, that one throws.
                        jsonResult = string.Empty;
                    }

                    if (IsValidGoogleResult(jsonResult))
                    {
                        return jsonResult;
                    }
                }

                if (_settings.UseGoogleHtmlFallbackEndpoint)
                {
                    var htmlResult = await TryTranslateUsingHtmlEndpointAsync(safeInput, sourceLang, targetLang,
                        cancellationToken).ConfigureAwait(false);
                    if (IsValidGoogleResult(htmlResult))
                    {
                        return htmlResult;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (QuotaExceededException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger?.LogInformation("{Message}", $"[GOOGLE_TRANSLATE_EXCEPTION] {exception}");
            }

            return string.Empty;
        }

        private const string WidgetUrl = "https://translate-pa.googleapis.com/v1/translateHtml";

        // Lines that arrive within this window travel in one request. Chat
        // often delivers several at once, and each separate request is another
        // chance for Google to decide the address is a bot.
        private static readonly TimeSpan BatchWindow = TimeSpan.FromMilliseconds(50);
        private const int MaxBatchLines = 20;
        private const int MaxBatchChars = 4000;

        private sealed class PendingLine
        {
            public PendingLine(string text) => Text = text;

            public string Text { get; }

            public TaskCompletionSource<string> Result { get; } =
                new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private readonly object _batchSync = new object();
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<PendingLine>>
            _pending = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<PendingLine>>();

        /// <summary>
        /// The service Chrome's page translation and Google's website widget
        /// use, which runs the current model; the older routes keep answering
        /// with a previous one. The text goes in as HTML, so what is escaped
        /// going in is unescaped coming out.
        /// </summary>
        private async Task<string> TryTranslateUsingWidgetEndpointAsync(string sentence, string inLang,
            string outLang, CancellationToken cancellationToken)
        {
            var line = new PendingLine(sentence);
            var key = inLang + "\n" + outLang;
            System.Collections.Generic.List<PendingLine> full = null;
            System.Collections.Generic.List<PendingLine> opened = null;

            lock (_batchSync)
            {
                if (!_pending.TryGetValue(key, out var list))
                {
                    list = new System.Collections.Generic.List<PendingLine>();
                    _pending[key] = list;
                    opened = list;
                }

                list.Add(line);

                var chars = 0;
                foreach (var queued in list)
                {
                    chars += queued.Text.Length;
                }

                if (list.Count >= MaxBatchLines || chars >= MaxBatchChars)
                {
                    _pending.Remove(key);
                    full = list;
                }
            }

            if (full != null)
            {
                _ = FlushWidgetBatchAsync(full, inLang, outLang);
            }
            else if (opened != null)
            {
                _ = FlushAfterWindowAsync(key, opened, inLang, outLang);
            }

            // The batch is not the caller's to cancel: the other lines in it
            // still want their answer.
            return await line.Result.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task FlushAfterWindowAsync(string key,
            System.Collections.Generic.List<PendingLine> opened, string inLang, string outLang)
        {
            await Task.Delay(BatchWindow).ConfigureAwait(false);

            lock (_batchSync)
            {
                if (!_pending.TryGetValue(key, out var current) || !ReferenceEquals(current, opened))
                {
                    return;
                }

                _pending.Remove(key);
            }

            await FlushWidgetBatchAsync(opened, inLang, outLang).ConfigureAwait(false);
        }

        private async Task FlushWidgetBatchAsync(System.Collections.Generic.List<PendingLine> lines,
            string inLang, string outLang)
        {
            try
            {
                var texts = new string[lines.Count];
                for (var i = 0; i < texts.Length; i++)
                {
                    texts[i] = lines[i].Text;
                }

                var results = await SendWidgetBatchAsync(texts, inLang, outLang).ConfigureAwait(false);
                if (results != null)
                {
                    for (var i = 0; i < lines.Count; i++)
                    {
                        lines[i].Result.TrySetResult(results[i]);
                    }

                    return;
                }

                // The answer did not line up with the question. One by one is
                // slower, but each line gets what it asked for.
                _logger?.LogInformation("[GOOGLE_WIDGET_BATCH_SPLIT] {Count} lines", lines.Count);
                foreach (var line in lines)
                {
                    try
                    {
                        var single = await SendWidgetBatchAsync(new[] { line.Text }, inLang, outLang)
                            .ConfigureAwait(false);
                        line.Result.TrySetResult(single[0]);
                    }
                    catch (Exception e)
                    {
                        line.Result.TrySetException(e);
                    }
                }
            }
            catch (Exception e)
            {
                foreach (var line in lines)
                {
                    line.Result.TrySetException(e);
                }
            }
        }

        /// <summary>
        /// Returns one translation per line, empty where nothing came back, or
        /// null when several lines were sent and the answer cannot be matched
        /// to them.
        /// </summary>
        private async Task<string[]> SendWidgetBatchAsync(string[] sentences, string inLang, string outLang)
        {
            var escaped = new JArray();
            foreach (var sentence in sentences)
            {
                escaped.Add(EscapeForHtml(sentence));
            }

            var body = new JArray(new JArray(escaped, inLang, outLang), "te_lib")
                .ToString(Newtonsoft.Json.Formatting.None);

            var empty = new string[sentences.Length];
            for (var i = 0; i < empty.Length; i++)
            {
                empty[i] = string.Empty;
            }

            for (var attempt = 0; attempt < 2; attempt++)
            {
                var key = await _widgetKey.GetAsync(attempt > 0, CancellationToken.None).ConfigureAwait(false);

                using (var request = new HttpRequestMessage(HttpMethod.Post, WidgetUrl))
                {
                    request.Headers.UserAgent.ParseAdd(BrowserUserAgent);
                    request.Headers.Add("X-Goog-Api-Key", key);
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json+protobuf");

                    using (var response = await ApiHttpClient.SendAsync(request, CancellationToken.None)
                               .ConfigureAwait(false))
                    {
                        if (response.StatusCode == (HttpStatusCode)429)
                        {
                            throw _rateLimit.Refused();
                        }

                        var text = await response.Content.ReadAsStringAsync(CancellationToken.None)
                            .ConfigureAwait(false);

                        if (response.IsSuccessStatusCode)
                        {
                            return sentences.Length == 1
                                ? new[] { ParseWidgetTranslation(text) }
                                : ParseWidgetBatch(text, sentences.Length);
                        }

                        _logger?.LogInformation("{Message}",
                            "[GOOGLE_WIDGET_HTTP_" + (int)response.StatusCode + "] " + text);

                        // A key Google has since changed is read again, once.
                        var keyRejected = response.StatusCode == HttpStatusCode.BadRequest ||
                                          response.StatusCode == HttpStatusCode.Forbidden;
                        if (!keyRejected)
                        {
                            return empty;
                        }
                    }
                }
            }

            return empty;
        }

        /// <summary>
        /// Parses [["a","b","c"]] (a language list may follow when the source
        /// was auto). Null when the count differs from what was sent.
        /// </summary>
        internal static string[] ParseWidgetBatch(string body, int expected)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                var translated = (JToken.Parse(body) as JArray)?[0] as JArray;
                if (translated == null || translated.Count != expected)
                {
                    return null;
                }

                var result = new string[expected];
                for (var i = 0; i < expected; i++)
                {
                    result[i] = translated[i].Type == JTokenType.String
                        ? WebUtility.HtmlDecode((string)translated[i]).Trim()
                        : string.Empty;
                }

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string EscapeForHtml(string text)
            => (text ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        /// <summary>Parses [["translated"]] (more fragments may follow the first).</summary>
        internal static string ParseWidgetTranslation(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            try
            {
                var translated = (JToken.Parse(body) as JArray)?[0] as JArray;
                if (translated == null || translated.Count == 0)
                {
                    return string.Empty;
                }

                var builder = new StringBuilder();
                foreach (var part in translated)
                {
                    if (part.Type == JTokenType.String)
                    {
                        builder.Append((string)part);
                    }
                }

                return WebUtility.HtmlDecode(builder.ToString()).Trim();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private async Task<string> TryTranslateUsingJsonEndpointAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            var url = string.Format(
                GoogleJsonBaseUrl,
                inLang,
                outLang,
                Uri.EscapeDataString(sentence));

            var body = await TranslationHttpPolicy.ExecuteHttpRequestWithRetryAsync(
                () => GetStringAsync(url, "GOOGLE_JSON", cancellationToken),
                _settings,
                _logger,
                "Google translate json",
                cancellationToken).ConfigureAwait(false);

            if (body == null)
            {
                return string.Empty;
            }

            var parsed = ParseGoogleJsonTranslation(body, _logger);
            if (!IsValidGoogleResult(parsed))
            {
                _logger?.LogInformation("[GOOGLE_JSON_INVALID_RESULT] Parsed translation was empty or malformed.");
            }

            return parsed;
        }

        private async Task<string> TryTranslateUsingHtmlEndpointAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            var url = string.Format(
                GoogleHtmlBaseUrl,
                outLang,
                inLang,
                outLang,
                Uri.EscapeDataString(sentence));

            var body = await TranslationHttpPolicy.ExecuteHttpRequestWithRetryAsync(
                () => GetStringAsync(url, "GOOGLE_HTML", cancellationToken),
                _settings,
                _logger,
                "Google translate html",
                cancellationToken).ConfigureAwait(false);

            if (body == null)
            {
                return string.Empty;
            }

            var parsed = ParseGoogleHtmlTranslation(body);
            if (!IsValidGoogleResult(parsed))
            {
                _logger?.LogInformation("[GOOGLE_HTML_INVALID_RESULT] HTML parse failed.");
            }

            return parsed;
        }

        private async Task<string> GetStringAsync(string url, string logTag, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.UserAgent.ParseAdd(BrowserUserAgent);
                request.Headers.Accept.ParseAdd(BrowserAccept);
                request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.8");

                using (var response = await ApiHttpClient.SendAsync(request, cancellationToken)
                           .ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger?.LogInformation("{Message}",
                            "[" + logTag + "_HTTP_" + (int)response.StatusCode + "] " + url);

                        if (response.StatusCode == (HttpStatusCode)429)
                        {
                            throw _rateLimit.Refused();
                        }

                        return null;
                    }

                    var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                    if (IsSorryPage(response.RequestMessage?.RequestUri, text))
                    {
                        _logger?.LogInformation("{Message}", "[GOOGLE_SORRY] " + logTag + " got the unusual-traffic page");
                        throw _sorryPause.Refused();
                    }

                    return text;
                }
            }
        }

        /// <summary>
        /// When Google decides an address sends too much it does not answer 429:
        /// it redirects to a /sorry/ page, or serves it with a 200. Read as a
        /// translation it is just an empty result, and every following line
        /// would go straight back into the same wall.
        /// </summary>
        internal static bool IsSorryPage(Uri finalUri, string body)
        {
            if (finalUri != null && finalUri.AbsolutePath.StartsWith("/sorry/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrEmpty(body) &&
                   body.IndexOf("systems have detected unusual traffic", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   body.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static string ParseGoogleJsonTranslation(string body, ILogger logger = null)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            try
            {
                var token = JToken.Parse(body);
                var rootArray = token as JArray;
                if (rootArray == null || rootArray.Count == 0)
                {
                    logger?.LogInformation("[GOOGLE_JSON_PARSE_ERROR] Root JSON token is not an array.");
                    return string.Empty;
                }

                var translatedFragments = rootArray[0] as JArray;
                if (translatedFragments == null)
                {
                    logger?.LogInformation("[GOOGLE_JSON_PARSE_ERROR] Segment array is missing.");
                    return string.Empty;
                }

                var builder = new StringBuilder();
                foreach (var fragmentToken in translatedFragments)
                {
                    var fragment = fragmentToken as JArray;
                    var translatedPart = fragment?[0]?.ToString();
                    if (string.IsNullOrEmpty(translatedPart))
                    {
                        continue;
                    }

                    builder.Append(translatedPart);
                }

                return WebUtility.HtmlDecode(builder.ToString().Trim());
            }
            catch (Exception exception)
            {
                logger?.LogInformation("{Message}", $"[GOOGLE_JSON_PARSE_EXCEPTION] {exception}");
                return string.Empty;
            }
        }

        internal static string ParseGoogleHtmlTranslation(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            var rxMatch = GoogleRxLegacy.Match(body);
            if (rxMatch.Success)
            {
                return WebUtility.HtmlDecode(rxMatch.Value.Trim());
            }

            rxMatch = GoogleRx.Match(body);
            if (rxMatch.Success)
            {
                return WebUtility.HtmlDecode(rxMatch.Value.Trim());
            }

            return string.Empty;
        }

        internal static bool IsValidGoogleResult(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmed = value.Trim();
            if (trimmed.IndexOf("<div", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return true;
        }
    }
}