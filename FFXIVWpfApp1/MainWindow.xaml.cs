// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using BondTech.HotKeyManagement.WPF._4;
using FFXIVTataruHelper.EventArguments;
using FFXIVTataruHelper.ViewModel;
using FFXIVTataruHelper.WinUtils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Timers;
using Xceed.Wpf.Toolkit;
using Updater;
using Updater.EventArguments;

namespace FFXIVTataruHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml//
    /// </summary>
    public partial class MainWindow : Window //-V3072
    {
        TataruModel TataruModel
        {
            get { return _TataruModel; }
            set { _TataruModel = value; }
        }

        LogWriter _LogWriter;

        TataruModel _TataruModel;
        TataruUIModel _TataruUIModel;

        Timer _UpdaterTimer = null;

        ///////////////////////////////////////////////////

        LanguagueWrapper _LanguagueWrapper;

        SquirrelUpdater _Updater;

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
            }

            try
            {
                _LogWriter = new LogWriter();
                _LogWriter.StartWriting();

                Logger.WriteLog("TataruHelper v" + Convert.ToString(System.Reflection.Assembly.GetEntryAssembly().GetName().Version));
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }

            try
            {
                InitializeComponent();

                UiWindow.Window = this;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);

                return;
            }

            try
            {
                _LanguagueWrapper = new LanguagueWrapper(this);
                _Updater = new SquirrelUpdater(new Utils.LoggerWrapper());
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }
        }

        #region **UserActions.

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var _AboutWin = new AboutWin();
            _AboutWin.Show();
        }

        private void Patrons_Click(object sender, RoutedEventArgs e)
        {
            var patreonWin = new PatreonWin();
            patreonWin.Show();

            patreonWin.Resources["DearPatrons"] = this.Resources["DearPatrons"];
            patreonWin.Resources["PatronsMsg"] = this.Resources["PatronsMsg"];
            patreonWin.Resources["PatronsThankYou"] = this.Resources["PatronsThankYou"];
        }

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://discord.gg/bSrpbd9");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri));
        }

        private void HideToTray_Changed(object sender, RoutedEventArgs e)
        {
            var isHideToTray = (bool)((CheckBox)sender).IsChecked;
            _TataruUIModel.IsHideSettingsToTray = isHideToTray;
        }

        private void DirectMemoryReading_Changed(object sender, RoutedEventArgs e)
        {
            var isDirectMemoryReading = (bool)((CheckBox)sender).IsChecked;
            _TataruUIModel.IsDirecMemoryReading = isDirectMemoryReading;

        }

        private void RestartApp_Click(object sender, RoutedEventArgs e)
        {
            _Updater.RestartApp();
        }

        private void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            UserStartedUpdateText.Text = (string)this.Resources["LookingForUpdates"];

            _TataruModel.TataruViewModel.UpdateCheckByUser = true;
            _TataruModel.TataruViewModel.UserStartedUpdateTextVisibility = true;

            _Updater.CheckAndInstallUpdates(CmdArgsStatus.IsPreRelease);
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        #endregion

        #region **WindowEvents

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.WriteLog("TataruHelper v" + Convert.ToString(System.Reflection.Assembly.GetEntryAssembly().GetName().Version));
            }
            catch (Exception) { }

            try
            {
                try
                {
                    _TataruModel = new TataruModel(this);

                    await _TataruModel.InitializeComponent();

                    _TataruUIModel = _TataruModel.TataruUIModel;

                    InitTataruModel();
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(ex);
                }

                _TataruModel.AsyncLoadSettings().Forget();

                _TataruModel.FFMemoryReader.AddExclusionWindowHandler((new WindowInteropHelper(this).Handle));

                this.DataContext = _TataruModel.TataruViewModel;

                _TataruModel.TataruViewModel.ShutdownRequested += OnShutDownRequsted;

                _OptimizeFootprint = new OptimizeFootprint();
                _OptimizeFootprint.Start();

                _WinMessagesHandler = new WinMessagesHandler(this);
                _WinMessagesHandler.ShowFirstInstance += OnShowFirstInstance;

                _Updater.UpdatingStateChanged += OnUpdaterEvent;

#if DEBUG
#else
                Task.Run(() =>
                {

                    _Updater.CheckAndInstallUpdates(CmdArgsStatus.IsPreRelease);

                    _UpdaterTimer = new Timer(TimeSpan.FromMinutes(30).TotalMilliseconds);
                    _UpdaterTimer.Elapsed += async (senderr, ee) => await UpdateTimerHandler();
                    _UpdaterTimer.AutoReset = true;
                    _UpdaterTimer.Start();

                }).Forget();
#endif
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            System.Drawing.PointD winSize = new System.Drawing.PointD(this.Width, this.Height);

            if (_TataruUIModel != null)
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
                    _UpdaterTimer?.Stop();
                    _Updater?.StopUpdate();

                    if (_OptimizeFootprint != null)
                        _OptimizeFootprint.Stop();

                    if (_TataruModel != null)
                        _TataruModel.Stop();

                    Task.Run(async () =>
                    {
                        if (_TataruModel != null)
                            await _TataruModel.SaveSettings();
                    }).Wait();

                    Utils.TataruSingleInstance.Stop();

                    if (_LogWriter != null)
                        _LogWriter.Stop();

                    _Updater?.KillAll();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(Convert.ToString(ex));
            }
        }

        #endregion

        #region **UiEvents.

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

        private async Task OnSettingsWindowSizeChange(PointDValueChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                System.Drawing.PointD winSize = new System.Drawing.PointD(this.Width, this.Height);

                var UIModel = ((TataruUIModel)ea.Sender);

                if (UIModel.IsFirstTime == 0)
                {
                    UIModel.IsFirstTime = -1;
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

        private async Task OnShowFirstInstance(BooleanChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                ShowSettingsWindow();
            });
        }

        private async Task OnUpdaterEvent(UpdatingState ea)
        {
            switch (ea.UpdateSate)
            {
                case UpdateSate.InitializingUpdater:
                    await this.UIThreadAsync(() =>
                    {
                        CheckUpdatesButton.IsEnabled = false;
                    });
                    break;
                case UpdateSate.DownloadingUpdate:
                    await this.UIThreadAsync(() =>
                    {
                        _TataruModel.TataruViewModel.UserStartedUpdateTextVisibility = false;

                        _TataruModel.TataruViewModel.DownloadingUpdateVisibility = true;
                    });
                    break;
                case UpdateSate.ReadyToRestart:
                    await this.UIThreadAsync(() =>
                    {
                        _TataruModel.TataruViewModel.UserStartedUpdateTextVisibility = false;

                        _TataruModel.TataruViewModel.RestartReadyVisibility = true;
                        _TataruModel.TataruViewModel.DownloadingUpdateVisibility = false;

                        TaskBarIcon.ShowBalloonTip((string)this.Resources["NotifyUpdateTitle"], (string)this.Resources["NotifyUpdateText"],
                            Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    });
                    break;
                case UpdateSate.UpdatingFinished:
                    await this.UIThreadAsync(() =>
                    {
                        _TataruModel.TataruViewModel.UserStartedUpdateTextVisibility = false;

                        if (!_TataruModel.TataruViewModel.RestartReadyVisibility
                        && !_TataruModel.TataruViewModel.DownloadingUpdateVisibility
                        && _TataruModel.TataruViewModel.UpdateCheckByUser
                        )
                        {
                            UserStartedUpdateText.Text = (string)this.Resources["NoUpdatesFound"];
                            _TataruModel.TataruViewModel.UserStartedUpdateTextVisibility = true;
                        }
                        _TataruModel.TataruViewModel.UpdateCheckByUser = false;
                        OnUserStartedUpdateEnd();
                    });
                    break;
            }

        }

        void OnUserStartedUpdateEnd()
        {
            Task.Run(async () =>
            {
                await Task.Delay((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
                await this.UIThreadAsync(() =>
                {
                    _TataruModel.TataruViewModel.UserStartedUpdateTextVisibility = false;
                    CheckUpdatesButton.IsEnabled = true;
                });
            });
        }

        #endregion

        #region **Initialization.

        void InitTataruModel()
        {
            var UIModel = _TataruModel.TataruUIModel;

            UIModel.UiLanguageChanged += OnUiLanguageChange;


            UIModel.IsHideSettingsToTrayChanged += OnHideSettingsToTrayChange;
            UIModel.IsDirecMemoryReadingChanged += OnDirecMemoryReadingChange;

            UIModel.SettingsWindowSizeChanged += OnSettingsWindowSizeChange;

            _TataruModel.FFMemoryReader.FFWindowStateChanged += OnFFWindowStateChange;
        }

        #endregion

        #region **HotKeys.

        private void ShowHideChatWinHotKey_KeyDown(object sender, KeyEventArgs e)
        {
            var mdl = _TataruModel.TataruViewModel.CurrentChatWindow;
            if (mdl != null)
            {
                mdl.RegisterHotKeyDown(TatruHotkeyType.ShowHideChatWindow, e);
            }
        }

        private void ShowHideChatWinHotKeyBox_KeyUp(object sender, KeyEventArgs e)
        {
            var mdl = _TataruModel.TataruViewModel.CurrentChatWindow;
            if (mdl != null)
            {
                mdl.RegisterHotKeyUp(TatruHotkeyType.ShowHideChatWindow, e);
            }
        }

        private void ClickThroughHotKey_KeyDown(object sender, KeyEventArgs e)
        {
            var mdl = _TataruModel.TataruViewModel.CurrentChatWindow;
            if (mdl != null)
            {
                mdl.RegisterHotKeyDown(TatruHotkeyType.ClickThrought, e);
            }
        }

        private void ClickThroughHotKey_KeyUp(object sender, KeyEventArgs e)
        {
            var mdl = _TataruModel.TataruViewModel.CurrentChatWindow;
            if (mdl != null)
            {
                mdl.RegisterHotKeyUp(TatruHotkeyType.ClickThrought, e);
            }
        }

        private void ClearChatHotKey_KeyDown(object sender, KeyEventArgs e)
        {
            var mdl = _TataruModel.TataruViewModel.CurrentChatWindow;
            if (mdl != null)
            {
                mdl.RegisterHotKeyDown(TatruHotkeyType.ClearChat, e);
            }
        }

        private void ClearChatHotKey_KeyUp(object sender, KeyEventArgs e)
        {
            var mdl = _TataruModel.TataruViewModel.CurrentChatWindow;
            if (mdl != null)
            {
                mdl.RegisterHotKeyUp(TatruHotkeyType.ClearChat, e);
            }
        }

        #endregion

        #region **Tray.

        private void TBMenuSettingsWin_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsWindow();
        }

        private void TBDoubleClick(object sender, RoutedEventArgs e)
        {

            ShowSettingsWindow();
        }

        public void ShowSettingsWindow()
        {
            Helper.Unminimize(this);

            this.Visibility = Visibility.Visible;
            this.Activate();
            this.Focus();
        }

        private void OnShutDownRequsted(object sender, EventArgs e)
        {
            this.ShutDown();
        }

        #endregion

        #region **System.

        private async Task UpdateTimerHandler()
        {
            await Task.Run(() =>
            {
                _Updater?.CheckAndInstallUpdates(CmdArgsStatus.IsPreRelease);
            });
        }

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
            _IsShutDown = true;

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
            _LogWriter?.Stop();
        }

        public void ShutDown()
        {
            _IsShutDown = true;
            Application.Current.Shutdown();
        }

        #endregion
    }
}
