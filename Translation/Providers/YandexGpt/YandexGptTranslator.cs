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
using Translation.Providers.AI;

namespace Translation.Providers.YandexGpt;

// YandexGPT (Yandex Foundation Models REST API).
// Docs: https://yandex.cloud/en/docs/foundation-models/text-generation/api-ref/TextGeneration/completion
// Endpoint: POST https://llm.api.cloud.yandex.net/foundationModels/v1/completion
// Auth:     Authorization: Api-Key <service-account-api-key>
//           x-folder-id:   <cloud-folder-id>
// Reuses the Yandex Cloud API key + folder id that are already configured for the
// Yandex Cloud Translate engine — both APIs accept the same credentials.
internal sealed class YandexGptTranslator : ITranslationProvider
{
    public TranslationEngineName EngineName => TranslationEngineName.YandexGPT;

    private const string Endpoint = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";
    private const string DefaultModelAlias = "yandexgpt-lite/latest";

    private readonly ILogger _logger;
    private readonly ITranslationCredentialStore _credentials;

    public YandexGptTranslator(ILogger logger, ITranslationCredentialStore credentials)
    {
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

        var apiKey = _credentials.GetApiKey(TranslationEngineName.Yandex);
        var folderId = _credentials.GetRegion(TranslationEngineName.Yandex);

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(folderId))
            throw new MissingApiKeyException(TranslationEngineName.YandexGPT);

        var configuredModel = _credentials.GetModel(TranslationEngineName.YandexGPT);
        var modelAlias = string.IsNullOrWhiteSpace(configuredModel) ? DefaultModelAlias : configuredModel;
        var modelUri = "gpt://" + folderId + "/" + modelAlias;

        var systemPrompt = FfxivTranslationPrompt.BuildSystemPrompt(inLang, outLang, kind);

        var payloadText = new JObject
        {
            ["modelUri"] = modelUri,
            ["completionOptions"] =
                new JObject { ["stream"] = false, ["temperature"] = 0.2, ["maxTokens"] = "2000", },
            ["messages"] = new JArray
            {
                new JObject { ["role"] = "system", ["text"] = systemPrompt },
                new JObject { ["role"] = "user", ["text"] = sentence },
            },
        }.ToString(Formatting.None);

        Exception lastException = null;

        for (var attempt = 1; attempt <= AiRetryPolicy.MaxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Content = new StringContent(payloadText, Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("Authorization", "Api-Key " + apiKey);
            request.Headers.TryAddWithoutValidation("x-folder-id", folderId);

            try
            {
                using var response = await ApiHttpClient.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var status = (int)response.StatusCode;

                if (response.StatusCode == (HttpStatusCode)429)
                {
                    throw new QuotaExceededException(TranslationEngineName.YandexGPT,
                        "YandexGPT quota exceeded (HTTP " + status + ").");
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("{Message}",
                        "[YandexGPT_HTTP_" + status + "_ATTEMPT_" + attempt + "] " + body);
                    if (AiRetryPolicy.IsTransientStatus(status) && attempt < AiRetryPolicy.MaxAttempts)
                    {
                        await AiRetryPolicy.DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (status >= 400 && status < 500 && status != 408)
                        throw new ServiceRefusedException(TranslationEngineName.YandexGPT, status,
                            AI.OpenAIChatClient.DescribeRefusal(status, body));

                    return string.Empty;
                }

                var parsed = ParseContent(body);
                if (!string.IsNullOrWhiteSpace(parsed))
                    return parsed;

                _logger?.LogInformation("{Message}", "[YandexGPT_EMPTY_CONTENT_ATTEMPT_" + attempt + "] " + body);

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
                _logger?.LogInformation("{Message}", "[YandexGPT_EXCEPTION_ATTEMPT_" + attempt + "] " + ex);

                if (attempt < AiRetryPolicy.MaxAttempts && AiRetryPolicy.IsTransientException(ex))
                {
                    await AiRetryPolicy.DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return string.Empty;
            }
        }

        _logger?.LogInformation("{Message}", "[YandexGPT_EXHAUSTED_RETRIES] " + lastException);
        return string.Empty;
    }

    internal static string ParseContent(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        var text = JToken.Parse(body)?["result"]?["alternatives"]?[0]?["message"]?["text"]?.ToString();
        return AiResponseSanitizer.StripWrappingArtifacts(text);
    }
}