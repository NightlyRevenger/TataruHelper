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
        public static string BackgroundColor = "#90909090";

        public static string Font1Color = "#FFFFFFFF";

        public static string Font2Color = "#FF000000";

        public static bool IsClickThrough = false;

        public static bool IsAlwaysOnTop = false;

        public static bool IsHideToTray = false;

        public static int FontSize = 24;

        public static List<string> RecentBackgroundColors = new List<string>(new string[] { "#00FFFFFF" });

        public static List<string> RecentFont1Colors = new List<string>(new string[] { "#FFFFFFFF" });
        public static List<string> RecentFont2Colors = new List<string>(new string[] { "#FF000000" });

        public static string HotKeysFilePath = "HotKeys.json";

        public static int LineBreakHeight = 0;

        public static int InsertSpaceCount = 0;

        public static int CurrentTranslationEngine = 0;

        public static int CurrentFFXIVLanguage = 0;

        public static int CurrentTranslateToLanguage = 0;

        public static List<string> FFXIVLanguagles = new List<string>(new string[] { "English", "Chinese", "Japanese", "French", "German", "Korean" });

        public static string ChatCodesFilePath = "ChatCodes.json";

        public static string GoogleTranslateLanguages = "Resources/GoogleTranslateLanguages.json";

        public static string MultillectTranslateLanguages = "Resources/MultillectTranslateLanguages.json";

        public static string DeeplLanguages = "Resources/DeeplLanguages.json";

        public static int CurentLanguague = (int)LanguagueWrapper.Languages.English;

        public static string LocalisationDirPath = @"Locale\";

        public static string ru_RU_LanguaguePath = @"ru_RU\ru_RU.mo";

        public static string en_US_LanguaguePath = @"en_US\en_US.mo";

        public static int SpiWaitTimeOutMS = 1000;

        public static int LookForPorcessDelay = 500;

        public static int MemoryReaderDelay = 33;

        public static int TranslationDelay = 33;

        public static int MaxTranslatedSentencesCount = 1;

        public static double MainWinHeight = 0;

        public static double MainWinWidth = 0;

        public static double ChatWinTop = 0;

        public static double ChatWinLeft = 0;

        public static double ChatWinHeight = 0;

        public static double ChatWinWidth = 0;
    }
}
