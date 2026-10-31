using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Translation.Credentials;
using Translation.Exceptions;
using Translation.Http;
using Translation.Models;
using Translation.Settings;
using Translation.Utils;

namespace Translation.Providers.LibreTranslate
{
    /// <summary>
    /// An open translation server the player points at - their own, or a public
    /// instance. Not an LLM: it takes a sentence and a pair of language codes
    /// and answers with one string, so there is no prompt and nothing to sanitize.
    ///
    /// The key is optional on purpose. A private instance usually wants none,
    /// and the public ones do; sending an empty one would be rejected by the
    /// first and sending none is fine for the second.
    /// </summary>
    internal sealed class LibreTranslateTranslator : ITranslationProvider
    {
        private const string DefaultEndpoint = "https://libretranslate.com/translate";

        public TranslationEngineName EngineName => TranslationEngineName.LibreTranslate;

        private readonly ILogger _logger;
        private readonly ITranslationCredentialStore _credentials;
        private readonly TranslationSettings _settings;

        public LibreTranslateTranslator(ILogger logger, ITranslationCredentialStore credentials,
            TranslationSettings settings)
        {
            _logger = logger;
            _credentials = credentials ?? NullCredentialStore.Instance;
            _settings = settings ?? new TranslationSettings();
        }

        public Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            return TranslationHttpPolicy.ExecuteTranslationWithRetryAsync(
                () => TranslateInternalAsync(sentence, inLang, outLang, cancellationToken),
                _settings,
                _logger,
                "LibreTranslate translate",
                cancellationToken);
        }

        private async Task<string> TranslateInternalAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sentence) || string.IsNullOrEmpty(outLang))
                return string.Empty;

            var endpoint = ResolveEndpoint(_credentials.GetEndpoint(EngineName));

            var fields = new Dictionary<string, string>
            {
                ["q"] = sentence,
                ["source"] = string.IsNullOrWhiteSpace(inLang) ? "auto" : inLang,
                ["target"] = outLang,
                ["format"] = "text",
            };

            var apiKey = _credentials.GetApiKey(EngineName);
            if (!string.IsNullOrWhiteSpace(apiKey))
                fields["api_key"] = apiKey;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = new FormUrlEncodedContent(fields);

                using var response = await ApiHttpClient.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == (HttpStatusCode)429)
                {
                    throw new QuotaExceededException(EngineName,
                        EngineName + " rate limit reached (HTTP 429).");
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("{Message}",
                        "[LIBRETRANSLATE_HTTP_" + (int)response.StatusCode + "] " + body);

                    // The public instance answers a request without a key with a 400
                    // that says where to get one; the line used to come back empty and
                    // was asked for a second time, with the same result.
                    var reason = ParseError(body);
                    if (reason != null && (response.StatusCode == HttpStatusCode.BadRequest ||
                                           response.StatusCode == HttpStatusCode.Unauthorized ||
                                           response.StatusCode == HttpStatusCode.Forbidden))
                    {
                        throw new ServiceRefusedException(EngineName, (int)response.StatusCode,
                            EngineName + " refused the request: " + reason);
                    }

                    return string.Empty;
                }

                return ParseContent(body);
            }
            catch (QuotaExceededException) { throw; }
            catch (ServiceRefusedException) { throw; }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception e)
            {
                _logger?.LogInformation("{Message}", e.ToString());
                return string.Empty;
            }
        }

        /// <summary>
        /// Completes an address the user typed by hand, the same way the local
        /// engines do: they describe the instance, not the route on it.
        /// </summary>
        internal static string ResolveEndpoint(string configured)
        {
            var value = (configured ?? string.Empty).Trim();
            if (value.Length == 0)
                return DefaultEndpoint;

            value = value.TrimEnd('/');

            return value.EndsWith("/translate", StringComparison.OrdinalIgnoreCase)
                ? value
                : value + "/translate";
        }

        /// <summary>The sentence a LibreTranslate server gives for a refusal, or null if it gave none.</summary>
        internal static string ParseError(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            try
            {
                var text = SafeJson.DeserializeExternal<JObject>(body)?["error"]?.ToString();
                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// A wrong address does not answer with an error, it answers with a login
        /// page or a proxy's HTML - which is a misconfiguration, not an exception.
        /// Treated as "no translation" so the stand-in search moves on quietly
        /// instead of a stack trace per line.
        /// </summary>
        internal static string ParseContent(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            try
            {
                var parsed = SafeJson.DeserializeExternal<JObject>(body);
                return parsed?["translatedText"]?.ToString() ?? string.Empty;
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }
    }
}
