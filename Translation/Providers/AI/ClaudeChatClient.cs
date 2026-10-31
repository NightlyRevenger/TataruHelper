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
    /// Anthropic's Messages API.
    ///
    /// Separate from <see cref="OpenAIChatClient"/> because the shape differs in
    /// four places: the key travels in its own header rather than as a bearer
    /// token, the API version is a required header, the instructions are a
    /// top-level field rather than a message with a role, and a reply is a list
    /// of blocks rather than one string.
    /// </summary>
    internal sealed class ClaudeChatClient
    {
        private const string Endpoint = "https://api.anthropic.com/v1/messages";

        /// <summary>Pinned deliberately: the wire format is versioned, and a silent bump is a silent break.</summary>
        private const string ApiVersion = "2023-06-01";

        /// <summary>
        /// A chat line is short. This is a ceiling against a runaway reply, not
        /// a target - the model stops when the sentence is translated.
        /// </summary>
        private const int MaxTokens = 2000;

        private readonly TranslationEngineName _engine;
        private readonly string _defaultModel;
        private readonly ILogger _logger;
        private readonly ITranslationCredentialStore _credentials;

        public ClaudeChatClient(
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

        public async Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sentence))
                return string.Empty;

            var apiKey = _credentials.GetApiKey(_engine);
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new MissingApiKeyException(_engine);

            var configuredModel = _credentials.GetModel(_engine);
            var model = string.IsNullOrWhiteSpace(configuredModel) ? _defaultModel : configuredModel;

            var systemPrompt = FfxivTranslationPrompt.BuildSystemPrompt(inLang, outLang);

            var payloadText = new JObject
            {
                ["model"] = model,
                ["max_tokens"] = MaxTokens,
                ["system"] = systemPrompt,

                // Turned off on purpose. Thinking is on by default on the current
                // models, and it buys nothing on a single chat line while costing
                // seconds - and seconds are the whole point of an overlay that
                // reads dialogue as it appears.
                ["thinking"] = new JObject { ["type"] = "disabled" },

                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "user", ["content"] = sentence },
                },
            }.ToString(Formatting.None);

            Exception lastException = null;

            for (var attempt = 1; attempt <= AiRetryPolicy.MaxAttempts; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
                request.Content = new StringContent(payloadText, Encoding.UTF8, "application/json");
                request.Headers.TryAddWithoutValidation("x-api-key", apiKey);
                request.Headers.TryAddWithoutValidation("anthropic-version", ApiVersion);

                try
                {
                    using var response = await ApiHttpClient.SendAsync(request, cancellationToken)
                        .ConfigureAwait(false);
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var status = (int)response.StatusCode;

                    if (response.StatusCode == (HttpStatusCode)429 ||
                        (!response.IsSuccessStatusCode &&
                         body.IndexOf("rate_limit_error", StringComparison.OrdinalIgnoreCase) >= 0))
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

                    // A declined request answers 200 with no text and says so in
                    // stop_reason, so an empty reply is not always worth retrying.
                    if (IsRefusal(body))
                    {
                        _logger?.LogInformation("{Message}", "[" + _engine + "_REFUSED] " + body);
                        return string.Empty;
                    }

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
        /// A reply is a list of blocks, and only the text ones carry the answer.
        /// Blocks of any other kind are skipped rather than indexed past, because
        /// what else may appear there is up to the model, not to us.
        /// </summary>
        internal static string ParseContent(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            var blocks = JToken.Parse(body)?["content"] as JArray;
            if (blocks == null || blocks.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();
            foreach (var block in blocks)
            {
                if (!string.Equals(block?["type"]?.ToString(), "text", StringComparison.Ordinal))
                    continue;

                var text = block["text"]?.ToString();
                if (!string.IsNullOrEmpty(text))
                    builder.Append(text);
            }

            return AiResponseSanitizer.StripWrappingArtifacts(builder.ToString());
        }

        internal static bool IsRefusal(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return false;

            try
            {
                return string.Equals(
                    JToken.Parse(body)?["stop_reason"]?.ToString(), "refusal", StringComparison.Ordinal);
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
