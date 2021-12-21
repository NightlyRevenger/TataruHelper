// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using NGettext;
using System;
using System.IO;
using System.Windows;

namespace FFXIVTataruHelper
{
    public class LanguagueWrapper
    {
        Window _SettingsWindow;

        public enum Languages : int
        {
            None = 0,
            Russian = 1,
            English = 2,
            Spanish = 3,
            Polish = 4,
            Korean = 5,
            PortugueseBR = 6,
            Catalan = 7,
            Italian = 8,
            Ukrainian = 9,
            Chinese = 10,
            ChineseTR = 11,
            Japanese = 12,
        };

        public Languages CurrentLanguage
        {
            get { return _CurrentLanguage; }

            set
            {
                if (value != _CurrentLanguage || true)
                {
                    SetLanguague(value);

                    _CurrentLanguage = value;
                }
            }
        }

        Languages _CurrentLanguage;

        string _DirPath = GlobalSettings.LocalisationDirPath;

        public LanguagueWrapper(Window window)
        {
            _SettingsWindow = window;
            _CurrentLanguage = Languages.None;
        }

        private void SetLanguague(Languages languague)
        {
            string path = _DirPath;

            if (languague != Languages.None)
            {
                switch (languague)
                {
                    case Languages.English:
                        path += GlobalSettings.en_US_LanguaguePath;
                        break;

                    case Languages.Russian:
                        path += GlobalSettings.ru_RU_LanguaguePath;
                        break;

                    case Languages.Spanish:
                        path += GlobalSettings.es_ES_LanguaguePath;
                        break;

                    case Languages.Polish:
                        path += GlobalSettings.pl_PL_LanguaguePath;
                        break;

                    case Languages.Korean:
                        path += GlobalSettings.ko_KR_LanguaguePath;
                        break;

                    case Languages.PortugueseBR:
                        path += GlobalSettings.pt_BR_LanguaguePath;
                        break;
                    case Languages.Catalan:
                        path += GlobalSettings.ca_Es_LanguaguePath;
                        break;
                    case Languages.Italian:
                        path += GlobalSettings.it_IT_LanguaguePath;
                        break;
                    case Languages.Ukrainian:
                        path += GlobalSettings.uk_UA_LanguaguePath;
                        break;
                    case Languages.Chinese:
                        path += GlobalSettings.zh_CN_LanguaguePath;
                        break;
                    case Languages.ChineseTR:
                        path += GlobalSettings.zh_TR_LanguaguePath;
                        break;
                    case Languages.Japanese:
                        path += GlobalSettings.ja_LanguaguePath;
                        break;

                    default:
                        path += GlobalSettings.en_US_LanguaguePath;
                        break;
                }

                LoadDynamicTranslation(path);
            }
        }


