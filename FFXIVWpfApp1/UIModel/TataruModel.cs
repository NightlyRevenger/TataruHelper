// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIITataruHelper.EventArguments;
using FFXIITataruHelper.FFHandlers;
using FFXIITataruHelper.Translation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIITataruHelper
{
    public class TataruModel
    {

        #region **Properties.

        public TataruUIModel TataruUIModel
        {
            get { return _TataruUIModel; }
        }

        public FFMemoryReader FFMemoryReader
        {
            get { return _FFMemoryReader; }
        }

        public ChatProcessor ChatProcessor
        {
            get { return _ChatProcessor; }
        }

        #endregion

        #region **Events.

        #endregion

        #region **LocalVariables.

        string SystemSettingFileName = "../AppSysSettings.json";

        string OldSettingFileName = "../AppSettings.json";
        string OldHotKeysFileName = "../HotKeys.json";

        TataruUIModel _TataruUIModel;

        FFMemoryReader _FFMemoryReader;

        WebTranslator _WebTranslator;

        ChatProcessor _ChatProcessor;

        #endregion

        public TataruModel()
        {
            CmdArgsStatus.LoadArgs();

            _WebTranslator = new WebTranslator();

            _TataruUIModel = new TataruUIModel(_WebTranslator.GetAllSupportedLanguages());

            _FFMemoryReader = new FFMemoryReader();
            _FFMemoryReader.Start();

            _ChatProcessor = new ChatProcessor(_FFMemoryReader, _WebTranslator, _TataruUIModel);
        }

        public void Stop()
        {
            _FFMemoryReader.Stop();
        }

        public async Task LoadSettings()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!Helper.LoadStaticFromJson(typeof(GlobalSettings), SystemSettingFileName))
                    {
                        Helper.SaveStaticToJson(typeof(GlobalSettings), SystemSettingFileName);
                        Helper.LoadStaticFromJson(typeof(GlobalSettings), SystemSettingFileName);
                    }

                    var userSettings = Helper.LoadJsonData<UserSettings>(GlobalSettings.Settings);

                    if (userSettings == null)
                    {
                        userSettings = new UserSettings();
                        Logger.WriteLog("userSettings == null");
                    }

                    _WebTranslator.LoadLanguagues(GlobalSettings.GoogleTranslateLanguages, GlobalSettings.MultillectTranslateLanguages,
                        GlobalSettings.DeeplLanguages, GlobalSettings.YandexLanguages);

                    LoadAllOldSettings(userSettings);

                    TataruUIModel.SetSettings(userSettings);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                }
            });
        }

        public async Task SaveSettings()
        {
            await Task.Run(() =>
            {
                try
                {
                    var userSettings = TataruUIModel.GetSettings();
                    Helper.SaveJson(userSettings, GlobalSettings.Settings);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                }
            });
        }

        private void LoadAllOldSettings(UserSettings userSettings)
        {
            try
            {
                if (File.Exists(OldSettingFileName))
                {
                    var oldSett = LoadOldSettingsFile(userSettings);
                    if (oldSett != null)
                    {
                        ApplyOldSettings(userSettings, oldSett);
                    }
                    File.Delete(OldSettingFileName);
                }
            }
            catch (Exception e)
            {
                userSettings = Helper.LoadJsonData<UserSettings>(GlobalSettings.Settings);
                Logger.WriteLog(e);
            }

            try
            {
                if (File.Exists(OldHotKeysFileName))
                {
                    var oldHkStr = String.Empty;

                    using (StreamReader sr = new StreamReader(OldHotKeysFileName))
                    {
                        oldHkStr = sr.ReadToEnd();
                    }

                    var oldHk = JsonConvert.DeserializeObject<List<WinUtils.HotKeyCombination>>(oldHkStr);

                    userSettings.ShowHideChatKeys = new WinUtils.HotKeyCombination(oldHk[0]);
                    userSettings.ClickThoughtChatKeys = new WinUtils.HotKeyCombination(oldHk[1]);

                    File.Delete(OldHotKeysFileName);
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }
        }

        private UIModel.OldSettings LoadOldSettingsFile(UserSettings userSettings)
        {
            UIModel.OldSettings oldSettings = null;

            try
            {
                oldSettings = new UIModel.OldSettings();
                string oldSet = string.Empty;
                using (StreamReader sr = new StreamReader(OldSettingFileName))
                {
                    oldSet = sr.ReadToEnd();
                }
                oldSet = oldSet.Replace("\",", "\":");
                oldSet = oldSet.Replace("[", "{");
                oldSet = oldSet.Replace("]", "}");
                var ind1 = oldSet.IndexOf('{');
                var ind2 = oldSet.LastIndexOf('}');
                var oldSetsb = new StringBuilder(oldSet);
                oldSetsb[ind1] = '[';
                oldSetsb[ind2] = ']';

                oldSet = oldSetsb.ToString();


                oldSet = ReomveBadOccr(oldSet, "FFXIVLanguagles");

                oldSet = ReomveBadOccr(oldSet, "RecentBackgroundColor");
                oldSet = ReomveBadOccr(oldSet, "RecentFont1Colors");
                oldSet = ReomveBadOccr(oldSet, "RecentFont2Colors");

                var res = JsonConvert.DeserializeObject<List<Object>>(oldSet);

                oldSettings.BackgroundColor = (string)((Newtonsoft.Json.Linq.JObject)res[0]).GetValue("BackgroundColor");
                oldSettings.Font1Color = (string)((Newtonsoft.Json.Linq.JObject)res[1]).GetValue("Font1Color");
                oldSettings.Font2Color = (string)((Newtonsoft.Json.Linq.JObject)res[2]).GetValue("Font2Color");

                oldSettings.IsClickThrough = (bool)((Newtonsoft.Json.Linq.JObject)res[3]).GetValue("IsClickThrough");
                oldSettings.IsAlwaysOnTop = (bool)((Newtonsoft.Json.Linq.JObject)res[4]).GetValue("IsAlwaysOnTop");
                oldSettings.IsHideToTray = (bool)((Newtonsoft.Json.Linq.JObject)res[5]).GetValue("IsHideToTray");

                oldSettings.FontSize = (int)((Newtonsoft.Json.Linq.JObject)res[6]).GetValue("FontSize");
                oldSettings.LineBreakHeight = (int)((Newtonsoft.Json.Linq.JObject)res[8]).GetValue("LineBreakHeight");
                oldSettings.InsertSpaceCount = (int)((Newtonsoft.Json.Linq.JObject)res[9]).GetValue("InsertSpaceCount");

                oldSettings.CurrentTranslationEngine = (int)((Newtonsoft.Json.Linq.JObject)res[10]).GetValue("CurrentTranslationEngine");

                oldSettings.CurrentFFXIVLanguage = (int)((Newtonsoft.Json.Linq.JObject)res[11]).GetValue("CurrentFFXIVLanguage");
                oldSettings.CurrentTranslateToLanguage = (int)((Newtonsoft.Json.Linq.JObject)res[12]).GetValue("CurrentTranslateToLanguage");

                oldSettings.CurentLanguague = (int)((Newtonsoft.Json.Linq.JObject)res[17]).GetValue("CurentLanguague");

                oldSettings.MainWinHeight = (double)((Newtonsoft.Json.Linq.JObject)res[26]).GetValue("MainWinHeight");
                oldSettings.MainWinWidth = (double)((Newtonsoft.Json.Linq.JObject)res[27]).GetValue("MainWinWidth");

                oldSettings.ChatWinTop = (double)((Newtonsoft.Json.Linq.JObject)res[28]).GetValue("ChatWinTop");
                oldSettings.ChatWinLeft = (double)((Newtonsoft.Json.Linq.JObject)res[29]).GetValue("ChatWinLeft");
                oldSettings.ChatWinHeight = (double)((Newtonsoft.Json.Linq.JObject)res[30]).GetValue("ChatWinHeight");
                oldSettings.ChatWinWidth = (double)((Newtonsoft.Json.Linq.JObject)res[31]).GetValue("ChatWinWidth");

                oldSettings.IsFirstTime = (int)((Newtonsoft.Json.Linq.JObject)res[32]).GetValue("IsFirstTime");


                //oldSettings = JsonConvert.DeserializeObject<UIModel.OldSettings>(oldSet);
            }
            catch (Exception e)
            {
                oldSettings = null;
                Logger.WriteLog(e);
            }

            return oldSettings;
        }

        private string ReomveBadOccr(string text, string marker)
        {
            int ind1 = 0, ind2 = 0;

            ind1 = text.IndexOf(marker);
            ind1 = text.LastIndexOf('{', ind1);

            ind2 = text.IndexOf('}', ind1) + 1;
            ind2 = text.IndexOf('}', ind2);
            ind2 = text.IndexOf(',', ind2) + 1;
            text = text.Substring(0, ind1) + text.Substring(ind2, text.Length - ind2);

            return text;
        }

        private void ApplyOldSettings(UserSettings userSettings, UIModel.OldSettings oldSettings)
        {
            var trLan = _WebTranslator.GetAllSupportedLanguages();

            userSettings.BackgroundColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(oldSettings.BackgroundColor);
            userSettings.Font1Color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(oldSettings.Font1Color);
            userSettings.Font2Color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(oldSettings.Font2Color);

            userSettings.IsClickThrough = (bool)oldSettings.IsClickThrough;
            userSettings.IsAlwaysOnTop = (bool)oldSettings.IsAlwaysOnTop;
            userSettings.IsHideToTray = (bool)oldSettings.IsHideToTray;

            userSettings.FontSize = (int)oldSettings.FontSize;
            userSettings.LineBreakHeight = (int)oldSettings.LineBreakHeight;
            userSettings.InsertSpaceCount = (int)oldSettings.InsertSpaceCount;

            userSettings.CurrentTranslationEngine = (int)oldSettings.CurrentTranslationEngine;

            //userSettings.CurrentFFXIVLanguage = oldSettings.CurrentFFXIVLanguage;

            userSettings.CurrentTranslateToLanguage = trLan[userSettings.CurrentTranslationEngine][(int)oldSettings.CurrentTranslateToLanguage].ShownName;

            userSettings.CurentUILanguague = (int)oldSettings.CurentLanguague;

            userSettings.SettingsWindowSize = new System.Drawing.PointD((double)oldSettings.MainWinWidth, (double)oldSettings.MainWinHeight);

            userSettings.ChatWindowLocation = new System.Drawing.RectangleD((double)oldSettings.ChatWinLeft, (double)oldSettings.ChatWinTop,
                (double)oldSettings.ChatWinWidth, (double)oldSettings.ChatWinHeight);

            userSettings.IsFirstTime = (int)oldSettings.IsFirstTime;

        }
    }
}
