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

namespace Translation.Providers.GoogleCloud
{
    internal sealed class GoogleCloudTranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.GoogleCloudTranslate;

        private const string Endpoint = "https://translation.googleapis.com/language/translate/v2";

        private readonly ILogger _logger;
        private readonly ITranslationCredentialStore _credentials;

        public GoogleCloudTranslator(ILogger logger, ITranslationCredentialStore credentials)
        {
            _logger = logger;
            _credentials = credentials ?? NullCredentialStore.Instance;
        }


        public async Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sentence))
                return string.Empty;

            var apiKey = _credentials.GetApiKey(TranslationEngineName.GoogleCloudTranslate);
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new MissingApiKeyException(TranslationEngineName.GoogleCloudTranslate);

            var target = string.IsNullOrWhiteSpace(outLang) ? "en" : outLang;
            var source = string.IsNullOrWhiteSpace(inLang) || inLang == "auto" ? null : inLang;

            var payload = new JObject { ["q"] = sentence, ["target"] = target, ["format"] = "text", };
            if (source != null)
                payload["source"] = source;

            using var request =
                new HttpRequestMessage(HttpMethod.Post, Endpoint + "?key=" + Uri.EscapeDataString(apiKey));
            request.Content =
                new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

            try
            {
                using var response = await ApiHttpClient.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.StatusCode == (HttpStatusCode)429 ||
                    (response.StatusCode == HttpStatusCode.Forbidden &&
                     (responseBody.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                      responseBody.Contains("userRateLimitExceeded", StringComparison.OrdinalIgnoreCase) ||
                      responseBody.Contains("dailyLimitExceeded", StringComparison.OrdinalIgnoreCase))))
                {
                    throw new QuotaExceededException(TranslationEngineName.GoogleCloudTranslate,
                        "Google Cloud Translation quota exceeded (HTTP " + (int)response.StatusCode + ").");
                }

                if (response.IsSuccessStatusCode)
                {
                    return ParseTranslation(responseBody);
                }

                _logger?.LogInformation("{Message}", "[GCLOUD_HTTP_" + (int)response.StatusCode + "] " + responseBody);
                // A client error is the service saying no to this key or request;
                // retrying or silence would only hide why nothing is translated.
                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500 && (int)response.StatusCode != 408)
                    throw new ServiceRefusedException(TranslationEngineName.GoogleCloudTranslate, (int)response.StatusCode,
                        AI.OpenAIChatClient.DescribeRefusal((int)response.StatusCode, responseBody));
                return string.Empty;
            }
            catch (QuotaExceededException) { throw; }
            catch (MissingApiKeyException) { throw; }
            catch (ServiceRefusedException) { throw; }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger?.LogInformation("{Message}", "[GCLOUD_EXCEPTION] " + ex);
                return string.Empty;
            }
        }

        internal static string ParseTranslation(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            var translations = JToken.Parse(body)?["data"]?["translations"] as JArray;
            if (translations == null || translations.Count == 0)
                return string.Empty;

            return translations[0]?["translatedText"]?.ToString() ?? string.Empty;
        }
    }
}