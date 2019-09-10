// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIVTataruHelper.FFHandlers;
using FFXIVTataruHelper.TataruComponentModel;
using FFXIVTataruHelper.Translation;
using FFXIVTataruHelper.ViewModel;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFXIVTataruHelper.UIModel;
using BondTech.HotKeyManagement.WPF._4;
using FFXIVTataruHelper.EventArguments;

namespace FFXIVTataruHelper
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

        public System.Windows.WindowState FFWindowState
        {
            get { return FFMemoryReader.FFWindowState; }
        }

        public ChatProcessor ChatProcessor
        {
            get { return _ChatProcessor; }
        }


        public WebTranslator WebTranslator
        {
            get { return _WebTranslator; }
        }


        public TataruViewModel TataruViewModel
        {
            get { return _TataruViewModel; }
        }

        public HotKeyManager HotKeyManager
        {
            get { return _HotKeyManager; }
        }


        #endregion

        #region **Events.

        #endregion

        #region **LocalVariables.

        string SystemSettingFileName = "AppSysSettings.json";

        List<PropertyBinder> _PropertyBinders;

        List<ChatWindow> _ChatWindows;

        TataruViewModel _TataruViewModel;

        TataruUIModel _TataruUIModel;

        FFMemoryReader _FFMemoryReader;

        WebTranslator _WebTranslator;

        HotKeyManager _HotKeyManager;

        ChatProcessor _ChatProcessor;

        private Object lockObj = new object();

        bool deletingObject = false;

        CancellationTokenSource _SaveSettingsCancellationTokenSource;

        DateTime SettingsChangeTime = DateTime.UtcNow;
        DateTime SettingsSaveTime = DateTime.UtcNow;

        #endregion

        public TataruModel()
        {
            CmdArgsStatus.LoadArgs();

            _PropertyBinders = new List<PropertyBinder>();
            _ChatWindows = new List<ChatWindow>();

            _HotKeyManager = new HotKeyManager(UiWindow.Window);

            _SaveSettingsCancellationTokenSource = new CancellationTokenSource();

            _WebTranslator = new WebTranslator();

            _TataruUIModel = new TataruUIModel();

            _FFMemoryReader = new FFMemoryReader();

            _ChatProcessor = new ChatProcessor(_WebTranslator);

            _TataruViewModel = new TataruViewModel(this);

        }

        public async Task InitializeComponent()
        {
            await Task.Run(() =>
            {

                _WebTranslator.LoadLanguages(GlobalSettings.GoogleTranslateLanguages, GlobalSettings.MultillectTranslateLanguages,
                    GlobalSettings.DeeplLanguages, GlobalSettings.YandexLanguages, GlobalSettings.AmazonLanguages, GlobalSettings.PapagoLanguages, GlobalSettings.BaiduLanguages);

                _FFMemoryReader.Start();

                InitEvents();
            });
        }

        private void InitEvents()
        {
            _FFMemoryReader.FFChatMessageArrived += _ChatProcessor.OnFFChatMessageArrived;

            _TataruUIModel.ChatWindowsListChangedAsync += AsyncOnSettingsWindowsListChanged;
            ////////////////

            _TataruViewModel.ChatWindowsListChangedAsync += AsyncOnViewModelWindowsListChanged;

            _TataruViewModel.ChatWindowsListChangedAsync += OnViewModelChatWindowsListChanged;
        }

        public void Stop()
        {
            foreach (var win in _ChatWindows)
            {
                win.Close();
            }
            _FFMemoryReader.Stop();
            _SaveSettingsCancellationTokenSource.Cancel();
        }

        public async Task AsyncLoadSettings()
        {
            await Task.Run(() =>
            {
                LoadSettings();
            });
        }

        public void LoadSettings()
        {
            try
            {
                if (!Helper.LoadStaticFromJson(typeof(GlobalSettings), SystemSettingFileName))
                {
                    Helper.SaveStaticToJson(typeof(GlobalSettings), SystemSettingFileName);
                    Helper.LoadStaticFromJson(typeof(GlobalSettings), SystemSettingFileName);
                }

                var userSettings = Helper.LoadJsonData<UserSettings>(GlobalSettings.Settings);

                LoadOldSettings(userSettings);

                if (userSettings == null)
                {
                    userSettings = new UserSettings();
                    Logger.WriteLog("userSettings == null");
                }

                for (int i = 0; i < userSettings.ChatWindows.Count; i++)
                {
                    userSettings.ChatWindows[i].WinId = i;
                    userSettings.ChatWindows[i].Name = Convert.ToString(i + 1);
                }

                TataruUIModel.SetSettings(userSettings);

                _TataruUIModel.PropertyChanged += OnUiSettingsChanged;
                SettingsChangeTime = DateTime.UtcNow;
                SettingsSaveTime = DateTime.UtcNow;

                WatchAndSaveChangedSettings();
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }
        }

        private void OnUiSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            SettingsChangeTime = DateTime.UtcNow;
        }

        private void WatchAndSaveChangedSettings()
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    var token = _SaveSettingsCancellationTokenSource.Token;

                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            SpinWait.SpinUntil(() => ((DateTime.UtcNow - SettingsChangeTime).TotalMilliseconds > GlobalSettings.SettingsSaveDelay
                            && (SettingsChangeTime - SettingsSaveTime).TotalMilliseconds > 0) ||
                            token.IsCancellationRequested);

                            if (!token.IsCancellationRequested)
                            {
                                await SaveSettings();
                                SettingsSaveTime = DateTime.UtcNow;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.WriteLog(e);
                            await Task.Delay(GlobalSettings.SettingsSaveDelay);
                        }
                    }


                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                }

            }, TaskCreationOptions.LongRunning);
        }

        private async Task AsyncOnSettingsWindowsListChanged(AsyncListChangedEventHandler<ChatWindowViewModelSettings> ea)
        {
            switch (ea.ChangedEventArgs.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    {
                        await UiWindow.Window.UIThreadAsync(() =>
                        {
                            ChatWindowViewModelSettings newElem = ea.ChangedElemnt;

                            ChatWindowViewModel reslut = null;

                            reslut = _TataruViewModel.ChatWindows.FirstOrDefault(x => x.WinId == newElem.WinId);


                            if (reslut == null)
                            {
                                try
                                {
                                    TataruViewModel.AddNewChatWindow(newElem);
                                }
                                catch (Exception e)
                                {
                                    Logger.WriteLog(e);
                                }

                                var binder = new PropertyBinder(newElem, _TataruViewModel.ChatWindows[_TataruViewModel.ChatWindows.Count - 1]);
                                CreateBinderCouples(binder);
                                _PropertyBinders.Add(binder);
                            }
                        });
                    }
                    break;
                case ListChangedType.ItemDeleted:
                    {
                        await Task.Run(() =>
                        {
                            SpinWait.SpinUntil(() => !deletingObject);
                            lock (lockObj)
                            {
                                deletingObject = true;
                            }
                        });

                        await UiWindow.Window.UIThreadAsync(() =>
                        {
                            try
                            {
                                ChatWindowViewModelSettings deletedElem = ea.ChangedElemnt;

                                var elementToDelete = _TataruViewModel.ChatWindows.FirstOrDefault(x => x.WinId == deletedElem.WinId);

                                if (elementToDelete != null)
                                {

                                    _TataruViewModel.DeleteChatWindow(_TataruViewModel.ChatWindows.IndexOf(elementToDelete));

                                    RemoveChatWindow(_ChatWindows, elementToDelete.WinId);


                                    var binder = _PropertyBinders.FirstOrDefault(x => x.Object1 == elementToDelete && x.Object2 == deletedElem);
                                    if (binder == null)
                                        binder = _PropertyBinders.FirstOrDefault(x => x.Object2 == elementToDelete && x.Object1 == deletedElem);

                                    if (binder != null)
                                    {
                                        binder.Stop();

                                        _PropertyBinders.Remove(binder);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.WriteLog(e);
                            }
                        });

                        deletingObject = false;
                    }
                    break;
            }
        }

        private async Task AsyncOnViewModelWindowsListChanged(AsyncListChangedEventHandler<ChatWindowViewModel> ea)
        {
            switch (ea.ChangedEventArgs.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    {
                        await UiWindow.Window.UIThreadAsync(() =>
                        {
                            ChatWindowViewModel newElem = ea.ChangedElemnt;

                            var reslut = _TataruUIModel.ChatWindows.FirstOrDefault(x => x.WinId == newElem.WinId);
                            if (reslut == null)
                            {
                                var sett = newElem.GetSettings();
                                _TataruUIModel.ChatWindows.Add(sett);

                                var binder = new PropertyBinder(_TataruUIModel.ChatWindows[_TataruUIModel.ChatWindows.Count - 1], newElem);
                                CreateBinderCouples(binder);
                                _PropertyBinders.Add(binder);
                            }
                        });

                    }
                    break;
                case ListChangedType.ItemDeleted:
                    {
                        await Task.Run(() =>
                        {
                            SpinWait.SpinUntil(() => !deletingObject);
                            lock (lockObj)
                            {
                                deletingObject = true;
                            }
                        });

                        await UiWindow.Window.UIThreadAsync(() =>
                        {
                            try
                            {
                                ChatWindowViewModel deletedElem = ea.ChangedElemnt;

                                ChatWindowViewModelSettings reslut = _TataruUIModel.ChatWindows.FirstOrDefault(x => x.WinId == deletedElem.WinId);

                                if (reslut != null)
                                {
                                    _TataruUIModel.ChatWindows.Remove(reslut);

                                    RemoveChatWindow(_ChatWindows, reslut.WinId);

                                    try
                                    {
                                        PropertyBinder binder = _PropertyBinders.FirstOrDefault(x => x.Object1 == reslut && x.Object2 == deletedElem);

                                        if (binder == null)
                                            binder = _PropertyBinders.FirstOrDefault(x => x.Object2 == reslut && x.Object1 == deletedElem);

                                        if (binder != null)
                                        {
                                            binder.Stop();
                                            _PropertyBinders.Remove(binder);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.WriteLog(e);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.WriteLog(e);
                            }
                        });

                        deletingObject = false;
                    }

                    break;
            }
        }

        private async Task OnViewModelChatWindowsListChanged(AsyncListChangedEventHandler<ChatWindowViewModel> ea)
        {
            await Task.Run(() =>
            {
                try
                {
                    switch (ea.ChangedEventArgs.ListChangedType)
                    {
                        case ListChangedType.ItemAdded:
                            {
                                UiWindow.Window.UIThread(() =>
                                {
                                    ChatWindowViewModel newElem = ea.ChangedElemnt;

                                    _ChatWindows.Add(new ChatWindow(this, newElem));
                                    _ChatWindows[_ChatWindows.Count - 1].Show();
                                });
                            }
                            break;
                        case ListChangedType.ItemDeleted:
                            {

                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                }
            });
        }

        private void CreateBinderCouples(PropertyBinder binder)
        {
            binder.AddPropertyCouple(new PropertyCouple<string, string>("Name", "Name"));
            binder.AddPropertyCouple(new PropertyCouple<double, double>("ChatFontSize", "ChatFontSize"));
            binder.AddPropertyCouple(new PropertyCouple<double, double>("LineBreakHeight", "LineBreakHeight"));
            binder.AddPropertyCouple(new PropertyCouple<int, int>("SpacingCount", "SpacingCount"));

            binder.AddPropertyCouple(new PropertyCouple<bool, bool>("IsAlwaysOnTop", "IsAlwaysOnTop"));
            binder.AddPropertyCouple(new PropertyCouple<bool, bool>("IsClickThrough", "IsClickThrough"));
            binder.AddPropertyCouple(new PropertyCouple<bool, bool>("IsAutoHide", "IsAutoHide"));

            binder.AddPropertyCouple(new PropertyCouple<TimeSpan, TimeSpan>("AutoHideTimeout", "AutoHideTimeout"));

            //binder.AddPropertyCouple(new PropertyCouple<bool, bool>("IsHiddenByUser", "IsHiddenByUser"));

            binder.AddPropertyCouple(new PropertyCouple<System.Windows.Media.Color, System.Windows.Media.Color>("BackGroundColor", "BackGroundColor"));

            binder.AddPropertyCouple(new PropertyCouple<System.Drawing.RectangleD, System.Drawing.RectangleD>("ChatWindowRectangle", "ChatWindowRectangle"));

            binder.AddPropertyCouple(new PropertyCouple<TranslationEngineName, System.Windows.Data.CollectionView>("TranslationEngineName", "TranslationEngines",
                (ref TranslationEngineName x, ref System.Windows.Data.CollectionView y) =>
                {
                    TranslationEngine result = null;
                    foreach (TranslationEngine elem in y.SourceCollection)
                        if (elem.EngineName == x)
                        {
                            result = elem;
                            break;
                        }

                    if (result != null)
                        if (!y.CurrentItem.Equals(result))
                            y.MoveCurrentTo(result);

                },
                (ref System.Windows.Data.CollectionView y, ref TranslationEngineName x) =>
                {
                    x = ((TranslationEngine)y.CurrentItem).EngineName;
                }));


            binder.AddPropertyCouple(new PropertyCouple<TranslatorLanguague, System.Windows.Data.CollectionView>("FromLanguague", "TranslateFromLanguagues",
                (ref TranslatorLanguague x, ref System.Windows.Data.CollectionView y) =>
                {
                    TranslatorLanguague result = null;

                    foreach (TranslatorLanguague elem in y.SourceCollection)
                        if (elem.SystemName == x.SystemName)
                        {
                            result = elem;
                            break;
                        }

                    if (result != null)
                        if (!y.CurrentItem.Equals(result))
                            y.MoveCurrentTo(result);


                },
                (ref System.Windows.Data.CollectionView y, ref TranslatorLanguague x) =>
                {
                    var lang = new TranslatorLanguague((TranslatorLanguague)y.CurrentItem);
                    x = lang;
                }));

            binder.AddPropertyCouple(new PropertyCouple<TranslatorLanguague, System.Windows.Data.CollectionView>("ToLanguague", "TranslateToLanguagues",
                (ref TranslatorLanguague x, ref System.Windows.Data.CollectionView y) =>
                {
                    TranslatorLanguague result = null;

                    foreach (TranslatorLanguague elem in y.SourceCollection)
                        if (elem.SystemName == x.SystemName)
                        {
                            result = elem;
                            break;
                        }

                    if (result != null)
                        if (!y.CurrentItem.Equals(result))
                            y.MoveCurrentTo(result);


                },
                (ref System.Windows.Data.CollectionView y, ref TranslatorLanguague x) =>
                {
                    var lang = new TranslatorLanguague((TranslatorLanguague)y.CurrentItem);
                    x = lang;
                }));

            binder.AddPropertyCouple(new PropertyCouple<WinUtils.HotKeyCombination, WinUtils.HotKeyCombination>("ShowHideChatKeys", "ShowHideChatKeys"));
            binder.AddPropertyCouple(new PropertyCouple<WinUtils.HotKeyCombination, WinUtils.HotKeyCombination>("ClickThoughtChatKeys", "ClickThoughtChatKeys"));
            binder.AddPropertyCouple(new PropertyCouple<WinUtils.HotKeyCombination, WinUtils.HotKeyCombination>("ClearChatKeys", "ClearChatKeys"));

            binder.AddPropertyCouple(new PropertyCouple<List<ChatCodeViewModel>, BindingList<ChatCodeViewModel>>("ChatCodes", "ChatCodes",
                (ref List<ChatCodeViewModel> x, ref BindingList<ChatCodeViewModel> y) =>
                {
                    foreach (var code in x)
                    {
                        var fCode = y.FirstOrDefault(p => p.Equals(code));
                        if (fCode != null)
                        {
                            fCode.Color = code.Color;
                            fCode.IsChecked = code.IsChecked;
                        }
                    }

                },
                (ref BindingList<ChatCodeViewModel> y, ref List<ChatCodeViewModel> x) =>
                    {
                        foreach (var code in y)
                        {
                            var fCode = x.FirstOrDefault(p => p.Equals(code));
                            if (fCode != null)
                            {
                                fCode.Color = code.Color;
                                fCode.IsChecked = code.IsChecked;
                            }
                        }
                    }));
        }

        private void RemoveChatWindow(List<ChatWindow> lsit, long winId)
        {
            var win = _ChatWindows.FirstOrDefault(x => x.WinId == winId);

            if (win != null)
            {
                UiWindow.Window.UIThread(() =>
                {
                    _ChatWindows.Remove(win);
                    win.Close();
                });
            }
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

        void LoadOldSettings(UserSettings userSettings)
        {
            if (!File.Exists(GlobalSettings.OldSettings))
                return;

            try
            {

                var oldSettings = Helper.LoadJsonData<UserSettingsOld>(GlobalSettings.OldSettings);

                try
                {
                    File.Delete(GlobalSettings.OldSettings);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex);
                }

                if (userSettings.ChatWindows.Count == 0)
                {
                    ChatWindowViewModelSettings windowSettings = new ChatWindowViewModelSettings("1", 0);

                    windowSettings.ChatFontSize = oldSettings.FontSize;
                    windowSettings.LineBreakHeight = oldSettings.LineBreakHeight;
                    windowSettings.SpacingCount = oldSettings.InsertSpaceCount;

                    windowSettings.IsAlwaysOnTop = oldSettings.IsAlwaysOnTop;
                    windowSettings.IsClickThrough = oldSettings.IsClickThrough;
                    windowSettings.IsAutoHide = oldSettings.IsAutoHide;

                    windowSettings.AutoHideTimeout = oldSettings.AutoHideTimeout;

                    windowSettings.BackGroundColor = oldSettings.BackgroundColor;
                    windowSettings.IsAutoHide = oldSettings.IsAutoHide;

                    windowSettings.TranslationEngineName = (TranslationEngineName)oldSettings.CurrentTranslationEngine;

                    var eng = WebTranslator.TranslationEngines.FirstOrDefault(x => x.EngineName == windowSettings.TranslationEngineName);
                    if (eng != null)
                    {
                        var lang1 = eng.SupportedLanguages.FirstOrDefault(x => x.ShownName == oldSettings.CurrentFFXIVLanguage);
                        var lang2 = eng.SupportedLanguages.FirstOrDefault(x => x.ShownName == oldSettings.CurrentTranslateToLanguage);
                        if (lang1 != null && lang2 != null)
                        {
                            windowSettings.FromLanguague = new TranslatorLanguague(lang1);
                            windowSettings.ToLanguague = new TranslatorLanguague(lang2);
                        }
                    }

                    windowSettings.ChatWindowRectangle = oldSettings.ChatWindowLocation;

                    foreach (var ck in windowSettings.ChatCodes)
                    {
                        ChatMsgType msgType = null;
                        if (oldSettings.ChatCodes.TryGetValue(ck.Code, out msgType))
                        {
                            bool isCheked = (msgType.MsgType == MsgType.Translate) ? true : false;

                            ck.IsChecked = isCheked;
                            ck.Color = msgType.Color;
                        }
                    }

                    windowSettings.ShowHideChatKeys = new WinUtils.HotKeyCombination(oldSettings.ShowHideChatKeys.Name + "0", oldSettings.ShowHideChatKeys);
                    windowSettings.ClickThoughtChatKeys = new WinUtils.HotKeyCombination(oldSettings.ClickThoughtChatKeys.Name + "0", oldSettings.ClickThoughtChatKeys);
                    windowSettings.ClearChatKeys = new WinUtils.HotKeyCombination(oldSettings.ClearChatKeys.Name + "0", oldSettings.ClearChatKeys);

                    userSettings.ChatWindows.Add(new ChatWindowViewModelSettings(windowSettings));
                }
            }
            catch (Exception exx)
            {
                Logger.WriteLog(exx);
            }
        }
    }
}

