// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIVTataruHelper.EventArguments;
using FFXIVTataruHelper.WinUtils;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FFXIVTataruHelper.UIModel;
using FFXIVTataruHelper.ViewModel;
using FFXIVTataruHelper.TataruComponentModel;

namespace FFXIVTataruHelper
{
    public class TataruUIModel : INotifyPropertyChanged
    {

        #region **Events.

        public event AsyncEventHandler<AsyncListChangedEventHandler<ChatWindowViewModelSettings>> ChatWindowsListChangedAsync
        {
            add { this._ChatWindowsListChangedAsync.Register(value); }
            remove { this._ChatWindowsListChangedAsync.Unregister(value); }
        }
        private AsyncEvent<AsyncListChangedEventHandler<ChatWindowViewModelSettings>> _ChatWindowsListChangedAsync;

        public event PropertyChangedEventHandler PropertyChanged;

        public event AsyncEventHandler<AsyncPropertyChangedEventArgs> AsyncPropertyChanged
        {
            add { this._AsyncPropertyChanged.Register(value); }
            remove { this._AsyncPropertyChanged.Unregister(value); }
        }
        private AsyncEvent<AsyncPropertyChangedEventArgs> _AsyncPropertyChanged;

        public event AsyncEventHandler<BooleanChangeEventArgs> IsHideSettingsToTrayChanged
        {
            add { this._IsHideSettingsToTrayChanged.Register(value); }
            remove { this._IsHideSettingsToTrayChanged.Unregister(value); }
        }
        private AsyncEvent<BooleanChangeEventArgs> _IsHideSettingsToTrayChanged;

        public event AsyncEventHandler<BooleanChangeEventArgs> IsDirecMemoryReadingChanged
        {
            add { this._IsDirecMemoryReadingChanged.Register(value); }
            remove { this._IsDirecMemoryReadingChanged.Unregister(value); }
        }
        private AsyncEvent<BooleanChangeEventArgs> _IsDirecMemoryReadingChanged;

        public event AsyncEventHandler<PointDValueChangeEventArgs> SettingsWindowSizeChanged
        {
            add { this._SettingsWindowSizeChanged.Register(value); }
            remove { this._SettingsWindowSizeChanged.Unregister(value); }
        }
        private AsyncEvent<PointDValueChangeEventArgs> _SettingsWindowSizeChanged;

        public event AsyncEventHandler<IntegerValueChangeEventArgs> UiLanguageChanged
        {
            add { this._UiLanguageChanged.Register(value); }
            remove { this._UiLanguageChanged.Unregister(value); }
        }
        private AsyncEvent<IntegerValueChangeEventArgs> _UiLanguageChanged;

        #endregion

        #region **Properties.

        public bool IsHideSettingsToTray
        {
            get { return _IsHideSettingsToTray; }
            set
            {
                var oldValue = _IsHideSettingsToTray;
                _IsHideSettingsToTray = value;

                var ea = new BooleanChangeEventArgs(this)
                {
                    OldValue = oldValue,
                    NewValue = value
                };

                _IsHideSettingsToTrayChanged.InvokeAsync(ea).EndWith(() => { NotifyPropertyChanged(); });
            }
        }

        public bool IsDirecMemoryReading
        {
            get { return _IsDirecMemoryReading; }
            set
            {
                var oldValue = _IsDirecMemoryReading;
                _IsDirecMemoryReading = value;

                var ea = new BooleanChangeEventArgs(this)
                {
                    OldValue = oldValue,
                    NewValue = value
                };

                _IsDirecMemoryReadingChanged.InvokeAsync(ea).EndWith(() => { NotifyPropertyChanged(); });
            }
        }

        public System.Drawing.PointD SettingsWindowSize
        {
            get { return _SettingsWindowSize; }
            set
            {
                var oldValue = _SettingsWindowSize;
                _SettingsWindowSize = value;

                var ea = new PointDValueChangeEventArgs(this)
                {
                    OldValue = oldValue,
                    NewValue = value
                };

                _SettingsWindowSizeChanged.InvokeAsync(ea).EndWith(() => { NotifyPropertyChanged(); });
            }
        }

        public int IsFirstTime
        {
            get { return _IsFirstTime; }
            set
            {
                _IsFirstTime = value;

                Task.Run(() => NotifyPropertyChanged());
            }
        }

