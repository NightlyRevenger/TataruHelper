using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Translation.Credentials;
using Translation.Models;
using Translation.Providers.Azure;
using Translation.Providers.Claude;
using Translation.Providers.DeepL;
using Translation.Providers.DeepSeek;
using Translation.Providers.Gemini;
using Translation.Providers.Google;
using Translation.Providers.GoogleCloud;
using Translation.Providers.LibreTranslate;
using Translation.Providers.Local;
using Translation.Providers.OpenAI;
using Translation.Providers.OpenRouter;
using Translation.Providers.Papago;
using Translation.Providers.Yandex;
using Translation.Providers.YandexCloud;
using Translation.Providers.YandexGpt;
using Translation.Settings;

namespace Translation.Providers
{
    internal static class TranslationProviderFactory
    {
        public static IReadOnlyDictionary<TranslationEngineName, ITranslationProvider> CreateDefaultProviders(
            ILogger logger,
            ITranslationCredentialStore credentials,
            TranslationSettings settings)
        {
            credentials = credentials ?? NullCredentialStore.Instance;
            settings = settings ?? new TranslationSettings();

            var google = new GoogleTranslator(logger, settings);
            var papago = new PapagoTranslator(logger, settings);
            var deepLF = new DeepLTranslator(logger, settings, credentials);
            var azure = new AzureTranslator(logger, credentials);
            var googleCloud = new GoogleCloudTranslator(logger, credentials);
            var deepLApi = new DeepLApiTranslator(logger, credentials);
            var openAi = new OpenAITranslator(logger, credentials);
            var deepSeek = new DeepSeekTranslator(logger, credentials);
            var yandexCloud = new YandexCloudTranslator(logger, credentials);
            var yandexGpt = new YandexGptTranslator(logger, credentials);
            var yandexFree = new YandexFreeTranslator(logger);
            var gemini = new GeminiTranslator(logger, credentials);
            var openRouter = new OpenRouterTranslator(logger, credentials);
            var claude = new ClaudeTranslator(logger, credentials);
            var libreTranslate = new LibreTranslateTranslator(logger, credentials, settings);
            var ollama = new OllamaTranslator(logger, credentials);
            var lmStudio = new LmStudioTranslator(logger, credentials);

            var providers = new ITranslationProvider[]
            {
                google, papago, deepLF, azure, googleCloud, deepLApi, openAi, deepSeek, yandexCloud, yandexGpt,
                yandexFree, gemini, openRouter, claude, libreTranslate, ollama, lmStudio,
            };

            return providers.ToDictionary(x => x.EngineName, x => x);
        }
    }
}