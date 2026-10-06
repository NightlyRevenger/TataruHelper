using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Translation.Credentials;
using Translation.Exceptions;
using Translation.Http;
using Translation.Models;

namespace Translation.Providers.AI
{
    /// <summary>
    /// Talks to anything that speaks the OpenAI chat-completions shape - which
    /// by now is most of them, including servers running on the player's own
    /// machine. Two things vary beyond the address: a local server needs no
    /// key at all, and some services want an extra header naming the caller.
    /// </summary>
    internal sealed class OpenAIChatClient
    {
        private readonly TranslationEngineName _engine;
        private readonly string _defaultEndpoint;
        private readonly string _defaultModel;
        private readonly ILogger _logger;
        private readonly ITranslationCredentialStore _credentials;
        private readonly bool _requiresApiKey;
        private readonly IReadOnlyDictionary<string, string> _extraHeaders;

        public OpenAIChatClient(
            TranslationEngineName engine,
            string defaultEndpoint,
            string defaultModel,
            ILogger logger,
            ITranslationCredentialStore credentials,
            bool requiresApiKey = true,
            IReadOnlyDictionary<string, string> extraHeaders = null)
        {
            _engine = engine;
            _defaultEndpoint = defaultEndpoint;
            _defaultModel = defaultModel;
            _logger = logger;
            _credentials = credentials ?? NullCredentialStore.Instance;
            _requiresApiKey = requiresApiKey;
            _extraHeaders = extraHeaders;
        }

        public async Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sentence))
                return string.Empty;

            var apiKey = _credentials.GetApiKey(_engine);
            if (_requiresApiKey && string.IsNullOrWhiteSpace(apiKey))
                throw new MissingApiKeyException(_engine);

            var endpoint = ResolveEndpoint(_credentials.GetEndpoint(_engine), _defaultEndpoint);

            var configuredModel = _credentials.GetModel(_engine);
            var model = string.IsNullOrWhiteSpace(configuredModel) ? _defaultModel : configuredModel;

            var systemPrompt = FfxivTranslationPrompt.BuildSystemPrompt(inLang, outLang);

            var payloadText = new JObject
            {
                ["model"] = model,
                ["temperature"] = 0.2,
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = systemPrompt },
                    new JObject { ["role"] = "user", ["content"] = sentence },
                },
            }.ToString(Formatting.None);

            Exception lastException = null;

            for (var attempt = 1; attempt <= AiRetryPolicy.MaxAttempts; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = new StringContent(payloadText, Encoding.UTF8, "application/json");

                if (!string.IsNullOrWhiteSpace(apiKey))
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);

                if (_extraHeaders != null)
                {
                    foreach (var header in _extraHeaders)
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                try
                {
                    using var response = await ApiHttpClient.SendAsync(request, cancellationToken)
                        .ConfigureAwait(false);
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var status = (int)response.StatusCode;

                    if (response.StatusCode == (HttpStatusCode)429 ||
                        (!response.IsSuccessStatusCode &&
                         body.IndexOf("insufficient_quota", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        throw new QuotaExceededException(_engine,
                            _engine + " quota exceeded (HTTP " + status + ").");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger?.LogInformation("{Message}",
                            "[" + _engine + "_HTTP_" + status + "_ATTEMPT_" + attempt + "] " + body);
                        if (AiRetryPolicy.IsTransientStatus(status) && attempt < AiRetryPolicy.MaxAttempts)
                        {
                            await AiRetryPolicy.DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        throw new ServiceRefusedException(_engine, status, DescribeRefusal(status, body));
                    }

                    var parsed = ParseContent(body);
                    if (!string.IsNullOrWhiteSpace(parsed))
                        return parsed;

                    _logger?.LogInformation("{Message}",
                        "[" + _engine + "_EMPTY_CONTENT_ATTEMPT_" + attempt + "] " + body);

                    if (attempt < AiRetryPolicy.MaxAttempts)
                    {
                        await AiRetryPolicy.DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    return string.Empty;
                }
                catch (QuotaExceededException) { throw; }
                catch (MissingApiKeyException) { throw; }
                catch (ServiceRefusedException) { throw; }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger?.LogInformation("{Message}", "[" + _engine + "_EXCEPTION_ATTEMPT_" + attempt + "] " + ex);

                    if (attempt < AiRetryPolicy.MaxAttempts && AiRetryPolicy.IsTransientException(ex))
                    {
                        await AiRetryPolicy.DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    return string.Empty;
                }
            }

            _logger?.LogInformation("{Message}", "[" + _engine + "_EXHAUSTED_RETRIES] " + lastException);
            return string.Empty;
        }

        /// <summary>
        /// Completes an address the user typed by hand. A local server is
        /// described by its owner as an address and a port - "Ollama is on
        /// 11434" - not as a full path to the chat-completions route, so the
        /// missing tail is filled in rather than failing with a 404 the user
        /// has no way to read.
        /// </summary>
        internal static string ResolveEndpoint(string configured, string fallback)
        {
            var value = (configured ?? string.Empty).Trim();
            if (value.Length == 0)
                return fallback;

            value = value.TrimEnd('/');

            if (value.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                return value;

            if (value.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                return value + "/chat/completions";

            return value + "/v1/chat/completions";
        }

        private const int LongestReason = 300;

        /// <summary>
        /// What to tell the player when the service said no: the status, and
        /// the service's own explanation when it gave one.
        /// </summary>
        internal static string DescribeRefusal(int status, string body)
        {
            var reason = ReadServiceError(body);
            return reason.Length == 0 ? "HTTP " + status : "HTTP " + status + ": " + reason;
        }

        /// <summary>
        /// The service's own words for a refusal. OpenAI and the services
        /// that copy it say it as {"error": {"message": ...}}; Ollama says
        /// {"error": "..."}; anything else is shown as it came, cut short,
        /// since an HTML error page is no explanation but its first line
        /// may be.
        /// </summary>
        internal static string ReadServiceError(string body)
        {
            var text = (body ?? string.Empty).Trim();
            if (text.Length == 0)
                return string.Empty;

            try
            {
                var json = JToken.Parse(text);
                var error = json["error"];
                var said = error?.Type == JTokenType.Object
                    ? error["message"]?.ToString()
                    : error?.ToString() ?? json["message"]?.ToString();

                if (!string.IsNullOrWhiteSpace(said))
                    text = said.Trim();
            }
            catch (JsonException)
            {
                // Not JSON; the body itself is the best there is.
            }

            text = text.Replace("\r", " ").Replace("\n", " ");
            return text.Length <= LongestReason ? text : text.Substring(0, LongestReason) + "...";
        }

        internal static string ParseContent(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            var content = JToken.Parse(body)?["choices"]?[0]?["message"]?["content"]?.ToString();
            return AiResponseSanitizer.StripWrappingArtifacts(content);
        }
    }
}