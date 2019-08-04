// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;

namespace FFXIITataruHelper
{
    static class GlobalSettings
    {
        public static string OldHotKeysFilePath = "../HotKeys.json";

        public static List<string> FFXIVLanguagles = new List<string>(new string[] { "English", "Chinese", "Japanese", "French", "German", "Korean" });

        public static string ChatCodesFilePath = @"Resources\ChatCodes.json";

        public static string GoogleTranslateLanguages = "Resources/GoogleTranslateLanguages.json";

        public static string MultillectTranslateLanguages = "Resources/MultillectTranslateLanguages.json";

        public static string DeeplLanguages = "Resources/DeeplLanguages.json";

        public static string YandexLanguages = "Resources/YandexTranslateLanguages.json";

        public static string LocalisationDirPath = @"Locale\";

        public static string ru_RU_LanguaguePath = @"ru_RU\ru_RU.mo";

        public static string en_US_LanguaguePath = @"en_US\en_US.mo";

        public static string es_ES_LanguaguePath = @"es_es\es_ES.mo";

        public static string pl_PL_LanguaguePath = @"pl_PL\pl_PL.mo";

        public static int SpiWaitTimeOutMS = 500;

        public static int LookForPorcessDelay = 500;

        public static int AutoHideWatcherDelay = 500;

        public static int MemoryReaderDelay = 33;

        public static int MaxСonsecutiveNotFromLogSentences = 10000;

        public static int TranslationDelay = 33;

        public static int TranslatorWaitTime = 2500;

        public static int MaxTranslatedSentencesCount = 1;

        public static int MaxTranslateTryCount = 3;

        public static string OldSettings = "../AppSettings.json";

        public static string Settings = "../UserSettings.json";

        public static string BlackList = @"Resources\MsgBlackList.json";
    }
}
