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
    class LanguagueWrapper
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
                if (value != _CurrentLanguage)
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

            if (languague == Languages.English)
                path += GlobalSettings.en_US_LanguaguePath;

            if (languague == Languages.Russian)
                path += GlobalSettings.ru_RU_LanguaguePath;

            LoadDynamicTranslation(path);
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

            _Window.Resources["ParagraphSettings"] = catalog.GetString("Paragraph Settings");
            _Window.Resources["IntervalWidth"] = catalog.GetString("Interval Width");
            _Window.Resources["LineBreakHeight"] = catalog.GetString("Line Break Height");


            _Window.Resources["TranslationEngine"] = catalog.GetString("Translation Engine");
            _Window.Resources["FFLanguage"] = catalog.GetString("FF Language");
            _Window.Resources["TraslateTo"] = catalog.GetString("Traslate To");

            _Window.Resources["Hotkeys"] = catalog.GetString("Hotkeys");
            _Window.Resources["ShowHideChatWindowHK"] = catalog.GetString("Show/hide Chat Window");
            _Window.Resources["ClickThroughHK"] = catalog.GetString("Click Through");

            _Window.Resources["OtherSett"] = catalog.GetString("Other");
            _Window.Resources["ClickThroughCB"] = catalog.GetString("Click Through");
            _Window.Resources["AlwaysOnTopCB"] = catalog.GetString("Always On Top");
            _Window.Resources["HideToTrayCB"] = catalog.GetString("Hide to Tray");

            _Window.Resources["AlwaysOnTopCB"] = catalog.GetString("Always On Top");
            _Window.Resources["HideToTrayCB"] = catalog.GetString("Hide to Tray");

            _Window.Resources["ShowChatBox"] = catalog.GetString("Show Chat Window");
            _Window.Resources["HideChatBox"] = catalog.GetString("Hide Chat Window");

            _Window.Resources["ResetChatPosition"] = catalog.GetString("Reset Chat Position");

            if (!_Window.Resources.Contains("TranslationEngineError"))
            {
                _Window.Resources.Add("TranslationEngineError", catalog.GetString("Translation engine error. Consider switching to other engine."));
            }
            _Window.Resources["TranslationEngineError"] = catalog.GetString("Translation engine error. Consider switching to other engine.");

            _Window.Resources["FFStatusText"] = catalog.GetString("Couldn't find FFXIV process.");

            if (!_Window.Resources.Contains("FFStatusTextFound"))
            {
                _Window.Resources.Add("FFStatusTextFound", catalog.GetString("Process found:"));
            }
            _Window.Resources["FFStatusTextFound"] = catalog.GetString("Process found:");
        }
    }
}
