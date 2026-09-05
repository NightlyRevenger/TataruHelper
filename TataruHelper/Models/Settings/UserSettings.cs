using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using FFXIVTataruHelper.WinUtils;

using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace FFXIVTataruHelper
{
    public class UserSettings
    {
        //public static string BackgroundColor = "#90909090";
        public Color BackgroundColor { get; set; } = (Color)ColorConverter.ConvertFromString("#4B000000");

        public Color Font1Color { get; set; } = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

        public Color Font2Color { get; set; } = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

        public bool IsClickThrough { get; set; } = false;

        public bool IsAlwaysOnTop { get; set; } = true;

        public bool IsHideToTray { get; set; } = false;

        public bool IsAutoHide { get; set; } = false;

        public bool IsDirecMemoryReading { get; set; } = true;


        /// <summary>Use hand-made translations of the game's dialogue where they exist.</summary>

        public bool IsLiteraryTranslation { get; set; } = true;


        /// <summary>Mark lines an engine translated, so the hand-made ones stand out.</summary>

        public bool IsMachineTranslationMarked { get; set; }


        /// <summary>Show the speaker's name in the reading language too.</summary>

        public bool IsSpeakerNameTranslated { get; set; }


        /// <summary>Translate player-chat sender prefixes into the reading language.</summary>

        public bool IsPlayerNicknameTranslated { get; set; }


        /// <summary>
        /// Fetch new hand-made translations as they appear, rather than saying
        /// they exist and waiting to be told to.
        ///
        /// Off by default: the export is around a gigabyte, and helping
        /// yourself to somebody's connection for it is not a thing to do
        /// without being asked. Whether the project has moved is checked
        /// either way - that costs a few kilobytes once a day.
        /// </summary>

        public bool IsReferenceIndexAutoInstall { get; set; }

        public TimeSpan AutoHideTimeout { get; set; } = new TimeSpan(0, 5, 0);

        public int FontSize { get; set; } = 14;

        public List<Color> RecentBackgroundColors { get; set; } =
            new List<Color>(new Color[] { (Color)ColorConverter.ConvertFromString("#4B000000") });

        public int LineBreakHeight { get; set; } = 0;

        public int InsertSpaceCount { get; set; } = 0;

        public int CurrentTranslationEngine { get; set; } = 0;

        public string CurrentFFXIVLanguage { get; set; } = "English";

        public string CurrentTranslateToLanguage { get; set; } = "English";

        public int CurentUILanguague { get; set; } = (int)LanguageWrapper.Languages.English;

        public HotKeyCombination ShowHideChatKeys { get; set; } = new HotKeyCombination("ShowHideChatWin");

        public HotKeyCombination ClickThoughtChatKeys { get; set; } = new HotKeyCombination("ClickThoughtChatWin");

        public HotKeyCombination ClearChatKeys { get; set; } = new HotKeyCombination("ClearChatWin");

        public PointD SettingsWindowSize { get; set; } = new PointD(0.0, 0.0);

        public RectangleD ChatWindowLocation { get; set; } = new RectangleD(0.0, 0.0, 0.0, 0.0);

        public Dictionary<string, ChatMsgType> ChatCodes { get; set; } = new Dictionary<string, ChatMsgType>()
        {
            {
                "003D",
                new ChatMsgType("003D", MsgType.Translate, "NPCD", (Color)ColorConverter.ConvertFromString("#FFABD647"))
            },
            {
                "0044",
                new ChatMsgType("0044", MsgType.Translate, "NPCA", (Color)ColorConverter.ConvertFromString("#FFABD647"))
            },
        };

        public List<ChatWindowViewModelSettings> ChatWindows { get; set; }

        public int IsFirstTime { get; set; } = 0;

        public UserSettings()
        {
            BackgroundColor = (Color)ColorConverter.ConvertFromString("#4B000000");

            Font1Color = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

            Font2Color = (Color)ColorConverter.ConvertFromString("#FFFFFFFF");

            IsClickThrough = false;

            IsAlwaysOnTop = true;

            IsHideToTray = false;

            IsAutoHide = false;

            IsDirecMemoryReading = true;

            AutoHideTimeout = new TimeSpan(0, 5, 0);

            FontSize = 14;

            RecentBackgroundColors =
                new List<Color>(new Color[] { (Color)ColorConverter.ConvertFromString("#4B000000") });

            LineBreakHeight = 0;

            InsertSpaceCount = 0;

            CurrentTranslationEngine = 0;

            CurrentFFXIVLanguage = "English";

            CurrentTranslateToLanguage = "English";

            CurentUILanguague = (int)LanguageWrapper.Languages.English;

            SettingsWindowSize = new PointD(0.0, 0.0);

            ChatWindowLocation = new RectangleD(0.0, 0.0, 0.0, 0.0);

            ShowHideChatKeys = new HotKeyCombination("ShowHideChatWin");
            ClickThoughtChatKeys = new HotKeyCombination("ClickThoughtChatWin");
            ClearChatKeys = new HotKeyCombination("ClearChatWin");

            ChatCodes = new Dictionary<string, ChatMsgType>()
            {
                {
                    "003D", new ChatMsgType("003D", MsgType.Translate, "NPCD",
                        (Color)ColorConverter.ConvertFromString("#FFABD647"))
                },
                {
                    "0044", new ChatMsgType("0044", MsgType.Translate, "NPCA",
                        (Color)ColorConverter.ConvertFromString("#FFABD647"))
                },
            };

            ChatWindows = new List<ChatWindowViewModelSettings>();

            IsFirstTime = 0;
        }

        /// <summary>
        /// A copy of somebody's settings.
        ///
        /// Every property is copied, and a test walks them to say so. Three
        /// were being missed and two were wrong: the background colour was
        /// assigned to a local variable of the same name and thrown away, and
        /// direct memory reading was set to true rather than copied. Nothing
        /// uses this yet, which is the only reason none of it had been noticed.
        /// </summary>
        public UserSettings(UserSettings userSettings)
        {
            BackgroundColor = userSettings.BackgroundColor;

            Font1Color = userSettings.Font1Color;

            Font2Color = userSettings.Font2Color;

            IsClickThrough = userSettings.IsClickThrough;

            IsAlwaysOnTop = userSettings.IsAlwaysOnTop;

            IsHideToTray = userSettings.IsHideToTray;

            IsAutoHide = userSettings.IsAutoHide;

            IsDirecMemoryReading = userSettings.IsDirecMemoryReading;

            IsLiteraryTranslation = userSettings.IsLiteraryTranslation;

            IsMachineTranslationMarked = userSettings.IsMachineTranslationMarked;

            IsSpeakerNameTranslated = userSettings.IsSpeakerNameTranslated;

            IsPlayerNicknameTranslated = userSettings.IsPlayerNicknameTranslated;

            IsReferenceIndexAutoInstall = userSettings.IsReferenceIndexAutoInstall;

            AutoHideTimeout = userSettings.AutoHideTimeout;

            FontSize = userSettings.FontSize;

            RecentBackgroundColors = userSettings.RecentBackgroundColors.ToList();

            LineBreakHeight = userSettings.LineBreakHeight;

            InsertSpaceCount = userSettings.InsertSpaceCount;

            CurrentTranslationEngine = userSettings.CurrentTranslationEngine;

            CurrentFFXIVLanguage = userSettings.CurrentFFXIVLanguage;

            CurrentTranslateToLanguage = userSettings.CurrentTranslateToLanguage;

            CurentUILanguague = userSettings.CurentUILanguague;

            ShowHideChatKeys = new HotKeyCombination(userSettings.ShowHideChatKeys);
            ClickThoughtChatKeys = new HotKeyCombination(userSettings.ClickThoughtChatKeys);
            ClearChatKeys = new HotKeyCombination(userSettings.ClearChatKeys);

            SettingsWindowSize = userSettings.SettingsWindowSize;

            ChatWindowLocation = userSettings.ChatWindowLocation;

            ChatCodes = userSettings.ChatCodes.ToDictionary(entry => entry.Key, entry => new ChatMsgType(entry.Value));

            ChatWindows = userSettings.ChatWindows.Select(element => new ChatWindowViewModelSettings(element)).ToList();

            IsFirstTime = userSettings.IsFirstTime;
        }
    }
}
