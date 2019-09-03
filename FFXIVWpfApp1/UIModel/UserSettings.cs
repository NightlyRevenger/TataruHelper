// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIVTataruHelper.WinUtils;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using FFXIVTataruHelper.UIModel;

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

        public TimeSpan AutoHideTimeout { get; set; } = new TimeSpan(0, 5, 0);

        public int FontSize { get; set; } = 14;

        public List<Color> RecentBackgroundColors { get; set; } = new List<Color>(new Color[] { (Color)ColorConverter.ConvertFromString("#4B000000") });

        public int LineBreakHeight { get; set; } = 0;

        public int InsertSpaceCount { get; set; } = 0;

        public int CurrentTranslationEngine { get; set; } = 0;

        public string CurrentFFXIVLanguage { get; set; } = "English";

        public string CurrentTranslateToLanguage { get; set; } = "English";

        public int CurentUILanguague { get; set; } = (int)LanguagueWrapper.Languages.English;

        public HotKeyCombination ShowHideChatKeys { get; set; } = new HotKeyCombination("ShowHideChatWin");

        public HotKeyCombination ClickThoughtChatKeys { get; set; } = new HotKeyCombination("ClickThoughtChatWin");

        public HotKeyCombination ClearChatKeys { get; set; } = new HotKeyCombination("ClearChatWin");

        public System.Drawing.PointD SettingsWindowSize { get; set; } = new System.Drawing.PointD(0.0, 0.0);

        public System.Drawing.RectangleD ChatWindowLocation { get; set; } = new System.Drawing.RectangleD(0.0, 0.0, 0.0, 0.0);

        public Dictionary<string, ChatMsgType> ChatCodes { get; set; } = new Dictionary<string, ChatMsgType>()
        {
            { "003D", new ChatMsgType("003D", MsgType.Translate,"NPCD",(Color)ColorConverter.ConvertFromString("#FFABD647")) },
            { "0044",  new ChatMsgType("0044", MsgType.Translate,"NPCA",(Color)ColorConverter.ConvertFromString("#FFABD647")) },
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

            RecentBackgroundColors = new List<Color>(new Color[] { (Color)ColorConverter.ConvertFromString("#4B000000") });

            LineBreakHeight = 0;

            InsertSpaceCount = 0;

            CurrentTranslationEngine = 0;

            CurrentFFXIVLanguage = "English";

            CurrentTranslateToLanguage = "English";

            CurentUILanguague = (int)LanguagueWrapper.Languages.English;

            SettingsWindowSize = new System.Drawing.PointD(0.0, 0.0);

            ChatWindowLocation = new System.Drawing.RectangleD(0.0, 0.0, 0.0, 0.0);

            ShowHideChatKeys = new HotKeyCombination("ShowHideChatWin");
            ClickThoughtChatKeys = new HotKeyCombination("ClickThoughtChatWin");
            ClearChatKeys = new HotKeyCombination("ClearChatWin");

            ChatCodes = new Dictionary<string, ChatMsgType>()
            {
                { "003D", new ChatMsgType("003D", MsgType.Translate,"NPCD",(Color)ColorConverter.ConvertFromString("#FFABD647")) },
                { "0044",  new ChatMsgType("0044", MsgType.Translate,"NPCA",(Color)ColorConverter.ConvertFromString("#FFABD647")) },
            };

            //ChatWindows = new List<ChatWindowViewModelSettings>(new ChatWindowViewModelSettings[] { new ChatWindowViewModelSettings() });

            ChatWindows = new List<ChatWindowViewModelSettings>();

            IsFirstTime = 0;
        }

        public UserSettings(UserSettings userSettings)
        {
            Color BackgroundColor = userSettings.BackgroundColor;

            Font1Color = userSettings.Font1Color;

            Font2Color = userSettings.Font2Color;

            IsClickThrough = userSettings.IsClickThrough;

            IsAlwaysOnTop = userSettings.IsAlwaysOnTop;

            IsHideToTray = userSettings.IsHideToTray;

            IsAutoHide = userSettings.IsAutoHide;

            IsDirecMemoryReading = userSettings.IsDirecMemoryReading;

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

            SettingsWindowSize = userSettings.SettingsWindowSize;

            ChatWindowLocation = userSettings.ChatWindowLocation;

            ChatCodes = userSettings.ChatCodes.ToDictionary(entry => entry.Key, entry => new ChatMsgType(entry.Value));

            ChatWindows = userSettings.ChatWindows.Select(element => new ChatWindowViewModelSettings(element)).ToList();

            IsFirstTime = userSettings.IsFirstTime;
        }
    }
}
