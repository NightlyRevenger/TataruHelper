using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Translation.Models
{
    public enum TranslationEngineName : int
    {
        GoogleTranslate = 0,
        Yandex = 3,
        Papago = 5,
        AzureTranslator = 7,
        GoogleCloudTranslate = 8,
        DeepLApi = 9,
        OpenAI = 10,
        DeepSeek = 11,
        YandexGPT = 12,
        DeepL = 13,

        /// <summary>Keyless Yandex, as opposed to <see cref="Yandex"/> which is Yandex Cloud.</summary>
        YandexFree = 14,

        Gemini = 15,

        OpenRouter = 16,

        /// <summary>Runs on the player's own machine; no key, no quota, no network.</summary>
        Ollama = 17,

        /// <summary>The other common local runner. Same shape as <see cref="Ollama"/>, different port.</summary>
        LmStudio = 18,

        Claude = 19,

        /// <summary>An open translation server the player points at. Needs an address, not a key.</summary>
        LibreTranslate = 20,

        Bing = 21,
        TranslateCom = 22,
        ModernMT = 23,
        Tencent = 24,
        Volcengine = 25,
        Alibaba = 26,
    }

    public class TranslationEngine : IEquatable<TranslationEngine>
    {
        public string Name
        {
            get { return GetDisplayName(EngineName); }
        }

        /// <summary>
        /// Name shown in the engine picker. The enum member names cannot be used
        /// directly: "Yandex" is the credentialed Yandex Cloud service while
        /// "YandexFree" is the keyless one, and showing them that way led users
        /// straight to the engine that fails without an API key.
        /// </summary>
        public static string GetDisplayName(TranslationEngineName engineName)
        {
            switch (engineName)
            {
                case TranslationEngineName.TranslateCom: return "Translate.com";
                case TranslationEngineName.Tencent: return "Tencent TranSmart";
                case TranslationEngineName.Alibaba: return "Alibaba Translate";
                case TranslationEngineName.YandexFree:
                    return "Yandex";
                case TranslationEngineName.Yandex:
                    return "Yandex Cloud";
                case TranslationEngineName.GoogleTranslate:
                    return "Google Translate";
                case TranslationEngineName.GoogleCloudTranslate:
                    return "Google Cloud Translate";
                case TranslationEngineName.DeepLApi:
                    return "DeepL API";
                case TranslationEngineName.AzureTranslator:
                    return "Azure Translator";
                case TranslationEngineName.YandexGPT:
                    return "YandexGPT";
                case TranslationEngineName.LmStudio:
                    return "LM Studio";
                default:
                    return engineName.ToString();
            }
        }

        public ReadOnlyCollection<TranslatorLanguage> SupportedLanguages
        {
            get { return _SupportedLanguages; }
        }

        public TranslationEngineName EngineName { get; private set; }
        public double Quality { get; private set; }

        ReadOnlyCollection<TranslatorLanguage> _SupportedLanguages;

        public TranslationEngine(TranslationEngineName translationEngineName,
            List<TranslatorLanguage> translatorLanguages, double quality)
        {
            this.EngineName = translationEngineName;
            _SupportedLanguages = new ReadOnlyCollection<TranslatorLanguage>(translatorLanguages);
            Quality = quality;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as TranslationEngine);
        }

        public bool Equals(TranslationEngine engine)
        {
            if (ReferenceEquals(engine, null))
                return false;

            if (ReferenceEquals(this, engine))
                return true;

            if (this.GetType() != engine.GetType())
                return false;

            return this.EngineName == engine.EngineName;
        }

        public static bool operator ==(TranslationEngine left, TranslationEngine right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null))
                return false;

            if (ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(TranslationEngine left, TranslationEngine right) => !(left == right);

        public override int GetHashCode()
        {
            return ((int)EngineName).GetHashCode();
        }
    }
}