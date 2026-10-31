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

namespace Translation.Providers.Azure
{
    internal sealed class AzureTranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.AzureTranslator;

        private const string Endpoint = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0";

        private readonly ILogger _logger;
        private readonly ITranslationCredentialStore _credentials;

        public AzureTranslator(ILogger logger, ITranslationCredentialStore credentials)
        {
            _logger = logger;
            _credentials = credentials ?? NullCredentialStore.Instance;
        }


        public async Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sentence))
                return string.Empty;

            var apiKey = _credentials.GetApiKey(TranslationEngineName.AzureTranslator);
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new MissingApiKeyException(TranslationEngineName.AzureTranslator);

            var region = _credentials.GetRegion(TranslationEngineName.AzureTranslator);
            var source = string.IsNullOrWhiteSpace(inLang) || inLang == "auto" ? string.Empty : inLang;
            var target = string.IsNullOrWhiteSpace(outLang) ? "en" : outLang;

            var url = Endpoint + "&to=" + Uri.EscapeDataString(target);
            if (!string.IsNullOrEmpty(source))
                url += "&from=" + Uri.EscapeDataString(source);

            var body = "[{\"Text\":" + JsonConvert.SerializeObject(sentence) + "}]";

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
                if (!string.IsNullOrWhiteSpace(region))
                    request.Headers.Add("Ocp-Apim-Subscription-Region", region);

                try
                {
                    using (var response = await ApiHttpClient.SendAsync(request, cancellationToken)
                               .ConfigureAwait(false))
                    {
                        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (response.StatusCode == (HttpStatusCode)429 ||
                            (response.StatusCode == HttpStatusCode.Forbidden &&
                             responseBody.IndexOf("quota", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            throw new QuotaExceededException(TranslationEngineName.AzureTranslator,
                                "Azure Translator quota exceeded (HTTP " + (int)response.StatusCode + ").");
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger?.LogInformation("{Message}",
                                "[AZURE_HTTP_" + (int)response.StatusCode + "] " + responseBody);

                            // A client error is the service saying no to this key or request;
                            // silence would only hide why nothing is translated.
                            var status = (int)response.StatusCode;
                            if (status >= 400 && status < 500 && status != 408)
                                throw new ServiceRefusedException(TranslationEngineName.AzureTranslator, status,
                                    AI.OpenAIChatClient.DescribeRefusal(status, responseBody));

                            return string.Empty;
                        }

                        return ParseTranslation(responseBody);
                    }
                }
                catch (QuotaExceededException)
                {
                    throw;
                }
                catch (MissingApiKeyException)
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
                    _logger?.LogInformation("{Message}", "[AZURE_EXCEPTION] " + ex);
                    return string.Empty;
                }
            }
        }

        internal static string ParseTranslation(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            var token = JToken.Parse(body);
            var arr = token as JArray;
            if (arr == null || arr.Count == 0)
                return string.Empty;

            var translations = arr[0]["translations"] as JArray;
            if (translations == null || translations.Count == 0)
                return string.Empty;

            return translations[0]?["text"]?.ToString() ?? string.Empty;
        }
    }
}