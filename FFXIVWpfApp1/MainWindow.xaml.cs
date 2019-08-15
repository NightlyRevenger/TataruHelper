// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;
using System.Windows.Interop;
using BondTech.HotKeyManagement.WPF._4;
using FFXIITataruHelper.WinUtils;
using FFXIITataruHelper.Translation;
using FFXIITataruHelper.EventArguments;
using System.Collections.ObjectModel;
using FFXIITataruHelper.ViewModel;

namespace FFXIITataruHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml//
    /// </summary>
    public partial class MainWindow : Window //-V3072
    {
        HotKeyManager _HotKeyManager;

        ChatWindow _ChatWindow;
        LogWriter _LogWriter;

        ChatStreamWindow _ChatStreamWindow;

        string _GitPath = @"https://github.com/NightlyRevenger/TataruHelper";

        string _EmptyHKString = "Empty";

        GlobalHotKey _ShowHideChat;
        GlobalHotKey _ClickThoughtChat;
        GlobalHotKey _ClearChat;

        LanguagueWrapper _LanguagueWrapper;

        Updater _Updater;

        TataruModel _TataruModel;

        TataruUIModel _TataruUIModel;

        bool _IsShutDown;

        OptimizeFootprint _OptimizeFootprint;

        WinMessagesHandler _WinMessagesHandler;

        public MainWindow()
        {
            _IsShutDown = false;

            if (Utils.TataruSingleInstance.IsOnlyInstance == false)
            {
                ShutDown();
                return;
            }//*/

            InitializeComponent();
            try
            {
                _LogWriter = new LogWriter();
                _LogWriter.StartWriting();

                _TataruModel = new TataruModel();
                _TataruUIModel = _TataruModel.TataruUIModel;

                InitTataruModel();
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }

            _ChatWindow = new ChatWindow(this, _TataruModel);
            _ChatWindow.Show();

            try
            {

                _ChatWindow.IsVisibleChanged += ChatVisibleChanged;

                _LanguagueWrapper = new LanguagueWrapper(this);

                _Updater = new Updater();

            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }
        }

        #region **UserActions.

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            _ChatWindow.Top = this.Top;
            _ChatWindow.Left = this.Left;
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;

            if (_ChatWindow.Visibility == Visibility.Visible)
            {
                b.Content = (string)this.Resources["ShowChatBox"];
                _ChatWindow.UserHide();
            }
            else
            {
                b.Content = (string)this.Resources["HideChatBox"];
                _ChatWindow.UserShow();
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            /*
            string caption = "TataruHelper v" + Convert.ToString(System.Reflection.Assembly.GetEntryAssembly().GetName().Version);
            string text = "TataruHelper" + Environment.NewLine + "GitHub: " + _GitPath;

            MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(text, caption, MessageBoxButton.OK);//*/
            var _AboutWin = new AboutWin();
            _AboutWin.Show();
        }

        private void Patrons_Click(object sender, RoutedEventArgs e)
        {
            /*
            string caption = "TataruHelper v" + Convert.ToString(System.Reflection.Assembly.GetEntryAssembly().GetName().Version);
            string text = "TataruHelper" + Environment.NewLine + "GitHub: " + _GitPath;

            MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(text, caption, MessageBoxButton.OK);//*/
            var patreonWin = new PatreonWin();
            patreonWin.Show();

            patreonWin.Resources["DearPatrons"] = this.Resources["DearPatrons"];
            patreonWin.Resources["PatronsMsg"] = this.Resources["PatronsMsg"];
            patreonWin.Resources["PatronsThankYou"] = this.Resources["PatronsThankYou"];
        }

        private void RuLanguage_Click(object sender, RoutedEventArgs e)
        {
            _TataruUIModel.UiLanguage = (int)LanguagueWrapper.Languages.Russian;
        }

        private void EnLanguage_Click(object sender, RoutedEventArgs e)
        {
            _TataruUIModel.UiLanguage = (int)LanguagueWrapper.Languages.English;
        }

        private void EsLanguage_Click(object sender, RoutedEventArgs e)
        {
            _TataruUIModel.UiLanguage = (int)LanguagueWrapper.Languages.Spanish;
        }

        private void PlLanguage_Click(object sender, RoutedEventArgs e)
        {
            _TataruUIModel.UiLanguage = (int)LanguagueWrapper.Languages.Polish;
        }

        private void ChatFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _TataruUIModel.ChatFontSize = (int)e.NewValue;
        }

        private void IntervalWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _TataruUIModel.ParagraphSpaceCount = (int)e.NewValue;
        }

        private void LineBreakeHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _TataruUIModel.LineBreakeHeight = (int)e.NewValue;
        }

        private void BackgroundColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            _TataruUIModel.BackgroundColor = (Color)e.NewValue.Value;
        }

        private void BackgroundColor_Closed(object sender, RoutedEventArgs e)
        {
            try
            {
                var colors = ((ColorPicker)sender).RecentColors;

                var tatruColors = _TataruUIModel.RecentBackgroundColors;
                foreach (var color in colors)
                {
                    var cl = (Color)color.Color;

                    if (!tatruColors.Contains(cl))
                    {
                        tatruColors.Add(cl);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private void TransaltorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            _TataruUIModel.TranslationEngine = (WebTranslator.TranslationEngine)cb.SelectedIndex;
        }

        private void FFXIVLanguague_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            _TataruUIModel.FFLanguage = Convert.ToString(cb.SelectedValue);
        }

        private void TranslateTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            _TataruUIModel.TranslateToLanguage = Convert.ToString(cb.SelectedValue);
        }

        private void ClickThroughBox_Changed(object sender, RoutedEventArgs e)
        {
            var isClickThrough = (bool)((CheckBox)sender).IsChecked;
            _TataruUIModel.IsChatClickThrough = isClickThrough;
        }

        private void AlwayOnTopBox_Changed(object sender, RoutedEventArgs e)
        {
            var isAlwaysOnTop = (bool)((CheckBox)sender).IsChecked;
            _TataruUIModel.IsChatAlwaysOnTop = isAlwaysOnTop;
        }

        private void HideToTray_Changed(object sender, RoutedEventArgs e)
        {
            var isHideToTray = (bool)((CheckBox)sender).IsChecked;
            _TataruUIModel.IsHideSettingsToTray = isHideToTray;
        }

        private void AutoHide_Changed(object sender, RoutedEventArgs e)
        {
            var isAutoHide = (bool)((CheckBox)sender).IsChecked;
            _TataruUIModel.IsAutoHide = isAutoHide;
        }

        private void DirectMemoryReading_Changed(object sender, RoutedEventArgs e)
        {
            var isDirectMemoryReading = (bool)((CheckBox)sender).IsChecked;
            _TataruUIModel.IsDirecMemoryReading = isDirectMemoryReading;

        }

        private void StreamerWindow_Changed(object sender, RoutedEventArgs e)
        {
            var checled = (bool)((CheckBox)sender).IsChecked;

            if (checled)
            {
                if (_ChatStreamWindow == null)
                {
                    _ChatStreamWindow = new ChatStreamWindow(this, _TataruModel);
                    _ChatStreamWindow.Show();
                }
            }
            else
            {
                if (_ChatStreamWindow != null)
                {
                    _ChatStreamWindow.Close();
                    _ChatStreamWindow = null;
                }
            }
            //*/
        }

        private void AutoHideTimeOut_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _TataruUIModel.AutoHideTimeout = new TimeSpan(0, 0, (int)e.NewValue);
        }

        #endregion

        #region **WindowEvents

        void LoadComboBoxes(ReadOnlyCollection<TranslatorLanguague> supportedLanguages)
        {
            try
            {
                bool LanguagueSelected = false;
                string selectedLanguageTo = string.Empty;
                string selectedLanguageFrom = string.Empty;

                if (TranslateToCombo.SelectedIndex != -1 && TransalteFromCombo.SelectedIndex != -1)
                {
                    LanguagueSelected = true;
                    selectedLanguageTo = TranslateToCombo.Text;
                    selectedLanguageFrom = TransalteFromCombo.Text;
                }

                TranslateToCombo.Items.Clear();
                TransalteFromCombo.Items.Clear();

                var lang = supportedLanguages;

                for (int i = 0; i < lang.Count; i++)
                {
                    TranslateToCombo.Items.Add(lang[i].ShownName);
                }

                for (int i = 0; i < GlobalSettings.FFXIVLanguages.Count; i++)
                {
                    var lng = lang.Where(x => x.ShownName.ToLower().Contains(GlobalSettings.FFXIVLanguages[i].ToLower())).ToList();

                    if (lng.Count > 0)
                    {
                        TransalteFromCombo.Items.Add(GlobalSettings.FFXIVLanguages[i]);
                    }
                }

                if (LanguagueSelected)
                {
                    for (int i = 0; i < TranslateToCombo.Items.Count; i++)
                    {
                        if (((string)TranslateToCombo.Items[i]).ToLower() == selectedLanguageTo.ToLower())
                        {
                            TranslateToCombo.SelectedIndex = i;
                            break;
                        }
                    }


                    for (int i = 0; i < TransalteFromCombo.Items.Count; i++)
                    {
                        if (((string)TransalteFromCombo.Items[i]).ToLower() == selectedLanguageFrom.ToLower())
                        {
                            TransalteFromCombo.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Init();

                _TataruModel.LoadSettings();

                _TataruModel.FFMemoryReader.AddExclusionWindowHandler((new WindowInteropHelper(this).Handle));

                _HotKeyManager = new HotKeyManager(this);

                _HotKeyManager.GlobalHotKeyPressed += GlobalHotKeyHandler;

                ShowHideChatWinHotKeyTb.KeyDown += ShowHideKeyDownBindHandler;
                ShowHideChatWinHotKeyTb.KeyUp += ShowHideKeyUpBindHandler;

                ClickThroughHotKeyTb.KeyDown += ClickThoughKeyDownBindHandler;
                ClickThroughHotKeyTb.KeyUp += ClickThoughKeyUpBindHandler;

                ClearChatHotKeyTb.KeyDown += ClearChatKeyDownBindHandler;
                ClearChatHotKeyTb.KeyUp += ClearChathKeyUpBindHandler;

                try
                {
                    Logger.WriteLog("TataruHelper v" + Convert.ToString(System.Reflection.Assembly.GetEntryAssembly().GetName().Version));
                }
                catch (Exception) { }

                _Updater.StartUpdate();

                this.DataContext = new TataruViewModel(_TataruModel.TataruUIModel);

                _OptimizeFootprint = new OptimizeFootprint();
                _OptimizeFootprint.Start();

                _WinMessagesHandler = new WinMessagesHandler(this);
                _WinMessagesHandler.ShowFirstInstance += OnShowFirstInstance;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(e);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            System.Drawing.PointD winSize = new System.Drawing.PointD(this.Width, this.Height);

            if (_TataruUIModel.SettingsWindowSize != winSize)
                _TataruUIModel.SettingsWindowSize = winSize;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            try
            {
                if (_IsShutDown == false)
                {
                    e.Cancel = true;
                    this.Hide();
                }
                else
                    e.Cancel = false;

                if (e.Cancel == false)
                {
                    if (_OptimizeFootprint != null)
                        _OptimizeFootprint.Stop();

                    Utils.TataruSingleInstance.Stop();

                    if (_ChatWindow != null)
                        _ChatWindow.Close();

                    if (_TataruModel != null)
                        _TataruModel.Stop();

                    Task.Run(async () =>
                    {
                        if (_TataruModel != null)
                            await _TataruModel.SaveSettings();
                    }).Wait();

                    if (_LogWriter != null)
                        _LogWriter.Stop();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(Convert.ToString(ex));
            }
        }

        #endregion

        #region **UiEvents.

        private void ChatVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_ChatWindow.Visibility == Visibility.Visible)
            {
                Button2.Content = (string)this.Resources["HideChatBox"];
            }
            else
            {
                Button2.Content = (string)this.Resources["ShowChatBox"];
            }
        }

        private async Task OnUiLanguageChange(IntegerValueChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
             {
                 if (ea.NewValue != ea.OldValue)
                 {
                     _LanguagueWrapper.CurrentLanguage = (LanguagueWrapper.Languages)ea.NewValue;
                 }
             });
        }

        private async Task OnChatFontSizeChange(IntegerValueChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewValue != ChatFontSize.Value)
                {
                    ChatFontSize.Value = ea.NewValue;
                }
            });
        }

        private async Task OnIntervalWidthChange(IntegerValueChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewValue != IntervalWidth.Value)
                {
                    IntervalWidth.Value = ea.NewValue;
                }
            });
        }

        private async Task OnLineBreakHeightChange(IntegerValueChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewValue != LineBreakeHeight.Value)
                {
                    LineBreakeHeight.Value = ea.NewValue;
                }
            });
        }

        private async Task OnBackgroundColorChange(ColorChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewColor != BackgroundColor.SelectedColor)
                {
                    BackgroundColor.SelectedColor = ea.NewColor;
                }
            });
        }

        private async Task OnTranslationEngineChange(TranslationEngineChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewEngine != TransaltorComboBox.SelectedIndex)
                {
                    TransaltorComboBox.SelectedIndex = ea.NewEngine;
                }
                LoadComboBoxes(ea.SupportedLanguages);
            });
        }

        private async Task OnFFLanguageChange(StringValueChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewString != Convert.ToString(TransalteFromCombo.SelectedValue))
                {
                    var items = TransalteFromCombo.Items;

                    for (int i = 0; i < items.Count; i++)
                    {
                        if (Convert.ToString(items[i]) == ea.NewString)
                        {
                            TransalteFromCombo.SelectedIndex = i;
                            break;
                        }
                    }
                }
            });
        }

        private async Task OnTranslateToLanguageChange(StringValueChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewString != Convert.ToString(TranslateToCombo.SelectedValue))
                {
                    var items = TranslateToCombo.Items;

                    for (int i = 0; i < items.Count; i++)
                    {
                        if (Convert.ToString(items[i]) == ea.NewString)
                        {
                            TranslateToCombo.SelectedIndex = i;
                            break;
                        }
                    }
                }
            });
        }

        private async Task OnChatClickThroughChange(BooleanChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewValue != ClickThroughBox.IsChecked)
                {
                    ClickThroughBox.IsChecked = ea.NewValue;
                }
            });
        }

        private async Task OnChatAlwayOnTopChange(BooleanChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewValue != AlwayOnTopBox.IsChecked)
                {
                    AlwayOnTopBox.IsChecked = ea.NewValue;
                }
            });
        }

        private async Task OnHideSettingsToTrayChange(BooleanChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewValue != HideToTray.IsChecked)
                {
                    HideToTray.IsChecked = ea.NewValue;
                }
            });
        }

        private async Task OnAutoHideChange(BooleanChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewValue != AutoHideBox.IsChecked)
                {
                    AutoHideBox.IsChecked = ea.NewValue;
                }
            });
        }

        private async Task OnDirecMemoryReadingChange(BooleanChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewValue != DirectMemoryBox.IsChecked)
                {
                    DirectMemoryBox.IsChecked = ea.NewValue;
                }

                _TataruModel.FFMemoryReader.UseDirectReading = ea.NewValue;
            });
        }

        private async Task OnAutoHideTimeOutChange(TimeSpanChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                int totalSeconds = (int)Math.Round(ea.NewValue.TotalSeconds);

                if (totalSeconds != AutoHideTimeOut.Value)
                {
                    AutoHideTimeOut.Value = totalSeconds;
                }
            });
        }

        private async Task OnSettingsWindowSizeChange(PointDValueChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                System.Drawing.PointD winSize = new System.Drawing.PointD(this.Width, this.Height);

                var UIModel = ((TataruUIModel)ea.Sender);

                if (UIModel.IsFirstTime == 0)
                {
                    UIModel.IsFirstTime = -1;

                    double left = _ChatWindow.Left + _ChatWindow.ActualWidth;
                    this.Left = left;
                    this.Top = _ChatWindow.Top;
                }

                if (ea.NewValue != winSize)
                {
                    if (ea.NewValue.X > 1 && ea.NewValue.Y > 1)
                    {
                        this.Width = ea.NewValue.X;
                        this.Height = ea.NewValue.Y;
                    }
                    else
                    {
                        UIModel.SettingsWindowSize = winSize;
                    }
                }
            });
        }

        private async Task OnFFWindowStateChange(WindowStateChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.IsRunningNew != ea.IsRunningOld)
                {
                    if (ea.IsRunningNew)
                    {
                        FFStatusText.Content = ((string)this.Resources["FFStatusTextFound"]) + " " + ea.Text;
                    }
                    else
                    {
                        FFStatusText.Content = ((string)this.Resources["FFStatusText"]);
                    }
                }
            });
        }

        private async Task OnColorListChange(ColorListChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.ColorsId == 0)
                {
                    var rc = BackgroundColor.RecentColors;
                    rc.Clear();

                    foreach (var color in ea.Colors)
                    {
                        rc.Add(new ColorItem(color, color.ToString()));
                    }
                }
            });
        }

        private async Task OnShowFirstInstance(BooleanChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                ShowSettingsWindow();

                _ChatWindow.UserShow();
            });
        }

        #endregion

        #region **Initialization.

        void InitTataruModel()
        {
            var UIModel = _TataruModel.TataruUIModel;

            UIModel.UiLanguageChanged += OnUiLanguageChange;

            UIModel.ChatFontSizeChanged += OnChatFontSizeChange;
            UIModel.ParagraphSpaceCountChanged += OnIntervalWidthChange;
            UIModel.LineBreakeHeightChanged += OnLineBreakHeightChange;

            UIModel.BackgroundColorChanged += OnBackgroundColorChange;

            UIModel.TranslationEngineChanged += OnTranslationEngineChange;
            UIModel.FFLanguageChanged += OnFFLanguageChange;
            UIModel.TranslateToLanguageChanged += OnTranslateToLanguageChange;

            UIModel.IsChatClickThroughChanged += OnChatClickThroughChange;
            UIModel.IsChatAlwaysOnTopChanged += OnChatAlwayOnTopChange;
            UIModel.IsHideSettingsToTrayChanged += OnHideSettingsToTrayChange;
            UIModel.IsAutoHideChanged += OnAutoHideChange;
            UIModel.IsDirecMemoryReadingChanged += OnDirecMemoryReadingChange;
            UIModel.AutoHideTimeoutChanged += OnAutoHideTimeOutChange;

            UIModel.SettingsWindowSizeChanged += OnSettingsWindowSizeChange;

            UIModel.ShowHideChatCombinationChanged += OnShowHideChatCombinationChange;
            UIModel.ClickThoughtChatCombinationChanged += OnClickThoughtChatCombinationChange;
            UIModel.ClearChatCombinationChanged += OnClearChatCombinationChange;

            UIModel.ColorListChanged += OnColorListChange;

            _TataruModel.FFMemoryReader.FFWindowStateChanged += OnFFWindowStateChange;
        }

        private void Init()
        {
            try
            {
                BackgroundColor.ShowAvailableColors = false;
                BackgroundColor.ShowStandardColors = true;
                BackgroundColor.ShowRecentColors = true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }
        }

        #endregion

        #region **HotKeys.

        private async Task OnShowHideChatCombinationChange(HotKeyCombinationChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                try
                {
                    if (ea.NewHotKeyCombination.IsInitialized)
                        ShowHideChatWinHotKeyTb.Text = ea.NewHotKeyCombination.CombinationKeysName();

                    RegisterHotkey(ea.NewHotKeyCombination, ref _ShowHideChat);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                }
            });
        }

        private async Task OnClickThoughtChatCombinationChange(HotKeyCombinationChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                try
                {
                    if (ea.NewHotKeyCombination.IsInitialized)
                        ClickThroughHotKeyTb.Text = ea.NewHotKeyCombination.CombinationKeysName();

                    RegisterHotkey(ea.NewHotKeyCombination, ref _ClickThoughtChat);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                }
            });
        }

        private async Task OnClearChatCombinationChange(HotKeyCombinationChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                try
                {
                    if (ea.NewHotKeyCombination.IsInitialized)
                        ClearChatHotKeyTb.Text = ea.NewHotKeyCombination.CombinationKeysName();

                    RegisterHotkey(ea.NewHotKeyCombination, ref _ClearChat);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                }
            });
        }

        private void RegisterHotkey(HotKeyCombination combination, ref GlobalHotKey globalHotKey)
        {
            try
            {
                if (combination.IsInitialized)
                {
                    if (globalHotKey != null)
                    {
                        _HotKeyManager.RemoveGlobalHotKey(globalHotKey);
                    }

                    var k = Keys.ConvertFromWpfKey(combination.NormalKey);
                    globalHotKey = new GlobalHotKey(combination.Name, combination.ModifierKey, k);

                    _HotKeyManager.AddGlobalHotKey(globalHotKey);

                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }
        }

        private void RemoveHotKey(HotKeyCombination combination, ref GlobalHotKey globalHotKey)
        {
            try
            {
                combination.ClearKeys();

                if (globalHotKey != null)
                {
                    _HotKeyManager.RemoveGlobalHotKey(globalHotKey);
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }
        }

        private void ShowHideKeyDownBindHandler(object sender, KeyEventArgs e)
        {
            RegisterHotKeyDown(ref _ShowHideChat, _TataruUIModel.ShowHideChatKeys, ShowHideChatWinHotKeyTb, e);
        }

        private void ShowHideKeyUpBindHandler(object sender, KeyEventArgs e)
        {
            RegisterHotKeyUp(ref _ShowHideChat, _TataruUIModel.ShowHideChatKeys, ShowHideChatWinHotKeyTb, e);
        }

        private void ClickThoughKeyDownBindHandler(object sender, KeyEventArgs e)
        {
            RegisterHotKeyDown(ref _ClickThoughtChat, _TataruUIModel.ClickThoughtChatKeys, ClickThroughHotKeyTb, e);
        }

        private void ClickThoughKeyUpBindHandler(object sender, KeyEventArgs e)
        {
            RegisterHotKeyUp(ref _ClickThoughtChat, _TataruUIModel.ClickThoughtChatKeys, ClickThroughHotKeyTb, e);
        }

        private void ClearChatKeyDownBindHandler(object sender, KeyEventArgs e)
        {
            RegisterHotKeyDown(ref _ClearChat, _TataruUIModel.ClearChatKeys, ClearChatHotKeyTb, e);
        }

        private void ClearChathKeyUpBindHandler(object sender, KeyEventArgs e)
        {
            RegisterHotKeyUp(ref _ClearChat, _TataruUIModel.ClearChatKeys, ClearChatHotKeyTb, e);
        }

        private void ShowHideChatWinHotKeyTb_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowHideChatWinHotKeyTb.Text = _EmptyHKString;

            RemoveHotKey(_TataruUIModel.ShowHideChatKeys, ref _ShowHideChat);
        }

        private void ClickThroughHotKeyTb_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ClickThroughHotKeyTb.Text = _EmptyHKString;

            RemoveHotKey(_TataruUIModel.ClickThoughtChatKeys, ref _ClickThoughtChat);
        }

        private void ClearChatHotKeyTb_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ClearChatHotKeyTb.Text = _EmptyHKString;

            RemoveHotKey(_TataruUIModel.ClearChatKeys, ref _ClearChat);
        }

        private void RegisterHotKeyDown(ref GlobalHotKey globalHotKey, HotKeyCombination hotKeyCombination, TextBox textBox, KeyEventArgs e)
        {
            try
            {
                var _key = Helper.RealKey(e);

                var pressedKeys = Keys.GetPressdKeys();

                pressedKeys = Keys.ClearRepeatedKeys(pressedKeys);

                pressedKeys = Keys.ClearMousKeys(pressedKeys);

                if (pressedKeys.Length <= 1)
                {
                    hotKeyCombination.ClearKeys();
                }

                hotKeyCombination.AddKey(_key);

                string str = _EmptyHKString;
                if (hotKeyCombination.IsInitialized)
                {
                    str = hotKeyCombination.CombinationKeysName();

                    textBox.Text = str;
                }
                else
                {
                    if (globalHotKey != null)
                    {
                        _HotKeyManager.RemoveGlobalHotKey(globalHotKey);
                    }
                }
                textBox.Text = str;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private void RegisterHotKeyUp(ref GlobalHotKey globalHotKey, HotKeyCombination hotKeyCombination, TextBox textBox, KeyEventArgs e)
        {
            try
            {
                var pressedKeys = Keys.GetPressdKeys();

                pressedKeys = Keys.ClearRepeatedKeys(pressedKeys);

                pressedKeys = Keys.ClearMousKeys(pressedKeys);

                string str = _EmptyHKString;
                if (pressedKeys.Length == 0)
                {
                    Keyboard.ClearFocus();

                    if (hotKeyCombination.IsInitialized)
                    {
                        str = hotKeyCombination.CombinationKeysName();

                        if (globalHotKey != null)
                        {
                            _HotKeyManager.RemoveGlobalHotKey(globalHotKey);
                        }
                        else
                        {
                            int t = 0;
                            t++;
                        }

                        var k = Keys.ConvertFromWpfKey(hotKeyCombination.NormalKey);
                        globalHotKey = new GlobalHotKey(hotKeyCombination.Name, hotKeyCombination.ModifierKey, k);

                        _HotKeyManager.AddGlobalHotKey(globalHotKey);

                        textBox.Text = str;
                    }
                    else
                    {
                        if (globalHotKey != null)
                        {
                            _HotKeyManager.RemoveGlobalHotKey(globalHotKey);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private void GlobalHotKeyHandler(object sender, GlobalHotKeyEventArgs e)
        {

            if (_ShowHideChat != null)
            {
                if (e.HotKey.Name == _ShowHideChat.Name)
                {
                    if (_ChatWindow.Visibility == Visibility.Visible)
                    {
                        _ChatWindow.UserHide();
                    }
                    else
                    {
                        _ChatWindow.UserShow();
                    }
                }
            }

            if (_ClickThoughtChat != null)
            {
                if (e.HotKey.Name == _ClickThoughtChat.Name)
                {
                    _TataruUIModel.IsChatClickThrough = !_TataruUIModel.IsChatClickThrough;
                }
            }

            if (_ClearChat != null)
            {
                if (e.HotKey.Name == _ClearChat.Name)
                {
                    _ChatWindow.ClearChat();

                    if (_ChatStreamWindow != null)
                        _ChatWindow.ClearChat();
                }
            }
        }

        #endregion

        #region **Tray.

        private void TBMenuChatWin_Click(object sender, RoutedEventArgs e)
        {
            /*
            Helper.Unminimize(_ChatWindow);

            _ChatWindow.Visibility = Visibility.Visible;
            _ChatWindow.Activate();
            _ChatWindow.Focus();//*/

            _ChatWindow.UserShow();
            _ChatWindow.Focus();
        }

        private void TBMenuSettingsWin_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsWindow();
        }
        private void TBDoubleClick(object sender, RoutedEventArgs e)
        {

            ShowSettingsWindow();
        }

        private void ShowSettingsWindow()
        {
            Helper.Unminimize(this);

            this.Visibility = Visibility.Visible;
            this.Activate();
            this.Focus();
        }

        private void TBMenuExit_Click(object sender, RoutedEventArgs e)
        {
            this.ShutDown();
        }

        #endregion

        #region **System.

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                if ((bool)HideToTray.IsChecked)
                    this.Hide();
            }

            base.OnStateChanged(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            if (_IsShutDown)
            {
                e.Cancel = false;
                base.OnClosing(e);
            }
            else
            {
                this.Hide();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_Updater != null)
                _Updater.Dispose();

            _LogWriter.Stop();
        }

        public void ShutDown()
        {
            _IsShutDown = true;
            Application.Current.Shutdown();
        }

        #endregion

    }
}
