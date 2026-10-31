namespace Translation.Settings
{
    public sealed class TranslationSettings
    {
        public int TranslationCacheSize { get; set; } = 10000;

        public int HttpRequestTimeoutMilliseconds { get; set; } = 10000;

        public int HttpReadWriteTimeoutMilliseconds { get; set; } = 30000;

        public int HttpRequestRetryCount { get; set; } = 2;

        public int HttpRequestRetryDelayMilliseconds { get; set; } = 750;

        public int TranslationRetryCount { get; set; } = 2;

        public int TranslationRetryDelayMilliseconds { get; set; } = 500;

        public double MaxSameLanguagePercent { get; set; } = 0.40;

        public bool UseGoogleTranslateHtmlEndpoint { get; set; } = true;

        public bool UseGoogleJsonEndpoint { get; set; } = true;

        public bool UseGoogleHtmlFallbackEndpoint { get; set; } = true;

        /// <summary>
        /// Index of translations made by hand. Consulted before a translator
        /// when the literary translation option is on, and fetched by the
        /// update button on the General page.
        ///
        /// Where an update writes. Kept with the user's settings rather than
        /// with the application, which is replaced wholesale when it updates -
        /// an index beside it would go with it, and a fetch would be undone by
        /// the next release without a word about it.
        /// </summary>
        public string ReferenceTranslationsPath { get; set; } =
            "%APPDATA%/TataruHelper/ReferenceTranslations.db";

        /// <summary>
        /// The index the application was installed with, beside the executable.
        /// Read but never written to: an update replaces that folder, and it is
        /// also the only copy left if the translation project ever goes away.
        /// </summary>
        public string ShippedReferenceTranslationsPath { get; set; } = "Resources/ReferenceTranslations.db";

        /// <summary>
        /// The language to rebuild the index in, as the translation project
        /// names its files. Only Russian has a translation worth reading for
        /// now; an index already built says its own language, and this is what
        /// is used when there is no index yet to ask.
        /// </summary>
        public string ReferenceTranslationsLanguage { get; set; } = "ru";

        /// <summary>
        /// The language the index is keyed on when there is no index to ask and
        /// no window to read it off: the one the game is played in. FFXIV ships
        /// in English, German, French and Japanese, and the export carries all
        /// four, so a line read off a German client is looked up in German.
        /// </summary>
        public string ReferenceTranslationsGameLanguage { get; set; } = "en";

        public string NTextCatLanguageModelsPath { get; set; } = "TranslationResources/Core14.profile.xml";

        public string PapagoEncoderPath { get; set; } = "TranslationResources/PapagoEncoder";

        public string PapagoKeyCachePath { get; set; } = "PapagoKey.cache";

        public string GoogleTranslateLanguages { get; set; } = "TranslationResources/GoogleTranslateLanguages.json";

        public string PapagoLanguages { get; set; } = "TranslationResources/PapagoLanguages.json";

        public string AzureTranslatorLanguages { get; set; } = "TranslationResources/AzureTranslatorLanguages.json";

        public string GoogleCloudTranslateLanguages { get; set; } =
            "TranslationResources/GoogleTranslateLanguages.json";

        public string DeepLApiLanguages { get; set; } = "TranslationResources/DeeplLanguages.json";

        public string DeepLLanguages { get; set; } = "TranslationResources/DeeplLanguages.json";

        public string OpenAILanguages { get; set; } = "TranslationResources/GoogleTranslateLanguages.json";

        public string DeepSeekLanguages { get; set; } = "TranslationResources/GoogleTranslateLanguages.json";

        public string OpenRouterLanguages { get; set; } = "TranslationResources/GoogleTranslateLanguages.json";

        public string ClaudeLanguages { get; set; } = "TranslationResources/GoogleTranslateLanguages.json";

        public string LibreTranslateLanguages { get; set; } =
            "TranslationResources/LibreTranslateLanguages.json";

        public string OllamaLanguages { get; set; } = "TranslationResources/GoogleTranslateLanguages.json";

        public string LmStudioLanguages { get; set; } = "TranslationResources/GoogleTranslateLanguages.json";

        public string YandexGptLanguages { get; set; } = "TranslationResources/GoogleTranslateLanguages.json";

        public string GeminiLanguages { get; set; } = "TranslationResources/GoogleTranslateLanguages.json";

        public string YandexLanguages { get; set; } = "TranslationResources/YandexTranslateLanguages.json";

        public string YandexCloudLanguages { get; set; } = "TranslationResources/YandexCloudLanguages.json";

        public string YandexAuthFile { get; set; } = "TranslationResources/YandexAuth";

        public string YandexUsersFile { get; set; } = "TranslationResources/YandexUsers.json";

        public string YandexEncoderPath { get; set; } = "TranslationResources/YandexEncoder";
    }
}