        private void LoadDynamicTranslation(string path)
        {
            ICatalog catalog = new Catalog();
            try
            {
                using (var fs = File.Open(path, System.IO.FileMode.Open))
                {
                    catalog = new Catalog(fs);
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }

            _SettingsWindow.Resources["SettingsWindowName"] = catalog.GetString("Settings");
            _SettingsWindow.Resources["ChatWindowName"] = catalog.GetString("Chat Window");
            _SettingsWindow.Resources["StreamWindowName"] = catalog.GetString("Stream Chat Window");

            _SettingsWindow.Resources["DocLanguage"] = catalog.GetString("Language");
            //_SettingsWindow.Resources["DocLanguageEn"] = catalog.GetString("English");
            //_SettingsWindow.Resources["DocLanguageRu"] = catalog.GetString("Russian");

            _SettingsWindow.Resources["DockHelp"] = catalog.GetString("Help");

            _SettingsWindow.Resources["ChatAppearance"] = catalog.GetString("Chat Appearance");
            _SettingsWindow.Resources["TranslationSettings"] = catalog.GetString("Translation Settings");
            _SettingsWindow.Resources["GeneralBehavior"] = catalog.GetString("General");

            _SettingsWindow.Resources["ChatWindowBehavior"] = catalog.GetString("Behavior");
            _SettingsWindow.Resources["ChatWindowHotkeys"] = catalog.GetString("Hotkeys");

            _SettingsWindow.Resources["FontSettings"] = catalog.GetString("Font Settings");
            _SettingsWindow.Resources["FontSize"] = catalog.GetString("Font Size");

            _SettingsWindow.Resources["FontColor"] = catalog.GetString("Font Color");
            _SettingsWindow.Resources["FontColor1"] = catalog.GetString("Color 1");
            _SettingsWindow.Resources["FontColor2"] = catalog.GetString("Color 2");

            _SettingsWindow.Resources["FontFamily"] = catalog.GetString("Font");

            _SettingsWindow.Resources["BackgroundColor"] = catalog.GetString("Background Color");

            _SettingsWindow.Resources["ColorPickerStandardButtonHeader"] = catalog.GetString("Standard");
            _SettingsWindow.Resources["ColorPickerAdvancedButtonHeader"] = catalog.GetString("Advanced");
            _SettingsWindow.Resources["ColorPickerAvailableColorsHeader"] = catalog.GetString("Available Colors");
            _SettingsWindow.Resources["ColorPickerStandardColorsHeader"] = catalog.GetString("Standard Colors");
            _SettingsWindow.Resources["ColorPickerRecentColorsHeader"] = catalog.GetString("Recent Colors");

            _SettingsWindow.Resources["ParagraphSettings"] = catalog.GetString("Paragraph Settings");
            _SettingsWindow.Resources["IntervalWidth"] = catalog.GetString("Spacing");
            _SettingsWindow.Resources["LineBreakHeight"] = catalog.GetString("Line Break Height");

            _SettingsWindow.Resources["ChatCodes"] = catalog.GetString("Chat Codes");

            _SettingsWindow.Resources["TranslationEngine"] = catalog.GetString("Translation Engine");
            _SettingsWindow.Resources["FFLanguage"] = catalog.GetString("FF Language");
            _SettingsWindow.Resources["TraslateTo"] = catalog.GetString("Translate to");

            _SettingsWindow.Resources["ShowHideChatWindowHK"] = catalog.GetString("Show/hide Chat Window");
            _SettingsWindow.Resources["ShowHideChatWindowHKToolTip"] = catalog.GetString("Hotkey to hide a Chat Box when it is not needed and call it up when it is needed. You should use Ctrl/Shift/Alt + Any key or combination of Ctrl+Shift+Alt + Any key, etc. The key combinations must not be repeated. If you cannot assign a key, it is occupied by the application or system. Example: CTRL+Q, CTRL+ALT+R, SHIFT+ALT+CTRL+T, ALT+SHIFT+Y, SHIFT+CTRL+G, etc.");

            _SettingsWindow.Resources["ClickThroughHK"] = catalog.GetString("Click Through");
            _SettingsWindow.Resources["ClickThroughHKToolTip"] = catalog.GetString("Hotkey to turn on/off clicks through the windows. You should use Ctrl/Shift/Alt + Any key or combination of Ctrl+Shift+Alt + Any key, etc. The key combinations must not be repeated. If you cannot assign a key, it is occupied by the application or system. Example: CTRL+Q, CTRL+ALT+R, SHIFT+ALT+CTRL+T, ALT+SHIFT+Y, SHIFT+CTRL+G, etc.");

            _SettingsWindow.Resources["ClearChatHK"] = catalog.GetString("Clear Chat");
            _SettingsWindow.Resources["ClearChatHKToolTip"] = catalog.GetString("Hotkey to clear any text in the chatbox. You should use Ctrl/Shift/Alt + Any key or combination of Ctrl+Shift+Alt + Any key, etc. The key combinations must not be repeated. If you cannot assign a key, it is occupied by the application or system. Example: CTRL+Q, CTRL+ALT+R, SHIFT+ALT+CTRL+T, ALT+SHIFT+Y, SHIFT+CTRL+G, etc.");

            _SettingsWindow.Resources["OtherSett"] = catalog.GetString("Other");
            _SettingsWindow.Resources["ClickThroughCB"] = catalog.GetString("Click Through");
            _SettingsWindow.Resources["AlwaysOnTopCB"] = catalog.GetString("Always On Top");
            _SettingsWindow.Resources["AutoHideCB"] = catalog.GetString("Auto Hide");

            _SettingsWindow.Resources["DirectMemoryCB"] = catalog.GetString("Cutscenes/No delay");
            _SettingsWindow.Resources["DirectMemoryToolTip"] = catalog.GetString("EXPERIMENTAL FUNCTION. If this option is active, the application will catch text from quest clouds and cutscene subtitles directly, not from the chatlog. Errors can occur.");

            _SettingsWindow.Resources["AlwaysOnTopCB"] = catalog.GetString("Always On Top");
            _SettingsWindow.Resources["HideToTrayCB"] = catalog.GetString("Hide to Tray");
            _SettingsWindow.Resources["ShowTimestampsCb"] = catalog.GetString("Show Timestamps");

            _SettingsWindow.Resources["StreamerWindowCB"] = catalog.GetString("Streamer Window");

            _SettingsWindow.Resources["ShowChatBox"] = catalog.GetString("Show Chat Window");
            _SettingsWindow.Resources["HideChatBox"] = catalog.GetString("Hide Chat Window");

            _SettingsWindow.Resources["ResetChatPosition"] = catalog.GetString("Reset Chat Position");

            _SettingsWindow.Resources["TranslationEngineError"] = catalog.GetString("Translation engine error. Consider switching to other engine.");

            _SettingsWindow.Resources["FFStatusLable"] = catalog.GetString("FF Status:");

            _SettingsWindow.Resources["FFStatusText"] = catalog.GetString("FFXIV process not found. Make sure that FFXIV is running using DirectX 11.");

            _SettingsWindow.Resources["FFStatusTextFound"] = catalog.GetString("Process found:");

            _SettingsWindow.Resources["DearPatrons"] = catalog.GetString("Dear Patrons!");
            _SettingsWindow.Resources["PatronsMsg"] = catalog.GetString("We express our great appreciation to the people who support our project and motivate us for new achievements.");
            _SettingsWindow.Resources["PatronsThankYou"] = catalog.GetString("Thank you");

            _SettingsWindow.Resources["TranslationEngineSwitchMsg"] = catalog.GetString("Translation engine error. Switching to:");

            _SettingsWindow.Resources["CkSystem"] = catalog.GetString("System");
            _SettingsWindow.Resources["CkEcho"] = catalog.GetString("Echo");
            _SettingsWindow.Resources["CkError"] = catalog.GetString("Error");
            _SettingsWindow.Resources["CkNPCD"] = catalog.GetString("NPCD");
            _SettingsWindow.Resources["CkNPCA"] = catalog.GetString("NPCA");
            _SettingsWindow.Resources["CkRecruitment"] = catalog.GetString("Recruitment");
            _SettingsWindow.Resources["CkSay"] = catalog.GetString("Say");
            _SettingsWindow.Resources["CkShout"] = catalog.GetString("Shout");
            _SettingsWindow.Resources["CkParty"] = catalog.GetString("Party");
            _SettingsWindow.Resources["CkTell"] = catalog.GetString("Tell");
            _SettingsWindow.Resources["CkFreeCompany"] = catalog.GetString("FreeCompany");
            _SettingsWindow.Resources["CkYell"] = catalog.GetString("Yell");
            _SettingsWindow.Resources["CkAlliance"] = catalog.GetString("Alliance");
            _SettingsWindow.Resources["CkLinkShell1"] = catalog.GetString("LinkShell1");
            _SettingsWindow.Resources["CkLinkShell2"] = catalog.GetString("LinkShell2");
            _SettingsWindow.Resources["CkLinkShell3"] = catalog.GetString("LinkShell3");
            _SettingsWindow.Resources["CkLinkShell4"] = catalog.GetString("LinkShell4");
            _SettingsWindow.Resources["CkLinkShell5"] = catalog.GetString("LinkShell5");
            _SettingsWindow.Resources["CkLinkShell6"] = catalog.GetString("LinkShell6");
            _SettingsWindow.Resources["CkLinkShell7"] = catalog.GetString("LinkShell7");
            _SettingsWindow.Resources["CkLinkShell8"] = catalog.GetString("LinkShell8");

            _SettingsWindow.Resources["CkNoviceNetwork"] = catalog.GetString("Novice Network");

            _SettingsWindow.Resources["CkServerInfo"] = catalog.GetString("Server Info");

            _SettingsWindow.Resources["CkCWLS1"] = catalog.GetString("CWLS1");
            _SettingsWindow.Resources["CkCWLS2"] = catalog.GetString("CWLS2");
            _SettingsWindow.Resources["CkCWLS3"] = catalog.GetString("CWLS3");
            _SettingsWindow.Resources["CkCWLS4"] = catalog.GetString("CWLS4");
            _SettingsWindow.Resources["CkCWLS5"] = catalog.GetString("CWLS5");
            _SettingsWindow.Resources["CkCWLS6"] = catalog.GetString("CWLS6");
            _SettingsWindow.Resources["CkCWLS7"] = catalog.GetString("CWLS7");
            _SettingsWindow.Resources["CkCWLS8"] = catalog.GetString("CWLS8");

            _SettingsWindow.Resources["CkEmotes"] = catalog.GetString("Emotes");
            _SettingsWindow.Resources["CkCustomEmotes"] = catalog.GetString("Custom Emotes");

            _SettingsWindow.Resources["DownloadingUpdate"] = catalog.GetString("Downloading new version:");
            _SettingsWindow.Resources["UpdateInstalled"] = catalog.GetString("Click here to update to new version.");

            _SettingsWindow.Resources["NotifyUpdateTitle"] = catalog.GetString("Tataru Update");
            _SettingsWindow.Resources["NotifyUpdateText"] = catalog.GetString("New Tataru helper version is available. Restart Application to update.");

            _SettingsWindow.Resources["CheckUpdatesText"] = catalog.GetString("Check updates");
            _SettingsWindow.Resources["LookingForUpdates"] = catalog.GetString("Looking for updates...");
            _SettingsWindow.Resources["NoUpdatesFound"] = catalog.GetString("No updates found.");
        }
    }
}
