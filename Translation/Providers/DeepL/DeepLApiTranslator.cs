using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using Translation.Credentials;
using Translation.Exceptions;
using Translation.Http;
using Translation.Models;

namespace Translation.Providers.DeepL
{
    internal sealed class DeepLApiTranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.DeepLApi;

        private const string FreeEndpoint = "https://api-free.deepl.com/v2/translate";
        private const string ProEndpoint = "https://api.deepl.com/v2/translate";

        private readonly ILogger _logger;
        private readonly ITranslationCredentialStore _credentials;

        public DeepLApiTranslator(ILogger logger, ITranslationCredentialStore credentials)
        {
            _logger = logger;
            _credentials = credentials ?? NullCredentialStore.Instance;
        }


        public async Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sentence))
                return string.Empty;

            var apiKey = _credentials.GetApiKey(TranslationEngineName.DeepLApi);
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new MissingApiKeyException(TranslationEngineName.DeepLApi);

            var endpoint = apiKey.TrimEnd().EndsWith(":fx", StringComparison.OrdinalIgnoreCase)
                ? FreeEndpoint
                : ProEndpoint;
            var target = string.IsNullOrWhiteSpace(outLang) ? "EN" : outLang.ToUpperInvariant();
            var source = string.IsNullOrWhiteSpace(inLang) || inLang == "auto" ? null : inLang.ToUpperInvariant();

            var fields = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("text", sentence),
                new KeyValuePair<string, string>("target_lang", target),
            };
            if (source != null)
                fields.Add(new KeyValuePair<string, string>("source_lang", source));

            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                request.Content = new FormUrlEncodedContent(fields);
                request.Headers.TryAddWithoutValidation("Authorization", "DeepL-Auth-Key " + apiKey);

                try
                {
                    using (var response = await ApiHttpClient.SendAsync(request, cancellationToken)
                               .ConfigureAwait(false))
                    {
                        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (response.StatusCode == (HttpStatusCode)429 || (int)response.StatusCode == 456)
                        {
                            throw new QuotaExceededException(TranslationEngineName.DeepLApi,
                                "DeepL API quota exceeded (HTTP " + (int)response.StatusCode + ").");
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger?.LogInformation("{Message}",
                                "[DEEPLAPI_HTTP_" + (int)response.StatusCode + "] " + responseBody);
                            // A client error is the service saying no to this key or request;
                            // retrying or silence would only hide why nothing is translated.
                            if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500 && (int)response.StatusCode != 408)
                                throw new ServiceRefusedException(TranslationEngineName.DeepLApi, (int)response.StatusCode,
                                    AI.OpenAIChatClient.DescribeRefusal((int)response.StatusCode, responseBody));
                            return string.Empty;
                        }

                        return ParseTranslation(responseBody);
                    }
                }
                catch (QuotaExceededException) { throw; }
                catch (MissingApiKeyException) { throw; }
                catch (ServiceRefusedException) { throw; }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    _logger?.LogInformation("{Message}", "[DEEPLAPI_EXCEPTION] " + ex);
                    return string.Empty;
                }
            }
        }

        internal static string ParseTranslation(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            var translations = JToken.Parse(body)?["translations"] as JArray;
            if (translations == null || translations.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var t in translations)
            {
                var text = t?["text"]?.ToString();
                if (!string.IsNullOrEmpty(text))
                    sb.Append(text);
            }

            return sb.ToString();
        }
    }
}