        public int UiLanguage
        {
            get { return _UiLanguage; }
            set
            {
                var oldValue = _UiLanguage;
                _UiLanguage = value;

                var ea = new IntegerValueChangeEventArgs(this)
                {
                    OldValue = oldValue,
                    NewValue = value
                };

                _UiLanguageChanged.InvokeAsync(ea).EndWith(() => { NotifyPropertyChanged(); });

            }
        }

        public AsyncBindingList<ChatWindowViewModelSettings> ChatWindows
        {
            get
            {
                return _ChatWindows;
            }
            set
            {
                if (_ChatWindows != null)
                    _ChatWindows.AsyncListChanged -= OnChatWindowsChangeAsync;

                _ChatWindows = value;
                _ChatWindows.AsyncListChanged += OnChatWindowsChangeAsync;

                NotifyPropertyChanged();
            }
        }

        #endregion

        #region **LocalVariables.

        bool _IsHideSettingsToTray;

        bool _IsDirecMemoryReading;

        System.Drawing.PointD _SettingsWindowSize = new System.Drawing.PointD(0.0, 0.0);

        AsyncBindingList<ChatWindowViewModelSettings> _ChatWindows;

        int _IsFirstTime;

        int _UiLanguage;

        #endregion

        public TataruUIModel()
        {
            this._ChatWindowsListChangedAsync = new AsyncEvent<AsyncListChangedEventHandler<ChatWindowViewModelSettings>>(this.EventErrorHandler, "TataruUIModel \n ChatWindowsListChangedAsync");

            this._AsyncPropertyChanged = new AsyncEvent<AsyncPropertyChangedEventArgs>(this.EventErrorHandler, "AsyncPropertyChanged");

            this._IsHideSettingsToTrayChanged = new AsyncEvent<BooleanChangeEventArgs>(this.EventErrorHandler, "IsHideSettingsToTrayChanged");

            this._IsDirecMemoryReadingChanged = new AsyncEvent<BooleanChangeEventArgs>(this.EventErrorHandler, "IsDirecMemoryReadingChanged");

            this._SettingsWindowSizeChanged = new AsyncEvent<PointDValueChangeEventArgs>(this.EventErrorHandler, "SettingsWindowSizeChanged");

            this._UiLanguageChanged = new AsyncEvent<IntegerValueChangeEventArgs>(this.EventErrorHandler, "UiLanguageChanged");

            this.ChatWindows = new AsyncBindingList<ChatWindowViewModelSettings>();
        }

        public void SetSettings(UserSettings userSettings)
        {
            UserSettings tmpSettings = new UserSettings();

            UiLanguage = userSettings.CurentUILanguague;

            IsHideSettingsToTray = userSettings.IsHideToTray;

            IsDirecMemoryReading = userSettings.IsDirecMemoryReading;

            SettingsWindowSize = userSettings.SettingsWindowSize;

            var tmpChatList = Helper.LoadJsonData<List<ChatMsgType>>(GlobalSettings.ChatCodesFilePath);

            var tmpChatWindows = new List<ChatWindowViewModelSettings>(userSettings.ChatWindows);

            UiWindow.Window.UIThread(() =>
            {
                foreach (var win in tmpChatWindows)
                {
                    ChatWindows.Add(win);
                }
            });

            IsFirstTime = userSettings.IsFirstTime;
        }

        public UserSettings GetSettings()
        {
            UserSettings userSettings = new UserSettings();

            userSettings.CurentUILanguague = this.UiLanguage;

            userSettings.IsHideToTray = this.IsHideSettingsToTray;

            userSettings.IsDirecMemoryReading = this.IsDirecMemoryReading;

            userSettings.SettingsWindowSize = this.SettingsWindowSize;

            userSettings.ChatWindows = this.ChatWindows.ToList().Select(element => new ChatWindowViewModelSettings(element)).ToList();

            userSettings.IsFirstTime = IsFirstTime;

            return userSettings;
        }

        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            Logger.WriteLog(text);
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            _AsyncPropertyChanged.InvokeAsync(new AsyncPropertyChangedEventArgs(this, propertyName)).Forget();
        }

        private async Task OnChatWindowsChangeAsync(AsyncListChangedEventHandler<ChatWindowViewModelSettings> e)
        {
            NotifyPropertyChanged("ChatWindows." + e.ChangedEventArgs.ToString());

            await _ChatWindowsListChangedAsync.InvokeAsync(e);
        }

    }
}
