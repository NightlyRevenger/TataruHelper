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

namespace Translation.Providers.YandexCloud
{
    internal sealed class YandexCloudTranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.Yandex;

        private const string Endpoint = "https://translate.api.cloud.yandex.net/translate/v2/translate";

        private readonly ILogger _logger;
        private readonly ITranslationCredentialStore _credentials;

        public YandexCloudTranslator(ILogger logger, ITranslationCredentialStore credentials)
        {
            _logger = logger;
            _credentials = credentials ?? NullCredentialStore.Instance;
        }


        public async Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sentence))
                return string.Empty;

            var apiKey = _credentials.GetApiKey(TranslationEngineName.Yandex);
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new MissingApiKeyException(TranslationEngineName.Yandex);

            // Folder ID is mandatory for Yandex Cloud Translate; we reuse the Region slot.
            var folderId = _credentials.GetRegion(TranslationEngineName.Yandex);
            if (string.IsNullOrWhiteSpace(folderId))
                throw new MissingApiKeyException(TranslationEngineName.Yandex);

            var target = string.IsNullOrWhiteSpace(outLang) ? "en" : outLang;
            var source = string.IsNullOrWhiteSpace(inLang) || inLang == "auto" ? null : inLang;

            var payload = new JObject
            {
                ["folderId"] = folderId,
                ["targetLanguageCode"] = target,
                ["texts"] = new JArray { sentence },
                ["format"] = "PLAIN_TEXT",
            };
            if (source != null)
                payload["sourceLanguageCode"] = source;

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("Authorization", "Api-Key " + apiKey);

            try
            {
                using var response = await ApiHttpClient.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.StatusCode == (HttpStatusCode)429 ||
                    (!response.IsSuccessStatusCode &&
                     (responseBody.IndexOf("quota", StringComparison.OrdinalIgnoreCase) >= 0 ||
                      responseBody.IndexOf("ResourceExhausted", StringComparison.OrdinalIgnoreCase) >= 0)))
                {
                    throw new QuotaExceededException(TranslationEngineName.Yandex,
                        "Yandex Translate quota exceeded (HTTP " + (int)response.StatusCode + ").");
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("{Message}",
                        "[YANDEX_HTTP_" + (int)response.StatusCode + "] " + responseBody);
                    // A client error is the service saying no to this key or request;
                    // retrying or silence would only hide why nothing is translated.
                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500 && (int)response.StatusCode != 408)
                        throw new ServiceRefusedException(TranslationEngineName.Yandex, (int)response.StatusCode,
                            AI.OpenAIChatClient.DescribeRefusal((int)response.StatusCode, responseBody));
                    return string.Empty;
                }

                return ParseTranslation(responseBody);
            }
            catch (QuotaExceededException) { throw; }
            catch (MissingApiKeyException) { throw; }
            catch (ServiceRefusedException) { throw; }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger?.LogInformation("{Message}", "[YANDEX_EXCEPTION] " + ex);
                return string.Empty;
            }
        }

        internal static string ParseTranslation(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            var translations = JToken.Parse(body)?["translations"] as JArray;
            if (translations == null || translations.Count == 0)
                return string.Empty;

            return translations[0]?["text"]?.ToString() ?? string.Empty;
        }
    }
}