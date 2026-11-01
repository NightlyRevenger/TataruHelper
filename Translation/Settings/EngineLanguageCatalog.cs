using System.Collections.Generic;

using Translation.Models;

namespace Translation.Settings
{
    /// <summary>
    /// Where one engine's language list lives, and how good the engine is.
    /// </summary>
    internal sealed class EngineLanguageSource
    {
        public EngineLanguageSource(TranslationEngineName engine, string languagesPath, double quality)
        {
            Engine = engine;
            LanguagesPath = languagesPath;
            Quality = quality;
        }

        public TranslationEngineName Engine { get; }

        public string LanguagesPath { get; }

        /// <summary>
        /// Ranks the engine when the selected one fails and a stand-in has to be
        /// found; the highest is tried first.
        /// </summary>
        public double Quality { get; }
    }

    /// <summary>
    /// Ties every engine to its language list and its rank, one line each.
    ///
    /// Both facts used to be spelled out at the call site: twelve strings of the
    /// same type handed positionally to one method, with the rank written beside
    /// the load. Nothing but the argument order tied a path to the engine it
    /// belonged to, and each new engine made that list longer.
    /// </summary>
    internal static class EngineLanguageCatalog
    {
        public static IReadOnlyList<EngineLanguageSource> From(TranslationSettings settings)
        {
            settings = settings ?? new TranslationSettings();

            return new[]
            {
                new EngineLanguageSource(TranslationEngineName.GoogleTranslate,
                    settings.GoogleTranslateLanguages, 9),
                // Fallback preference, based on a small EN/RU and JA/RU dialogue
                // sample and latency, rather than a universal quality score.
                new EngineLanguageSource(TranslationEngineName.TranslateCom, settings.AdditionalWebLanguages, 8.6),
                new EngineLanguageSource(TranslationEngineName.ModernMT, settings.AdditionalWebLanguages, 8.4),
                new EngineLanguageSource(TranslationEngineName.Tencent, settings.AdditionalWebLanguages, 6.6),
                new EngineLanguageSource(TranslationEngineName.Volcengine, settings.AdditionalWebLanguages, 6.4),
                new EngineLanguageSource(TranslationEngineName.Alibaba, settings.AdditionalWebLanguages, 6.2),
                new EngineLanguageSource(TranslationEngineName.Bing,
                    settings.BingLanguages, 8.5),
                new EngineLanguageSource(TranslationEngineName.Papago,
                    settings.PapagoLanguages, 6),
                new EngineLanguageSource(TranslationEngineName.DeepL,
                    settings.DeepLLanguages, 10),
                new EngineLanguageSource(TranslationEngineName.AzureTranslator,
                    settings.AzureTranslatorLanguages, 9),
                new EngineLanguageSource(TranslationEngineName.GoogleCloudTranslate,
                    settings.GoogleCloudTranslateLanguages, 9),
                new EngineLanguageSource(TranslationEngineName.DeepLApi,
                    settings.DeepLApiLanguages, 10),
                new EngineLanguageSource(TranslationEngineName.OpenAI,
                    settings.OpenAILanguages, 8),
                new EngineLanguageSource(TranslationEngineName.DeepSeek,
                    settings.DeepSeekLanguages, 7),
                new EngineLanguageSource(TranslationEngineName.OpenRouter,
                    settings.OpenRouterLanguages, 8),
                new EngineLanguageSource(TranslationEngineName.Claude,
                    settings.ClaudeLanguages, 9),

                // Ranked below the hosted engines as stand-ins: whatever model
                // the player happens to be running is the one thing here we
                // cannot judge the quality of.
                new EngineLanguageSource(TranslationEngineName.LibreTranslate,
                    settings.LibreTranslateLanguages, 6),
                new EngineLanguageSource(TranslationEngineName.Ollama,
                    settings.OllamaLanguages, 6),
                new EngineLanguageSource(TranslationEngineName.LmStudio,
                    settings.LmStudioLanguages, 6),
                new EngineLanguageSource(TranslationEngineName.Yandex,
                    settings.YandexCloudLanguages, 8),
                new EngineLanguageSource(TranslationEngineName.YandexGPT,
                    settings.YandexGptLanguages, 8),
                new EngineLanguageSource(TranslationEngineName.Gemini,
                    settings.GeminiLanguages, 8),

                // Ranked above Google: on game dialogue it reads more naturally and
                // answers in about a fifth of the time (250ms against 1250ms).
                new EngineLanguageSource(TranslationEngineName.YandexFree,
                    settings.YandexLanguages, 9.5),
            };
        }
    }
}
