using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using FFXIVTataruHelper.EventArguments;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.Settings;
using FFXIVTataruHelper.Services.UI;
using FFXIVTataruHelper.TataruComponentModel;

namespace FFXIVTataruHelper
{
    public class TataruUIModel : INotifyPropertyChanged
    {
        #region **Events.

        public event AsyncEventHandler<AsyncListChangedEventHandler<ChatWindowViewModelSettings>>
            ChatWindowsListChangedAsync
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

                var ea = new BooleanChangeEventArgs(this) { OldValue = oldValue, NewValue = value };

                _IsHideSettingsToTrayChanged.InvokeAsync(ea).EndWith(() => { NotifyPropertyChanged(); });
            }
        }

        /// <summary>
        /// Reads NPC dialogue straight from the game's UI instead of waiting for it
        /// to reach the chat log.
        ///
        /// While on, the chat-log copies of those same lines are dropped. Without
        /// that every line was translated twice - once live and once on click-through
        /// - and the only workaround was unticking the NPC chat codes by hand.
        /// </summary>
        public bool IsRealtimeTranslation
        {
            get { return _IsRealtimeTranslation; }
            set
            {
                _IsRealtimeTranslation = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Whether a line the game's own translators have already rendered by
        /// hand is used in place of asking a translation service for it.
        ///
        /// Off, the application is a translator and nothing else. On, dialogue
        /// that appears in the xivrus translation is shown as written there -
        /// instantly, and closer to what the writers meant, since it is made
        /// from the Japanese rather than the English adaptation.
        /// </summary>
        public bool IsLiteraryTranslation
        {
            get { return _IsLiteraryTranslation; }
            set
            {
                _IsLiteraryTranslation = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>

        /// Whether a line an engine translated carries a mark saying so. Off, there

        /// is no telling the two apart by reading them.

        /// </summary>

        public bool IsMachineTranslationMarked

        {

            get { return _IsMachineTranslationMarked; }

            set

            {

                _IsMachineTranslationMarked = value;

                NotifyPropertyChanged();

            }

        }


        /// <summary>


        /// Whether the speaker's name is translated as well as the line, so a


        /// message does not change alphabet halfway through.


        /// </summary>


        public bool IsSpeakerNameTranslated


        {


            get { return _IsSpeakerNameTranslated; }


            set


            {


                _IsSpeakerNameTranslated = value;


                NotifyPropertyChanged();


            }


        }

        /// <summary>Whether player-chat sender prefixes are translated into the reading language.</summary>
        public bool IsPlayerNicknameTranslated
        {
            get { return _IsPlayerNicknameTranslated; }
            set
            {
                _IsPlayerNicknameTranslated = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Whether new hand-made translations are fetched as they appear.
        ///
        /// The check for them happens either way, and costs a few kilobytes a
        /// day. This says what to do with the answer: fetch around a gigabyte,
        /// or say that there is something to fetch and leave it there.
        /// </summary>
        public bool IsReferenceIndexAutoInstall
        {
            get { return _IsReferenceIndexAutoInstall; }
            set
            {
                _IsReferenceIndexAutoInstall = value;
                NotifyPropertyChanged();
            }
        }



        public PointD SettingsWindowSize
        {
            get { return _SettingsWindowSize; }
            set
            {
                var oldValue = _SettingsWindowSize;
                _SettingsWindowSize = value;

                var ea = new PointDValueChangeEventArgs(this) { OldValue = oldValue, NewValue = value };

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

                var ea = new IntegerValueChangeEventArgs(this) { OldValue = oldValue, NewValue = value };

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

        bool _IsRealtimeTranslation = true;


        bool _IsLiteraryTranslation = true;



        bool _IsMachineTranslationMarked;




        bool _IsSpeakerNameTranslated;

        bool _IsPlayerNicknameTranslated;

        bool _IsReferenceIndexAutoInstall;

        PointD _SettingsWindowSize = new PointD(0.0, 0.0);

        AsyncBindingList<ChatWindowViewModelSettings> _ChatWindows;

        int _IsFirstTime;

        int _UiLanguage;

        private readonly IUiDispatcher _uiDispatcher;
        private readonly IAppLogger _logger;

        #endregion

        public TataruUIModel(ISettingsStore settingsStore, IUiDispatcher uiDispatcher, IAppLogger logger)
        {
            _uiDispatcher = uiDispatcher;
            _logger = logger;

            this._ChatWindowsListChangedAsync =
                new AsyncEvent<AsyncListChangedEventHandler<ChatWindowViewModelSettings>>(this.EventErrorHandler,
                    "TataruUIModel \n ChatWindowsListChangedAsync");

            this._AsyncPropertyChanged =
                new AsyncEvent<AsyncPropertyChangedEventArgs>(this.EventErrorHandler, "AsyncPropertyChanged");

            this._IsHideSettingsToTrayChanged =
                new AsyncEvent<BooleanChangeEventArgs>(this.EventErrorHandler, "IsHideSettingsToTrayChanged");

            this._SettingsWindowSizeChanged =
                new AsyncEvent<PointDValueChangeEventArgs>(this.EventErrorHandler, "SettingsWindowSizeChanged");

            this._UiLanguageChanged =
                new AsyncEvent<IntegerValueChangeEventArgs>(this.EventErrorHandler, "UiLanguageChanged");

            this.ChatWindows = new AsyncBindingList<ChatWindowViewModelSettings>(_logger);
        }

        /// <summary>
        /// Whether the saved settings have been read in. Nothing that invents a
        /// window may run before this is true.
        /// </summary>
        public bool AreSettingsLoaded
        {
            get { return _AreSettingsLoaded; }
            private set
            {
                _AreSettingsLoaded = value;
                NotifyPropertyChanged();
            }
        }

        bool _AreSettingsLoaded;

        public void SetSettings(UserSettings userSettings)
        {
            UiLanguage = userSettings.CurentUILanguague;

            IsHideSettingsToTray = userSettings.IsHideToTray;

            IsRealtimeTranslation = userSettings.IsDirecMemoryReading;

            IsLiteraryTranslation = userSettings.IsLiteraryTranslation;

            IsMachineTranslationMarked = userSettings.IsMachineTranslationMarked;

            IsSpeakerNameTranslated = userSettings.IsSpeakerNameTranslated;

            IsPlayerNicknameTranslated = userSettings.IsPlayerNicknameTranslated;

            IsReferenceIndexAutoInstall = userSettings.IsReferenceIndexAutoInstall;

            SettingsWindowSize = userSettings.SettingsWindowSize;

            var tmpChatWindows = new List<ChatWindowViewModelSettings>(userSettings.ChatWindows);

            _uiDispatcher.Invoke(() =>
            {
                foreach (var win in tmpChatWindows)
                {
                    ChatWindows.Add(win);
                }
            });

            IsFirstTime = userSettings.IsFirstTime;

            // Last, and after the windows are in: until this is set, nothing
            // may add a window of its own. Settings arrive on a background
            // thread, and a window added before they land takes a number the
            // settings are about to use - after which the real window is
            // dropped as already present, and the impostor cannot be selected
            // or deleted, because both searches find it first.
            AreSettingsLoaded = true;
        }

        public UserSettings GetSettings()
        {
            UserSettings userSettings = new UserSettings();

            userSettings.CurentUILanguague = this.UiLanguage;

            userSettings.IsHideToTray = this.IsHideSettingsToTray;

            userSettings.IsDirecMemoryReading = this.IsRealtimeTranslation;

            userSettings.IsLiteraryTranslation = this.IsLiteraryTranslation;

            userSettings.IsMachineTranslationMarked = this.IsMachineTranslationMarked;

            userSettings.IsSpeakerNameTranslated = this.IsSpeakerNameTranslated;

            userSettings.IsPlayerNicknameTranslated = this.IsPlayerNicknameTranslated;

            userSettings.IsReferenceIndexAutoInstall = this.IsReferenceIndexAutoInstall;

            userSettings.SettingsWindowSize = this.SettingsWindowSize;

            userSettings.ChatWindows = this.ChatWindows.ToList()
                .Select(element => new ChatWindowViewModelSettings(element)).ToList();

            userSettings.IsFirstTime = IsFirstTime;

            return userSettings;
        }

        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            _logger.WriteLog(text);
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
