// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using NGettext;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FFXIITataruHelper
{
    public class LanguagueWrapper
    {
        Window _Window;

        public enum Languages : int
        {
            None = 0,
            Russian = 1,
            English = 2
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
            _Window = window;
            _CurrentLanguage = Languages.None;
        }

        private void SetLanguague(Languages languague)
        {
            string path = _DirPath;

            if (languague != Languages.None)
            {
                if (languague == Languages.English)
                    path += GlobalSettings.en_US_LanguaguePath;

                if (languague == Languages.Russian)
                    path += GlobalSettings.ru_RU_LanguaguePath;

                LoadDynamicTranslation(path);
            }
        }


        private void LoadDynamicTranslation(string path)
        {
            ICatalog catalog = new Catalog();
            try
            {
                var fs = File.Open(path, System.IO.FileMode.Open);
                catalog = new Catalog(fs);
                fs.Close();
                fs.Dispose();
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }

            _Window.Resources["SettingsWindowName"] = catalog.GetString("Settings");

            _Window.Resources["DocLanguage"] = catalog.GetString("Language");
            _Window.Resources["DocLanguageEn"] = catalog.GetString("English");
            _Window.Resources["DocLanguageRu"] = catalog.GetString("Russian");

            _Window.Resources["DockHelp"] = catalog.GetString("Help");

            _Window.Resources["ChatAppearance"] = catalog.GetString("Chat Appearance");
            _Window.Resources["TranslationSettings"] = catalog.GetString("Translation Settings");
            _Window.Resources["GeneralBehavior"] = catalog.GetString("General Behavior");

            _Window.Resources["FontSettings"] = catalog.GetString("Font Settings");
            _Window.Resources["FontSize"] = catalog.GetString("Font Size");

            _Window.Resources["FontColor"] = catalog.GetString("Font Color");
            _Window.Resources["FontColor1"] = catalog.GetString("Color 1");
            _Window.Resources["FontColor2"] = catalog.GetString("Color 2");

            _Window.Resources["BackgroundColor"] = catalog.GetString("Background Color");

            _Window.Resources["ColorPickerStandardButtonHeader"] = catalog.GetString("Standard");
            _Window.Resources["ColorPickerAdvancedButtonHeader"] = catalog.GetString("Advanced");
            _Window.Resources["ColorPickerStandardColorsHeader"] = catalog.GetString("Standard Colors");
            _Window.Resources["ColorPickerRecentColorsHeader"] = catalog.GetString("Recent Colors");

            _Window.Resources["ParagraphSettings"] = catalog.GetString("Paragraph Settings");
            _Window.Resources["IntervalWidth"] = catalog.GetString("Interval Width");
            _Window.Resources["LineBreakHeight"] = catalog.GetString("Line Break Height");

            _Window.Resources["ChatCodes"] = catalog.GetString("Chat Codes");

            _Window.Resources["TranslationEngine"] = catalog.GetString("Translation Engine");
            _Window.Resources["FFLanguage"] = catalog.GetString("FF Language");
            _Window.Resources["TraslateTo"] = catalog.GetString("Traslate To");

            _Window.Resources["Hotkeys"] = catalog.GetString("Hotkeys");

            _Window.Resources["ShowHideChatWindowHK"] = catalog.GetString("Show/hide Chat Window");
            _Window.Resources["ShowHideChatWindowHKToolTip"] = catalog.GetString("Show Hide Chat Window Hotkey Tooltip");

            _Window.Resources["ClickThroughHK"] = catalog.GetString("Click Through");
            _Window.Resources["ClickThroughHKToolTip"] = catalog.GetString("Click Through HotKey ToolTip");

            _Window.Resources["ClearChatHK"] = catalog.GetString("Clear Chat");
            _Window.Resources["ClearChatHKToolTip"] = catalog.GetString("Clear Chat HotKey ToolTip");

            _Window.Resources["OtherSett"] = catalog.GetString("Other");
            _Window.Resources["ClickThroughCB"] = catalog.GetString("Click Through");
            _Window.Resources["AlwaysOnTopCB"] = catalog.GetString("Always On Top");
            _Window.Resources["HideToTrayCB"] = catalog.GetString("Hide to Tray");

            _Window.Resources["AlwaysOnTopCB"] = catalog.GetString("Always On Top");
            _Window.Resources["HideToTrayCB"] = catalog.GetString("Hide to Tray");

            _Window.Resources["ShowChatBox"] = catalog.GetString("Show Chat Window");
            _Window.Resources["HideChatBox"] = catalog.GetString("Hide Chat Window");

            _Window.Resources["ResetChatPosition"] = catalog.GetString("Reset Chat Position");

            /*
            if (!_Window.Resources.Contains("TranslationEngineError"))
            {
                _Window.Resources.Add("TranslationEngineError", catalog.GetString("Translation engine error. Consider switching to other engine."));
            }//*/

            _Window.Resources["TranslationEngineError"] = catalog.GetString("Translation engine error. Consider switching to other engine.");

            _Window.Resources["FFStatusLable"] = catalog.GetString("FF Status:");

            _Window.Resources["FFStatusText"] = catalog.GetString("Couldn't find FFXIV process.");

            /*
            if (!_Window.Resources.Contains("FFStatusTextFound"))
            {
                _Window.Resources.Add("FFStatusTextFound", catalog.GetString("Process found:"));
            }//*/
            _Window.Resources["FFStatusTextFound"] = catalog.GetString("Process found:");

            /*
            if (!_Window.Resources.Contains("TranslationEngineSwitchMsg"))
            {
                _Window.Resources.Add("TranslationEngineSwitchMsg", catalog.GetString("Translation engine error. Switching to:"));
            }//*/

            _Window.Resources["TranslationEngineSwitchMsg"] = catalog.GetString("Translation engine error. Switching to:");

            _Window.Resources["CkSystem"] = catalog.GetString("System");
            _Window.Resources["CkEcho"] = catalog.GetString("Echo");
            _Window.Resources["CkError"] = catalog.GetString("Error");
            _Window.Resources["CkNPCD"] = catalog.GetString("NPCD");
            _Window.Resources["CkNPCA"] = catalog.GetString("NPCA");
            _Window.Resources["CkRecruitment"] = catalog.GetString("Recruitment");
            _Window.Resources["CkSay"] = catalog.GetString("Say");
            _Window.Resources["CkShout"] = catalog.GetString("Shout");
            _Window.Resources["CkParty"] = catalog.GetString("Party");
            _Window.Resources["CkTell"] = catalog.GetString("Tell");
            _Window.Resources["CkFreeCompany"] = catalog.GetString("FreeCompany");
            _Window.Resources["CkYell"] = catalog.GetString("Yell");
            _Window.Resources["CkAlliance"] = catalog.GetString("Alliance");
            _Window.Resources["CkLinkShell1"] = catalog.GetString("LinkShell1");
            _Window.Resources["CkLinkShell2"] = catalog.GetString("LinkShell2");
            _Window.Resources["CkLinkShell3"] = catalog.GetString("LinkShell3");
            _Window.Resources["CkLinkShell4"] = catalog.GetString("LinkShell4");
            _Window.Resources["CkLinkShell5"] = catalog.GetString("LinkShell5");
            _Window.Resources["CkLinkShell6"] = catalog.GetString("LinkShell6");
            _Window.Resources["CkLinkShell7"] = catalog.GetString("LinkShell7");
            _Window.Resources["CkLinkShell8"] = catalog.GetString("LinkShell8");

        }
    }
}
