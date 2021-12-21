// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;

namespace Translation
{
    static class GlobalTranslationSettings
    {
        public static int TranslationCacheSize = 10000;

        public static double MaxSameLanguagePercent = 0.40;

        public static string NTextCatLanguageModelsPath = "TranslationResources/Core14.profile.xml";

        public static string BaiduEncoder = "TranslationResources/BaiduEncoder";

        public static string PapagoEncoderPath = "TranslationResources/PapagoEncoder";

        public static string GoogleTranslateLanguages = "TranslationResources/GoogleTranslateLanguages.json";

        public static string DeeplLanguages = "TranslationResources/DeeplLanguages.json";

        public static string BaiduLanguages = "TranslationResources/BaiduLanguages.json";

        public static string PapagoLanguages = "TranslationResources/PapagoLanguages.json";

        public static string YandexLanguages = "TranslationResources/YandexTranslateLanguages.json";

        public static string YandexAuthFile = "TranslationResources/YandexAuth";

        public static string YandexUsersFile = "TranslationResources/YandexUsers.json";

        public static string YandexEncoderPath = "TranslationResources/YandexEncoder";
    }
}
