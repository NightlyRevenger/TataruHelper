using System;
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
    /// Google's Generative Language API.
    ///
    /// Separate from <see cref="OpenAIChatClient"/> because Gemini does not speak
    /// the OpenAI chat shape: the model is part of the URL, the prompt goes in
    /// "contents" rather than "messages", instructions have their own field, and
    /// the key travels in a header rather than as a bearer token.
    /// </summary>
    internal sealed class GeminiChatClient
    {
        private const string EndpointFormat =
            "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";

        private readonly TranslationEngineName _engine;
        private readonly string _defaultModel;
        private readonly ILogger _logger;
        private readonly ITranslationCredentialStore _credentials;

        public GeminiChatClient(
            TranslationEngineName engine,
            string defaultModel,
            ILogger logger,
            ITranslationCredentialStore credentials)
        {
            _engine = engine;
            _defaultModel = defaultModel;
            _logger = logger;
            _credentials = credentials ?? NullCredentialStore.Instance;
        }

        public Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            return TranslateAsync(sentence, inLang, outLang, TranslationTextKind.Chat, cancellationToken);
        }

        public async Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            TranslationTextKind kind, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sentence))
                return string.Empty;

            var apiKey = _credentials.GetApiKey(_engine);
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new MissingApiKeyException(_engine);

            var configuredModel = _credentials.GetModel(_engine);
            var model = string.IsNullOrWhiteSpace(configuredModel) ? _defaultModel : configuredModel;

            var endpoint = string.Format(EndpointFormat, Uri.EscapeDataString(model));
            var systemPrompt = FfxivTranslationPrompt.BuildSystemPrompt(inLang, outLang, kind);

            var payloadText = new JObject
            {
                ["systemInstruction"] = new JObject
                {
                    ["parts"] = new JArray { new JObject { ["text"] = systemPrompt } },
                },
                ["contents"] = new JArray
                {
                    new JObject
                    {
                        ["role"] = "user",
                        ["parts"] = new JArray { new JObject { ["text"] = sentence } },
                    },
                },
                ["generationConfig"] = new JObject
                {
                    ["temperature"] = 0.2,
                },
            }.ToString(Formatting.None);

            Exception lastException = null;

            for (var attempt = 1; attempt <= AiRetryPolicy.MaxAttempts; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = new StringContent(payloadText, Encoding.UTF8, "application/json");
                request.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);

                try
                {
                    using var response = await ApiHttpClient.SendAsync(request, cancellationToken)
                        .ConfigureAwait(false);
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var status = (int)response.StatusCode;

                    if (response.StatusCode == (HttpStatusCode)429 ||
                        (!response.IsSuccessStatusCode &&
                         body.IndexOf("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase) >= 0))
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

                        throw new ServiceRefusedException(_engine, status,
                            OpenAIChatClient.DescribeRefusal(status, body));
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
        /// A reply is candidates[0].content.parts[], which the model may split into
        /// several parts, so they are joined rather than taking the first.
        /// </summary>
        internal static string ParseContent(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            var parts = JToken.Parse(body)?["candidates"]?[0]?["content"]?["parts"] as JArray;
            if (parts == null || parts.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();
            foreach (var part in parts)
            {
                var text = part?["text"]?.ToString();
                if (!string.IsNullOrEmpty(text))
                    builder.Append(text);
            }

            return AiResponseSanitizer.StripWrappingArtifacts(builder.ToString());
        }
    }
}
