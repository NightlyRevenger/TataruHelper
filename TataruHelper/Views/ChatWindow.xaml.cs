using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using FFXIVTataruHelper.EventArguments;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.Settings;
using FFXIVTataruHelper.Services.UI;
using FFXIVTataruHelper.TataruComponentModel;
using FFXIVTataruHelper.ViewModel;
using FFXIVTataruHelper.WinUtils;

using Translation.Models;

namespace FFXIVTataruHelper
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        public IntPtr WindowHandle
        {
            get { return new WindowInteropHelper(this).Handle; }
        }

        public long WinId
        {
            get { return _ChatWindowViewModel.WinId; }
        }

        private WindowResizer _WindowResizer;

        private WindowClickThroughBehavior _clickThroughBehavior;

        private ChatMessageParagraphBuilder _paragraphBuilder;

        private DateTime _TextArrivedTime;

        private readonly CancellationTokenSource _lifecycleCts =
            new CancellationTokenSource();

        private bool _AutoHidden;

        private readonly HashSet<string> _reportedFailureEngines = new HashSet<string>(StringComparer.Ordinal);

        private const uint TopMostSetWindowPosFlags =
            Win32Interfaces.SWP_NOMOVE |
            Win32Interfaces.SWP_NOSIZE |
            Win32Interfaces.SWP_NOACTIVATE |
            Win32Interfaces.SWP_NOOWNERZORDER;

        TataruModel _TataruModel;
        ChatWindowViewModel _ChatWindowViewModel;

        MainWindow _MainWindow;
        readonly IAppLogger _Logger;
        readonly ISettingsStore _SettingsStore;
        readonly IUiDispatcher _UiDispatcher;
        readonly DialogueOverlayHost _dialogueOverlayHost;

        public ChatWindow(
            TataruModel tataruModel,
            ChatWindowViewModel chatWindowViewModel,
            MainWindow mainWindow,
            IAppLogger logger,
            ISettingsStore settingsStore,
            IUiDispatcher uiDispatcher,
            DialogueOverlayHost dialogueOverlayHost)
        {
            InitializeComponent();

            try
            {
                _MainWindow = mainWindow;

                _TataruModel = tataruModel;
                _ChatWindowViewModel = chatWindowViewModel;
                _Logger = logger;
                _SettingsStore = settingsStore;
                _UiDispatcher = uiDispatcher;
                _dialogueOverlayHost = dialogueOverlayHost;

                this.DataContext = _ChatWindowViewModel;

                this.ShowInTaskbar = false;

                ChatRtb.AcceptsTab = true;

                ChatRtb.BorderThickness = new Thickness(0);

                ChatRtb.Document.Blocks.Clear();

                ChatRtb.IsUndoEnabled = false;
                ApplyContentPadding();

                _TextArrivedTime = DateTime.UtcNow;

                _AutoHidden = false;
                _WindowResizer = new WindowResizer(this, _Logger);
                _clickThroughBehavior = new WindowClickThroughBehavior(this, _Logger);
                _paragraphBuilder = new ChatMessageParagraphBuilder(_ChatWindowViewModel);
            }
            catch (Exception e)
            {
                _Logger.WriteLog(e);
            }
        }

        #region **UserActions.

        public async Task ClearChat()
        {
            await this.UIThreadAsync(() =>
            {
                ChatRtb.Document.Blocks.Clear();
            });
        }

        #endregion

        #region **WindowEvents.

        protected virtual void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AutoHideStatusCheck();

            _TataruModel.FFMemoryReader.AddExclusionWindowHandler((new WindowInteropHelper(this).Handle));

            if (_ChatWindowViewModel.IsClickThrough)
                MakeWindowClickThrough();
            else
                MakeWindowClickable();

            ApplyAlwaysOnTop();

            _ChatWindowViewModel.AsyncPropertyChanged += OnSettingsWindowPropertyChange;
            _ChatWindowViewModel.RequestChatClear += OnChatClearRequest;
            _TataruModel.FFMemoryReader.AsyncPropertyChanged += OnMemoryReaderPropertyChange;

            _TataruModel.ChatProcessor.TextArrived += OnTextArrived;
            _TataruModel.FFMemoryReader.FFWindowStateChanged += OnFFWindowStateChange;

            _dialogueOverlayHost.WantedChanged += OnDialogueOverlayWantedChanged;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _lifecycleCts.Cancel();
        }

        protected virtual void Window_Deactivated(object sender, EventArgs e)
        {
            int t = 0;
            t++;
        }

        #endregion

        #region **UiEvents.

        protected async Task OnTextArrived(ChatMessageArrivedEventArgs ea)
        {
            string text = "";
            string speaker = "";
            Color textColor = Color.FromArgb(255, 255, 255, 255);
            ChatCodeViewModel chatCode;

            chatCode = _ChatWindowViewModel.ChatCodes.FirstOrDefault(x => x.Code == ea.ChatMessage.Code);

            if (chatCode == null)
                return;
            if (!chatCode.IsChecked)
                return;

            textColor = chatCode.Color;

            if (ea.ChatMessage.Text.Length > 0)
            {
                TranslationResult result = default;

                using (var translationCts = new CancellationTokenSource(_SettingsStore.TranslatorWaitTimeMs))
                {
                    try
                    {
                        result = await _TataruModel.ChatProcessor.Translate(
                            ea.ChatMessage.Text,
                            _ChatWindowViewModel.SelectedEngine,
                            _ChatWindowViewModel.CurrentTranslateFromLanguage,
                            _ChatWindowViewModel.CurrentTranslateToLanguage,
                            chatCode.Code,
                            translationCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        result = TranslationResult.Failure(
                            _ChatWindowViewModel.SelectedEngine?.EngineName ?? default,
                            TranslationFailureKind.ProviderException,
                            "Translation timed out.");
                    }
                }

                if (result.IsSuccess)
                {
                    text = result.Text;
                    speaker = result.SpeakerName;

                    // The selected engine failed and another one answered instead.
                    // Adopt it so the picker reflects what is actually translating.
                    if (_ChatWindowViewModel.SelectedEngine != null &&
                        result.Engine != _ChatWindowViewModel.SelectedEngine.EngineName)
                    {
                        var previousEngineName = _ChatWindowViewModel.SelectedEngine.Name;
                        var substitute = _ChatWindowViewModel.AvailableEngines
                            .FirstOrDefault(engine => engine.EngineName == result.Engine);

                        if (substitute != null)
                        {
                            // Attached to the line rather than sent as its own
                            // message: with "show only the last message" on, a
                            // separate notice is wiped by the very next translation.
                            text = FormatEngineSwitchNotice(previousEngineName, substitute.Name) + " " + text;

                            _UiDispatcher.Invoke(() => _ChatWindowViewModel.SelectedEngine = substitute);
                        }
                    }
                }
                else
                {
                    var failedEngineName = _ChatWindowViewModel.SelectedEngine?.Name ?? result.Engine.ToString();
                    if (_reportedFailureEngines.Add(failedEngineName))
                    {
                        _UiDispatcher.Invoke(() => ShowFailureNotice(failedEngineName, textColor));
                    }

                    return;
                }
            }
            else
            {
                text = string.Empty;
            }

            DateTime timeStamp = default(DateTime);

            if (_ChatWindowViewModel.ShowTimestamps)
                timeStamp = ea.ChatMessage.TimeStamp;

            await this.UIThreadAsync(() =>
            {
                ShowWindow();

                if (_ChatWindowViewModel.IsHiddenByUser == false)
                    _TextArrivedTime = DateTime.UtcNow;

                ShowTranslatedText(text, textColor, speaker, timeStamp, ea.ChatMessage.Text);

                if (_ChatWindowViewModel.IsHiddenByUser == false)
                    _TextArrivedTime = DateTime.UtcNow;
            });
        }

        protected virtual async Task OnSettingsWindowPropertyChange(AsyncPropertyChangedEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                switch (ea.PropertyName)
                {
                    case "IsClickThrough":

                        if (_ChatWindowViewModel.IsClickThrough)
                            MakeWindowClickThrough();
                        else
                            MakeWindowClickable();

                        break;
                    case "IsAlwaysOnTop":
                        {
                            ApplyAlwaysOnTop();

                            if (!_ChatWindowViewModel.IsAlwaysOnTop &&
                                !_TataruModel.FFMemoryReader.IsGameWindowForeground)
                            {
                                _ChatWindowViewModel.IsWindowVisible = false;
                            }
                            else if (_ChatWindowViewModel.IsAlwaysOnTop && !_ChatWindowViewModel.IsHiddenByUser)
                            {
                                _ChatWindowViewModel.IsWindowVisible = true;
                                ShowWindow();
                            }
                        }
                        break;
                    case "IsAutoHide":
                        {
                            _TextArrivedTime = DateTime.UtcNow;
                            if (!_ChatWindowViewModel.IsAutoHide)
                                ShowWindow();
                        }
                        break;
                    case "IsWindowVisible":
                        {
                            if (_ChatWindowViewModel.IsWindowVisible == true)
                            {
                                _TextArrivedTime = DateTime.UtcNow;
                                ShowWindow();
                                ApplyAlwaysOnTop();
                            }
                        }
                        break;
                    case "BackGroundColor":
                        {
                            if (_ChatWindowViewModel.BackGroundColor.A == 255)
                                this.AllowsTransparency = false;
                            else
                                this.AllowsTransparency = true;

                            if (_ChatWindowViewModel.IsClickThrough)
                            {
                                MakeWindowClickable();
                                MakeWindowClickThrough();
                            }
                        }
                        break;
                    case "ContentPadding":
                        {
                            ApplyContentPadding();
                        }
                        break;
                    case "MessageContainerPadding":
                        {
                            ApplyMessageContainerVisuals();
                        }
                        break;
                    case "MessageContainerAlpha":
                        {
                            ApplyMessageContainerVisuals();
                        }
                        break;
                    case "MessageContainerBorderThickness":
                        {
                            ApplyMessageContainerVisuals();
                        }
                        break;
                    case "MessageContainerBorderAlpha":
                        {
                            ApplyMessageContainerVisuals();
                        }
                        break;
                    case "MessagesInContainer":
                        {
                            if (_ChatWindowViewModel.MessagesInContainer)
                            {
                                ApplyMessageContainerVisuals();
                            }
                        }
                        break;
                    case "ShowOnlyLastMessage":
                        {
                            if (_ChatWindowViewModel.ShowOnlyLastMessage)
                            {
                                EnforceLastMessageOnly();
                            }
                        }
                        break;
                }
            });
        }

        protected virtual async Task OnChatClearRequest(TatruEventArgs ea)
        {
            await ClearChat();
        }

        protected virtual async Task OnFFWindowStateChange(WindowStateChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.IsRunningNew != ea.IsRunningOld)
                {
                    //_TataruUIModel.IsHiddenByUser = false;

                    _ChatWindowViewModel.IsHiddenByUser = false;

                    //_TextArrivedTime = DateTime.UtcNow;

                    //_AutoHidden = false;
                }

                if (ea.NewWindowState != ea.OldWindowState)
                {
                    if (ea.NewWindowState == WindowState.Minimized)
                    {
                        _ChatWindowViewModel.IsWindowVisible = false;
                    }
                    else if (_ChatWindowViewModel.IsHiddenByUser == false)
                    {
                        if (_AutoHidden == false)
                            _ChatWindowViewModel.IsWindowVisible = true;
                    }
                }
            });
        }

        protected virtual async Task OnMemoryReaderPropertyChange(AsyncPropertyChangedEventArgs ea)
        {
            if (!string.Equals(ea.PropertyName, "IsGameWindowForeground", StringComparison.Ordinal))
                return;

            await this.UIThreadAsync(() =>
            {
                if (_ChatWindowViewModel.IsAlwaysOnTop)
                {
                    ApplyAlwaysOnTop();
                    return;
                }

                if (_TataruModel.FFMemoryReader.IsGameWindowForeground)
                {
                    if (!_ChatWindowViewModel.IsHiddenByUser && !_AutoHidden)
                        _ChatWindowViewModel.IsWindowVisible = true;
                }
                else
                {
                    _ChatWindowViewModel.IsWindowVisible = false;
                }

                ApplyAlwaysOnTop();
            });
        }

        #endregion

        #region **Transaltion.

        private DialogueOverlayWindow _dialogueOverlay;

        /// <summary>
        /// The copy of the game's dialogue box, made the first time there is
        /// something to put in it. Nothing is created while the setting is off,
        /// so it costs a comparison and not a window - and nothing is created
        /// by a window that does not hold the claim, so several chat windows
        /// do not stack several copies over the one box.
        /// </summary>
        private DialogueOverlayWindow EnsureDialogueOverlay()
        {
            if (!_dialogueOverlayHost.IsWanted || !_dialogueOverlayHost.BelongsTo(this))
            {
                return null;
            }

            if (_dialogueOverlay == null)
            {
                _dialogueOverlay = new DialogueOverlayWindow(
                    _TataruModel.FFMemoryReader,
                    () => _TataruModel.FFMemoryReader.GameWindowHandle);
                _dialogueOverlay.Start();
            }

            return _dialogueOverlay;
        }

        /// <summary>
        /// Takes the copy down and lets it go. Called when the setting is
        /// turned off and when this window closes; the claim is a separate
        /// matter, since a window that is merely not wanted right now is still
        /// the one that would fill the copy if it were.
        /// </summary>
        private void CloseDialogueOverlay()
        {
            if (_dialogueOverlay == null)
            {
                return;
            }

            _dialogueOverlay.Stop();
            _dialogueOverlay.Close();
            _dialogueOverlay = null;
        }

        /// <summary>
        /// The setting was turned on or off. Turned off, the copy comes down
        /// there and then rather than staying over the game until whatever is
        /// said next - which, in a quiet moment, is a long time to look at
        /// something you have just switched off.
        /// </summary>
        private void OnDialogueOverlayWantedChanged(object sender, EventArgs e)
        {
            _UiDispatcher.Invoke(() =>
            {
                if (!_dialogueOverlayHost.IsWanted)
                {
                    CloseDialogueOverlay();
                }
            });
        }

        void ShowTranslatedText(
            string translatedMsg, Color color, string speaker = "", DateTime timeStamp = default(DateTime),
            string sourceLine = null)
        {
            try
            {
                translatedMsg = translatedMsg.Trim(new char[] { ' ' });
                if (_ChatWindowViewModel.ShowOnlyLastMessage)
                {
                    ChatRtb.Document.Blocks.Clear();
                }

                EnsureDialogueOverlay()?.SetLine(speaker, translatedMsg, sourceLine);

                Paragraph paragraph = _paragraphBuilder.BuildMessageParagraph(
                    translatedMsg, color, speaker, timeStamp);
                ChatRtb.Document.Blocks.Add(paragraph);

                TrimToMaxMessages();

                ChatRtb.ScrollToEnd();
            }
            catch (Exception exc)
            {
                _Logger.WriteLog(Convert.ToString(exc));
            }
        }

        private void ApplyContentPadding()
        {
            ChatRtb.Padding = new Thickness(_ChatWindowViewModel.ContentPadding);
        }

        private void ApplyMessageContainerVisuals()
        {
            foreach (var paragraph in ChatRtb.Document.Blocks.OfType<Paragraph>())
            {
                foreach (var inline in paragraph.Inlines)
                {
                    if (inline is InlineUIContainer container && container.Child is Border border)
                    {
                        _paragraphBuilder.ApplyMessageContainerVisual(border);
                    }
                }
            }
        }

        private void EnforceLastMessageOnly()
        {
            while (ChatRtb.Document.Blocks.Count > 1)
            {
                ChatRtb.Document.Blocks.Remove(ChatRtb.Document.Blocks.FirstBlock);
            }
        }

        private void TrimToMaxMessages()
        {
            int maxMessages = _SettingsStore.MaxChatMessages;
            if (maxMessages <= 0)
            {
                return;
            }

            while (ChatRtb.Document.Blocks.Count > maxMessages)
            {
                ChatRtb.Document.Blocks.Remove(ChatRtb.Document.Blocks.FirstBlock);
            }
        }

        static string FormatEngineSwitchNotice(string failedEngineName, string newEngineName)
        {
            var template = (string)Application.Current.Resources["TranslationEngineSwitched"];
            return string.Format(template ?? "[{0} → {1}]", failedEngineName, newEngineName);
        }

        void ShowFailureNotice(string engineName, Color textColor)
        {
            var prefix = (string)Application.Current.Resources["TranslationEngineError"];
            var text = (prefix ?? "Translation failed:") + " " + engineName;

            ShowWindow();

            if (_ChatWindowViewModel.IsHiddenByUser == false)
                _TextArrivedTime = DateTime.UtcNow;

            ShowTranslatedText(text, textColor);

            if (_ChatWindowViewModel.IsHiddenByUser == false)
                _TextArrivedTime = DateTime.UtcNow;
        }

        #endregion

        #region **WindowResize.

        // for each rectangle, assign the following method to its MouseEnter event.
        protected virtual void DisplayResizeCursor(object sender, MouseEventArgs e)
        {
            _WindowResizer.displayResizeCursor(sender);
        }

        protected virtual void DisplayDragCursor(object sender, MouseEventArgs e)
        {
            _WindowResizer.DisplayDragCursor(sender);
        }

        // for each rectangle, assign the following method to its MouseLeave event.
        protected virtual void ResetCursor(object sender, MouseEventArgs e)
        {
            _WindowResizer.resetCursor();
        }

        // for each rectangle, assign the following method to its PreviewMouseDown event.
        protected virtual void Resize(object sender, MouseButtonEventArgs e)
        {
            _WindowResizer.resizeWindow(sender);
        }

        // finally, you may use the following method to enable dragging!
        protected virtual void Drag(object sender, MouseButtonEventArgs e)
        {
            _WindowResizer.dragWindow(sender, e);
        }

        #endregion

        #region **System.

        protected void MakeWindowClickThrough()
        {
            _clickThroughBehavior.MakeClickThrough();
        }

        protected void MakeWindowClickable()
        {
            _clickThroughBehavior.MakeClickable();
        }

        void HideThisWindow_Click(object sender, RoutedEventArgs e)
        {
            _ChatWindowViewModel.IsHiddenByUser = true;

            _ChatWindowViewModel.IsWindowVisible = false;
        }

        void Settings_Click(object sender, RoutedEventArgs e)
        {
            /*
            Helper.Unminimize(_SettigsWindow);

            _SettigsWindow.Visibility = Visibility.Visible;
            _SettigsWindow.Activate();
            _SettigsWindow.Focus();//*/
            _MainWindow.ShowSettingsWindow();
        }

        void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ApplyAlwaysOnTop()
        {
            try
            {
                this.UIThread(() =>
                {
                    bool isAlwaysOnTop = _ChatWindowViewModel.IsAlwaysOnTop ||
                                         _TataruModel.FFMemoryReader.IsGameWindowForeground;
                    this.Topmost = isAlwaysOnTop;

                    var thisHandle = new WindowInteropHelper(this).Handle;
                    if (thisHandle == IntPtr.Zero)
                        return;

                    ApplyTopMostToHandle(thisHandle, isAlwaysOnTop);

                    // WPF can create a hidden owner when ShowInTaskbar=false.
                    // Keep that owner in the same topmost band as this window.
                    var hiddenOwnerHandle = Win32Interfaces.GetWindow(thisHandle, Win32Interfaces.GW_OWNER);
                    if (hiddenOwnerHandle != IntPtr.Zero)
                    {
                        ApplyTopMostToHandle(hiddenOwnerHandle, isAlwaysOnTop);
                    }
                });
            }
            catch (Exception e)
            {
                _Logger.WriteLog(Convert.ToString(e));
            }
        }

        private static void ApplyTopMostToHandle(IntPtr handle, bool isAlwaysOnTop)
        {
            var insertAfter = isAlwaysOnTop
                ? Win32Interfaces.HWND_TOPMOST
                : Win32Interfaces.HWND_NOTOPMOST;

            Win32Interfaces.SetWindowPos(
                handle,
                insertAfter,
                0,
                0,
                0,
                0,
                TopMostSetWindowPosFlags);
        }

        private void ShowWindow()
        {
            if (_ChatWindowViewModel.IsHiddenByUser == false)
            {
                if (_TataruModel.FFMemoryReader.FFWindowState == WindowState.Minimized)
                    return;

                if (!_ChatWindowViewModel.IsAlwaysOnTop &&
                    !_TataruModel.FFMemoryReader.IsGameWindowForeground)
                {
                    return;
                }

                if (this.WindowState == WindowState.Minimized)
                    this.WindowState = WindowState.Normal;

                if (this.Visibility != Visibility.Visible)
                    this.Show();

                ApplyAlwaysOnTop();
            }
        }

        protected virtual void AutoHideStatusCheck()
        {
            var cancellationToken = _lifecycleCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (_ChatWindowViewModel.IsAutoHide)
                        {
                            var ts = DateTime.UtcNow - _TextArrivedTime;
                            if (ts > _ChatWindowViewModel.AutoHideTimeout)
                            {
                                this.UIThread(() =>
                                {
                                    _AutoHidden = true;
                                    _ChatWindowViewModel.IsWindowVisible = false;
                                });
                            }
                            else
                                _AutoHidden = false;
                        }
                        else
                            _AutoHidden = false;

                        await Task.Delay(_SettingsStore.AutoHideWatcherDelayMs, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _Logger.WriteLog("AutoHide watcher failed.");
                    _Logger.WriteLog(ex);
                }
            }, cancellationToken);
        }

        protected override void OnClosed(EventArgs e)
        {
            // The copy is its own window, not part of this one: stopping it
            // here is the only thing that takes its timer and its frame off
            // screen when this window goes.
            CloseDialogueOverlay();
            _dialogueOverlayHost.WantedChanged -= OnDialogueOverlayWantedChanged;
            _dialogueOverlayHost.Release(this);

            _ChatWindowViewModel.AsyncPropertyChanged -= OnSettingsWindowPropertyChange;
            _ChatWindowViewModel.RequestChatClear -= OnChatClearRequest;
            _TataruModel.FFMemoryReader.AsyncPropertyChanged -= OnMemoryReaderPropertyChange;

            _TataruModel.ChatProcessor.TextArrived -= OnTextArrived;
            _TataruModel.FFMemoryReader.FFWindowStateChanged -= OnFFWindowStateChange;

            _lifecycleCts.Cancel();
            _lifecycleCts.Dispose();

            base.OnClosed(e);
        }

        #endregion
    }
}