// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;
using System.Windows.Interop;
using BondTech.HotKeyManagement.WPF._4;
using System.IO;
using NGettext;
using FFXIITataruHelper.WinUtils;
using FFXIITataruHelper.Translation;
using Squirrel;
using System.Threading;

namespace FFXIITataruHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window //-V3072
    {
        HotKeyManager _HotKeyManager;

        ChatWindow _ChatWindow;
        LogWriter _LogWriter;

        bool _IsHideToTray;

        string SettingFileName = "../AppSettings.json";
        string GitPath = @"https://github.com/NightlyRevenger/TataruHelper/releases/latest";
        string UpdatePath = @"https://github.com/NightlyRevenger/TataruHelper";

        string _EmptyHKString = "Empty";

        GlobalHotKey _ShowHideChat;
        GlobalHotKey _ClickThoughtChat;

        HotKeyCombination _ShowHideChatKeys;
        HotKeyCombination _ClickThoughtChatKeys;

        LanguagueWrapper _LanguagueWrapper;

        Task _UpdateInProgress = null;

        UpdateManager _UpdateManager;

        private bool _IsShutDown;

        public MainWindow()
        {
            _IsShutDown = false;
            _LogWriter = new LogWriter();
            _LogWriter.StartWriting();

            _ChatWindow = new ChatWindow(this);
            _ChatWindow.Show();

            _ShowHideChatKeys = new HotKeyCombination("ShowHideChatWin");
            _ClickThoughtChatKeys = new HotKeyCombination("ClickThoughtChatWin");

            _ChatWindow.IsVisibleChanged += ChatVisibleChanged;

            _IsHideToTray = false;

            InitializeComponent();

            _LanguagueWrapper = new LanguagueWrapper(this);

            SquirrelAwareApp.HandleEvents(
                onInitialInstall: v => OnInitialInstall(ref _UpdateManager),
                onAppUpdate: v => OnAppUpdate(ref _UpdateManager),
                onAppUninstall: v => OnAppUninstall(ref _UpdateManager));

            LoadSettings();
            //
        }

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
                _ChatWindow.Hide();
            }
            else
            {
                b.Content = (string)this.Resources["HideChatBox"];
                _ChatWindow.Show();
            }
        }

        private void RuLanguage_Click(object sender, RoutedEventArgs e)
        {
            _LanguagueWrapper.CurrentLanguage = LanguagueWrapper.Languages.Russian;

            GlobalSettings.CurentLanguague = (int)LanguagueWrapper.Languages.Russian;
        }

        private void EnLanguage_Click(object sender, RoutedEventArgs e)
        {
            _LanguagueWrapper.CurrentLanguage = LanguagueWrapper.Languages.English;

            GlobalSettings.CurentLanguague = (int)LanguagueWrapper.Languages.English;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            string caption = "TataruHelper v" + Convert.ToString(System.Reflection.Assembly.GetEntryAssembly().GetName().Version);
            string text = "TataruHelper" + Environment.NewLine + "GitHub: " + UpdatePath;

            MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(text, caption, MessageBoxButton.OK);
        }

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

        private void ClickThroughBox_Changed(object sender, RoutedEventArgs e)
        {
            _ChatWindow.IsClickThrought = (bool)((CheckBox)sender).IsChecked;
            GlobalSettings.IsClickThrough = (bool)((CheckBox)sender).IsChecked;
        }

        private void AlwayOnTopBox_Changed(object sender, RoutedEventArgs e)
        {
            _ChatWindow.Topmost = (bool)((CheckBox)sender).IsChecked;
            GlobalSettings.IsAlwaysOnTop = (bool)((CheckBox)sender).IsChecked;
        }

        private void ChatFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _ChatWindow.FontSize = (int)e.NewValue;
            GlobalSettings.FontSize = (int)e.NewValue;
        }

        private void BackgroundColor_Closed(object sender, RoutedEventArgs e)
        {
            ColorPicker cp = (ColorPicker)sender;
            SaveRecentColors(cp, GlobalSettings.RecentBackgroundColors);
        }

        private void BackgroundColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker cp = (ColorPicker)sender;
            _ChatWindow.FormBackGround = new SolidColorBrush(e.NewValue.Value);

            GlobalSettings.BackgroundColor = e.NewValue.Value.ToString();
        }

        private void FontColor1_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker cp = (ColorPicker)sender;
            _ChatWindow.FontColor1 = e.NewValue.Value;

            GlobalSettings.Font1Color = e.NewValue.Value.ToString();
        }

        private void FontColor2_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            ColorPicker cp = (ColorPicker)sender;
            _ChatWindow.FontColor2 = e.NewValue.Value;

            GlobalSettings.Font2Color = e.NewValue.Value.ToString();
        }

        private void FontColor1_Closed(object sender, RoutedEventArgs e)
        {
            ColorPicker cp = (ColorPicker)sender;
            SaveRecentColors(cp, GlobalSettings.RecentFont1Colors);
        }

        private void FontColor2_Closed(object sender, RoutedEventArgs e)
        {
            ColorPicker cp = (ColorPicker)sender;
            SaveRecentColors(cp, GlobalSettings.RecentFont2Colors);
        }

        private void HideToTray_Changed(object sender, RoutedEventArgs e)
        {
            _IsHideToTray = (bool)((CheckBox)sender).IsChecked;
            GlobalSettings.IsHideToTray = _IsHideToTray;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                if (_IsHideToTray)
                    this.Hide();
            }

            base.OnStateChanged(e);
        }

        public void LoadSettings()
        {
            try
            {

                BackgroundColor.ShowAvailableColors = false;
                BackgroundColor.ShowStandardColors = true;
                BackgroundColor.ShowRecentColors = true;

                FontColor1.ShowAvailableColors = false;
                FontColor1.ShowStandardColors = true;
                FontColor1.ShowRecentColors = true;

                FontColor2.ShowAvailableColors = false;
                FontColor2.ShowStandardColors = true;
                FontColor2.ShowRecentColors = true;

                if (!Helper.LoadStaticFromJson(typeof(GlobalSettings), SettingFileName))
                {
                    Helper.SaveStaticToJson(typeof(GlobalSettings), SettingFileName);
                    Helper.LoadStaticFromJson(typeof(GlobalSettings), SettingFileName);
                }

                IntervalWidth.Value = GlobalSettings.InsertSpaceCount;

                ChatFontSize.Value = GlobalSettings.FontSize;
                BackgroundColor.SelectedColor = (Color)ColorConverter.ConvertFromString(GlobalSettings.BackgroundColor);

                FontColor1.SelectedColor = (Color)ColorConverter.ConvertFromString(GlobalSettings.Font1Color);
                FontColor2.SelectedColor = (Color)ColorConverter.ConvertFromString(GlobalSettings.Font2Color);

                SetRecentColor(BackgroundColor, GlobalSettings.RecentBackgroundColors);
                SetRecentColor(FontColor1, GlobalSettings.RecentFont1Colors);
                SetRecentColor(FontColor2, GlobalSettings.RecentFont2Colors);

                bool tmpCheck = GlobalSettings.IsClickThrough;
                ClickThroughBox.IsChecked = !ClickThroughBox.IsChecked;
                ClickThroughBox.IsChecked = tmpCheck;

                tmpCheck = GlobalSettings.IsAlwaysOnTop;
                AlwayOnTopBox.IsChecked = !AlwayOnTopBox.IsChecked;
                AlwayOnTopBox.IsChecked = tmpCheck;
                _ChatWindow.Topmost = tmpCheck;

                LineBreakeHeight.Value = GlobalSettings.LineBreakHeight;

                _ChatWindow.TranslationEngine = (WebTranslator.TranslationEngine)GlobalSettings.CurrentTranslationEngine;

                TransaltorComboBox.SelectedIndex = -1;
                TransaltorComboBox.SelectedIndex = GlobalSettings.CurrentTranslationEngine;

                tmpCheck = GlobalSettings.IsHideToTray;
                HideToTray.IsChecked = !HideToTray.IsChecked;
                HideToTray.IsChecked = tmpCheck;

                LoadChatWindowSize();
                LoadMainWindowSize();

                _LanguagueWrapper.CurrentLanguage = (LanguagueWrapper.Languages)GlobalSettings.CurentLanguague;

            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        void LoadChatWindowSize()
        {
            double tmpTop = GlobalSettings.ChatWinTop, tmpLeft = GlobalSettings.ChatWinLeft;
            double tmpHeight = GlobalSettings.ChatWinHeight, tmpWidth = GlobalSettings.ChatWinWidth;

            if (tmpHeight > SystemParameters.VirtualScreenHeight)
                tmpHeight = SystemParameters.VirtualScreenHeight;

            if (tmpWidth > SystemParameters.VirtualScreenWidth)
                tmpWidth = SystemParameters.VirtualScreenWidth;

            if (tmpTop > SystemParameters.VirtualScreenHeight)
                tmpTop = SystemParameters.VirtualScreenHeight - _ChatWindow.Height;

            if (tmpLeft > SystemParameters.VirtualScreenWidth)
                tmpLeft = SystemParameters.VirtualScreenWidth - _ChatWindow.Width;

            if (GlobalSettings.ChatWinTop >= 0)
                _ChatWindow.Top = tmpTop;

            if (GlobalSettings.ChatWinLeft >= 0)
                _ChatWindow.Left = tmpLeft;

            if (GlobalSettings.ChatWinHeight >= 10)
                _ChatWindow.Height = tmpHeight;

            if (GlobalSettings.ChatWinWidth >= 10)
                _ChatWindow.Width = tmpWidth;
        }

        void LoadMainWindowSize()
        {
            double tmpHeight = GlobalSettings.MainWinHeight, tmpWidth = GlobalSettings.MainWinWidth;

            if (tmpHeight > SystemParameters.VirtualScreenHeight)
                tmpHeight = SystemParameters.VirtualScreenHeight;

            if (tmpWidth > SystemParameters.VirtualScreenWidth)
                tmpWidth = SystemParameters.VirtualScreenWidth;

            if (GlobalSettings.MainWinHeight >= 10)
                this.Height = tmpHeight;

            if (GlobalSettings.MainWinWidth >= 10)
                this.Width = tmpWidth;
        }

        void SaveRecentColors(ColorPicker cp, List<string> colors)
        {
            colors.Clear();
            foreach (var rc in cp.RecentColors)
            {
                colors.Add(rc.Color.Value.ToString());
            }

            SaveSettings();
        }

        void SetRecentColor(ColorPicker cp, List<string> colors)
        {
            List<ColorItem> colorItems = new List<ColorItem>();
            colors = colors.Distinct().ToList();
            foreach (var ci in colors)
            {
                colorItems.Add(new ColorItem((Color)ColorConverter.ConvertFromString(ci), ci));
            }
            cp.RecentColors = new System.Collections.ObjectModel.ObservableCollection<ColorItem>(colorItems);
        }

        void LoadComboBoxes()
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

            var lang = _ChatWindow.CurrentLanguages;

            for (int i = 0; i < lang.Count; i++)
            {
                TranslateToCombo.Items.Add(lang[i].ShownName);
            }

            for (int i = 0; i < GlobalSettings.FFXIVLanguagles.Count; i++)
            {
                var lng = lang.Where(x => x.ShownName.ToLower().Contains(GlobalSettings.FFXIVLanguagles[i].ToLower())).ToList();

                if (lng.Count > 0)
                {
                    TransalteFromCombo.Items.Add(GlobalSettings.FFXIVLanguagles[i]);
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

        void LoadLanguagues()
        {
            LoadComboBoxes();

            TransalteFromCombo.SelectedIndex = GlobalSettings.CurrentFFXIVLanguage;

            TranslateToCombo.SelectedIndex = GlobalSettings.CurrentTranslateToLanguage;
        }

        void LoadHotKeys()
        {
            List<HotKeyCombination> hotKeyCombinations = new List<HotKeyCombination>();
            hotKeyCombinations = Helper.LoadJsonData<List<HotKeyCombination>>(GlobalSettings.HotKeysFilePath);

            if (hotKeyCombinations.Count > 0)
            {
                _ShowHideChatKeys = hotKeyCombinations[0];
                _ClickThoughtChatKeys = hotKeyCombinations[1];
            }

            if (_ShowHideChatKeys != null)
            {
                if (_ShowHideChatKeys.IsInitialized)
                {
                    var k = Keys.ConvertFromWpfKey(_ShowHideChatKeys.NormalKey);

                    _ShowHideChat = new GlobalHotKey(_ShowHideChatKeys.Name, _ShowHideChatKeys.ModifierKey, k);
                    _HotKeyManager.AddGlobalHotKey(_ShowHideChat);

                    ShowHideChatWinHotKeyTb.Text = _ShowHideChatKeys.toLogString();
                }
            }
            if (_ClickThoughtChatKeys != null)
            {
                if (_ClickThoughtChatKeys.IsInitialized)
                {
                    var k = Keys.ConvertFromWpfKey(_ClickThoughtChatKeys.NormalKey);

                    _ClickThoughtChat = new GlobalHotKey(_ClickThoughtChatKeys.Name, _ClickThoughtChatKeys.ModifierKey, k);
                    _HotKeyManager.AddGlobalHotKey(_ClickThoughtChat);

                    ClickThroughHotKeyTb.Text = _ClickThoughtChatKeys.toLogString();
                }
            }
        }

        public void SaveSettings()
        {
            Helper.SaveStaticToJson(typeof(GlobalSettings), SettingFileName);

            List<HotKeyCombination> hotKeyCombinations = new List<HotKeyCombination>();
            hotKeyCombinations.Add(_ShowHideChatKeys);
            hotKeyCombinations.Add(_ClickThoughtChatKeys);

            Helper.SaveJson(hotKeyCombinations, GlobalSettings.HotKeysFilePath);
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
                {
                    e.Cancel = false;
                }

                if (e.Cancel == false)
                {
                    //_LogWriter.Stop();

                    _ChatWindow.Close();
                    SaveSettings();
                    _LogWriter.Stop();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(Convert.ToString(ex));
            }
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
            _UpdateManager.Dispose();
            _LogWriter.Stop();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLanguagues();
            _HotKeyManager = new HotKeyManager(this);

            ShowHideChatWinHotKeyTb.KeyDown += ShowHideKeyDownBindHandler;
            ShowHideChatWinHotKeyTb.KeyUp += ShowHideKeyUpBindHandler;

            ClickThroughHotKeyTb.KeyDown += ClickThoughKeyDownBindHandler;
            ClickThroughHotKeyTb.KeyUp += ClickThoughKeyUpBindHandler;

            _HotKeyManager.GlobalHotKeyPressed += GlobalHotKeyHandler;

            LoadHotKeys();

            Task.Run(async () =>
            {
                await Task.Delay(GlobalSettings.LookForPorcessDelay * 4);
                Squirrel_Update();
            });
        }

        private void RegisterHotKeyDown(ref GlobalHotKey globalHotKey, HotKeyCombination hotKeyCombination, TextBox textBox, KeyEventArgs e)
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
                str = hotKeyCombination.toLogString();

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

        private void RegisterHotKeyUp(ref GlobalHotKey globalHotKey, HotKeyCombination hotKeyCombination, TextBox textBox, KeyEventArgs e)
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
                    str = hotKeyCombination.toLogString();

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

        private void ShowHideKeyDownBindHandler(object sender, KeyEventArgs e)
        {
            RegisterHotKeyDown(ref _ShowHideChat, _ShowHideChatKeys, ShowHideChatWinHotKeyTb, e);
        }

        private void ShowHideKeyUpBindHandler(object sender, KeyEventArgs e)
        {
            RegisterHotKeyUp(ref _ShowHideChat, _ShowHideChatKeys, ShowHideChatWinHotKeyTb, e);
        }

        private void ClickThoughKeyDownBindHandler(object sender, KeyEventArgs e)
        {
            RegisterHotKeyDown(ref _ClickThoughtChat, _ClickThoughtChatKeys, ClickThroughHotKeyTb, e);
        }

        private void ClickThoughKeyUpBindHandler(object sender, KeyEventArgs e)
        {
            RegisterHotKeyUp(ref _ClickThoughtChat, _ClickThoughtChatKeys, ClickThroughHotKeyTb, e);
        }

        private void ShowHideChatWinHotKeyTb_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _ShowHideChatKeys.ClearKeys();

            ShowHideChatWinHotKeyTb.Text = _EmptyHKString;

            if (_ShowHideChat != null)
                _HotKeyManager.RemoveGlobalHotKey(_ShowHideChat);
        }

        private void ClickThroughHotKeyTb_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _ClickThoughtChatKeys.ClearKeys();

            ClickThroughHotKeyTb.Text = _EmptyHKString;

            if (_ClickThoughtChat != null)
                _HotKeyManager.RemoveGlobalHotKey(_ClickThoughtChat);
        }

        private void GlobalHotKeyHandler(object sender, GlobalHotKeyEventArgs e)
        {

            if (_ShowHideChatKeys != null)
            {
                if (e.HotKey.Name == _ShowHideChatKeys.Name)
                {
                    if (_ChatWindow.Visibility == Visibility.Visible)
                    {
                        Button2.Content = "Show ChatBox";
                        _ChatWindow.Hide();
                    }
                    else
                    {
                        Button2.Content = "Hide ChatBox";
                        _ChatWindow.Show();
                    }
                }
            }

            if (_ClickThoughtChat != null)
            {
                if (e.HotKey.Name == _ClickThoughtChat.Name)
                {
                    ClickThroughBox.IsChecked = !ClickThroughBox.IsChecked;
                }
            }
        }

        private void TransaltorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            GlobalSettings.CurrentTranslationEngine = cb.SelectedIndex;

            _ChatWindow.TranslationEngine = (WebTranslator.TranslationEngine)GlobalSettings.CurrentTranslationEngine;

            LoadComboBoxes();
        }

        private void FFXIVLanguague_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            GlobalSettings.CurrentFFXIVLanguage = cb.SelectedIndex;

            _ChatWindow.FFXIVLanguague = Convert.ToString(cb.SelectedValue);
        }

        private void TranslateTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            GlobalSettings.CurrentTranslateToLanguage = cb.SelectedIndex;

            _ChatWindow.TargetLanguage = Convert.ToString(cb.SelectedValue);
        }

        private void LineBreakeHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _ChatWindow.LineBreakeHight = (int)e.NewValue;
            GlobalSettings.LineBreakHeight = (int)e.NewValue;
        }

        private void IntervalWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _ChatWindow.InsertSpaceCount = (int)e.NewValue;
            GlobalSettings.InsertSpaceCount = (int)e.NewValue;
        }

        private void TBMenuChatWin_Click(object sender, RoutedEventArgs e)
        {
            Helper.Unminimize(_ChatWindow);

            _ChatWindow.Visibility = Visibility.Visible;
            _ChatWindow.Activate();
            _ChatWindow.Focus();
        }

        private void TBMenuSettingsWin_Click(object sender, RoutedEventArgs e)
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

        public void ShutDown()
        {
            _IsShutDown = true;
            Application.Current.Shutdown();
        }

        private void TBDoubleClick(object sender, RoutedEventArgs e)
        {
            Helper.Unminimize(this);

            this.Visibility = Visibility.Visible;
            this.Activate();
            this.Focus();

        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GlobalSettings.MainWinHeight = this.Height;

            GlobalSettings.MainWinWidth = this.Width;
        }

        private void Squirrel_Update()
        {
            Task.Run(async () =>
            {
                await UpdateIfAvailable();
            });

            SpinWait.SpinUntil(() => _UpdateInProgress != null);

            Task.Run(() =>
            {
                var res = WaitForUpdatesOnShutdown();
                res.Wait();

            }).Wait();
        }

        public async Task UpdateIfAvailable()
        {
            _UpdateInProgress = RealUpdateIfAvailable();
            await _UpdateInProgress;
        }

        public async Task WaitForUpdatesOnShutdown()
        {
            // We don't actually care about errors here, only completion
            await _UpdateInProgress.ContinueWith(ex => { });
        }

        private async Task RealUpdateIfAvailable()
        {
            Logger.WriteLog("Checking remote server for update.");

            try
            {
                _UpdateManager = await UpdateManager.GitHubUpdateManager(UpdatePath);

                var res = await _UpdateManager.UpdateApp();

                string test = String.Empty;

                try
                {
                    if (res != null)
                    {
                        test = res.BaseUrl + Environment.NewLine;
                        test += res.EntryAsString + Environment.NewLine;
                        test += res.PackageName + Environment.NewLine;
                        test += res.Version + Environment.NewLine;
                        test += "IsDelta: " + res.IsDelta + Environment.NewLine;
                    }
                    else
                    {
                        Logger.WriteLog("No Updates Found");
                    }

                    if (test.Length > 0)
                        Logger.WriteLog(test);

                }
                catch (Exception ex3)
                {
                    Logger.WriteLog(ex3);
                }

            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private void OnInitialInstall(ref UpdateManager mgr)
        {
            try
            {
                //mgr.CreateShortcutForThisExe();
                //mgr.CreateShortcutsForExecutable("Navvy.Desktop.exe", ShortcutLocation.Desktop, false);
                //mgr.CreateShortcutsForExecutable("Navvy.Desktop.exe", ShortcutLocation.StartMenu, false);
                mgr.CreateUninstallerRegistryEntry();

            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        private void OnAppUpdate(ref UpdateManager mgr)
        {
            try
            {
                mgr.RemoveUninstallerRegistryEntry();
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }

            try
            {
                mgr.CreateUninstallerRegistryEntry();
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        private void OnAppUninstall(ref UpdateManager mgr)
        {
            try
            {
                //mgr.RemoveShortcutsForExecutable("Navvy.Desktop.exe", ShortcutLocation.Desktop);
                //mgr.RemoveShortcutsForExecutable("Navvy.Desktop.exe", ShortcutLocation.StartMenu);
                mgr.RemoveUninstallerRegistryEntry();
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

    }
}
