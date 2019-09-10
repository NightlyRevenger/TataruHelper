// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System.Collections.Generic;

namespace FFXIVTataruHelper
{
    static class GlobalSettings
    {
        public static string OldHotKeysFilePath = "../HotKeys.json";

        public static List<string> FFXIVLanguages = new List<string>(new string[] { "English", "Chinese", "Japanese", "French", "German", "Korean" });

        public static string ChatCodesFilePath = @"Resources\ChatCodes.json";

        public static string GoogleTranslateLanguages = "Resources/GoogleTranslateLanguages.json";

        public static string MultillectTranslateLanguages = "Resources/MultillectTranslateLanguages.json";

        public static string DeeplLanguages = "Resources/DeeplLanguages.json";

        public static string YandexLanguages = "Resources/YandexTranslateLanguages.json";

        public static string BaiduLanguages = "Resources/BaiduLanguages.json";

        public static string BaiduEncoder = "Resources/BaiduEncoder.js";

        public static string AmazonLanguages = "Resources/AmazonLanguages.json";

        public static string PapagoLanguages = "Resources/PapagoLanguages.json";

        public static string PapagoEncoderPath = "Resources/PapagoEncoder";

        public static string LocalisationDirPath = @"Locale_cloud\";

        public static string ru_RU_LanguaguePath = @"ru\ru_RU.mo";

        public static string en_US_LanguaguePath = @"en\en_US.mo";

        public static string es_ES_LanguaguePath = @"es-ES\es_ES.mo";

        public static string pl_PL_LanguaguePath = @"pl\pl_PL.mo";

        public static string ko_KR_LanguaguePath = @"ko\ko_KR.mo";

        public static string pt_BR_LanguaguePath = @"pt-BR\pt_BR.mo";

        public static int SpiWaitTimeOutMS = 500;

        public static int LookForPorcessDelay = 500;

        public static int AutoHideWatcherDelay = 500;

        public static int AutoTimeOutHideMinValueSeconds = 1;

        public static int MemoryReaderDelay = 33;

        public static int MaxСonsecutiveNotFromLogSentences = 100000;

        public static int TranslationDelay = 33;

        public static int TranslatorWaitTime = 5000;

        public static int SettingsSaveDelay = 2500;

        public static int MaxTranslateTryCount = 4;

        public static string OldSettings = "../UserSettings.json";

        public static string Settings = "../UserSettingsNew.json";

        public static string BlackList = @"Resources\MsgBlackList.json";

        public static string IgnoreNickNameChatCodes = @"Resources\IgnoreNickNameChatCodes.json";
    }
}
