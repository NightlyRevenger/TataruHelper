// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIVTataruHelper.EventArguments;
using FFXIVTataruHelper.WinUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Data;
using FFXIVTataruHelper.UIModel;
using BondTech.HotKeyManagement.WPF._4;
using System.Windows.Input;
using Translation;

namespace FFXIVTataruHelper.ViewModel
{
    public enum TatruHotkeyType : int
    {
        ShowHideChatWindow = 0,
        ClickThrought = 1,
        ClearChat = 2
    }

    public class ChatWindowViewModel : INotifyPropertyChanged, IDisposable, FFXIVTataruHelper.TataruComponentModel.INotifyPropertyChangedAsync
    {
        #region **Events.
        public event PropertyChangedEventHandler PropertyChanged;

        public event AsyncEventHandler<AsyncPropertyChangedEventArgs> AsyncPropertyChanged
        {
            add { this._AsyncPropertyChanged.Register(value); }
            remove { this._AsyncPropertyChanged.Unregister(value); }
        }
        private AsyncEvent<AsyncPropertyChangedEventArgs> _AsyncPropertyChanged;

        public event AsyncEventHandler<TatruEventArgs> RequestChatClear
        {
            add { this._RequestChatClear.Register(value); }
            remove { this._RequestChatClear.Unregister(value); }
        }
        private AsyncEvent<TatruEventArgs> _RequestChatClear;

        #endregion

        #region **Properties.

        public long WinId
        {
            get { return _WinId; }
        }

        public string Name
        {
            get { return _Name; }
            private set
            {
                if (_Name == value) return;

                _Name = value;
                OnPropertyChanged();
            }
        }

        public double ChatFontSize
        {
            get { return _ChatFontSize; }
            set
            {
                if (_ChatFontSize == value) return;

                _ChatFontSize = value;
                OnPropertyChanged();
            }
        }

        public double LineBreakHeight
        {
            get { return _LineBreakHeight; }
            set
            {
                if (_LineBreakHeight == value) return;

                _LineBreakHeight = value;
                OnPropertyChanged();
            }
        }

        public int SpacingCount
        {
            get { return _SpacingCount; }
            set
            {
                if (_SpacingCount == value) return;

                _SpacingCount = value;
                OnPropertyChanged();
            }
        }

        public FontFamily ChatFont
        {
            get { return _ChatFont; }
            set
            {
                if (_ChatFont == value) return;

                _ChatFont = value;
                OnPropertyChanged();
            }
        }

        public bool IsAlwaysOnTop
        {
            get { return _IsAlwaysOnTop; }
            set
            {
                if (_IsAlwaysOnTop == value) return;

                _IsAlwaysOnTop = value;
                OnPropertyChanged();
            }
        }

        public bool IsClickThrough
        {
            get { return _IsClickThrough; }
            set
            {
                if (_IsClickThrough == value) return;

                _IsClickThrough = value;
                OnPropertyChanged();
            }
        }

        public bool IsAutoHide
        {
            get { return _IsAutoHide; }
            set
            {
                if (_IsAutoHide == value) return;

                _IsAutoHide = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan AutoHideTimeout
        {
            get { return _AutoHideTimeout; }
            set
            {
                if (_AutoHideTimeout == value) return;

                _AutoHideTimeout = value;
                OnPropertyChanged();
                OnPropertyChanged("AutoHideTimeoutSeconds");
            }
        }

        public int AutoHideTimeoutSeconds
        {
            get { return (int)Math.Round(AutoHideTimeout.TotalSeconds); }
            set
            {
                if ((int)Math.Round(AutoHideTimeout.TotalSeconds) == value) return;

                _AutoHideTimeout = new TimeSpan(0, 0, value);
                OnPropertyChanged();
                OnPropertyChanged("AutoHideTimeout");
            }
        }

        public Color BackGroundColor
        {
            get { return _BackGroundColor; }
            set
            {
                if (_BackGroundColor == value) return;

                var tmpColor = value;
                if (tmpColor.A < 1)
                    tmpColor.A = 1;

                _BackGroundColor = tmpColor;
                OnPropertyChanged();
            }
        }

        public System.Drawing.RectangleD ChatWindowRectangle
        {
            get { return _ChatWindowRectangle; }
            set
            {
                if (_ChatWindowRectangle == value) return;

                _ChatWindowRectangle = value;
                OnPropertyChanged();
                OnPropertyChanged("ChatWindowTop");
                OnPropertyChanged("ChatWindowLeft");
                OnPropertyChanged("ChatWindowWidth");
                OnPropertyChanged("ChatWindowHeight");
            }
        }

        public double ChatWindowTop
        {
            get { return ChatWindowRectangle.Y; }
            set
            {
                if (ChatWindowRectangle.Y == value) return;

                var rect = ChatWindowRectangle;
                rect.Y = value;
                ChatWindowRectangle = rect;
            }
        }

        public double ChatWindowLeft
        {
            get { return ChatWindowRectangle.X; }
            set
            {
                if (ChatWindowRectangle.X == value) return;

                var rect = ChatWindowRectangle;
                rect.X = value;
                ChatWindowRectangle = rect;
            }
        }

        public double ChatWindowWidth
        {
            get { return ChatWindowRectangle.Width; }
            set
            {
                if (ChatWindowRectangle.Width == value) return;

                var rect = ChatWindowRectangle;
                rect.Width = value;
                ChatWindowRectangle = rect;
            }
        }

        public double ChatWindowHeight
        {
            get { return ChatWindowRectangle.Height; }
            set
            {
                if (ChatWindowRectangle.Height == value) return;

                var rect = ChatWindowRectangle;
                rect.Height = value;
                ChatWindowRectangle = rect;
            }
        }

        public bool IsHiddenByUser
        {
            get { return _IsHiddenByUser; }
            set
            {
                if (_IsHiddenByUser == value) return;

                _IsHiddenByUser = value;
                OnPropertyChanged();
            }
        }

        public BindingList<ChatCodeViewModel> ChatCodes
        {
            get { return _ChatCodes; }
            set
            {
                if (_ChatCodes != null)
                    _ChatCodes.ListChanged -= OnChatCodesChange;

                _ChatCodes = value;

                _ChatCodes.ListChanged += OnChatCodesChange;
                OnPropertyChanged();
            }
        }

        public HotKeyCombination ShowHideChatKeys
        {
            get { return _ShowHideChatKeys; }
            set
            {
                if (_ShowHideChatKeys == value) return;

                _ShowHideChatKeys = new HotKeyCombination("ShowHideChatKeys" + Convert.ToString(WinId), value);

                ReRegisterGlobalHotekey(_HotKeyManager, ref _ShowHideChat, _ShowHideChatKeys);

                OnPropertyChanged();
            }
        }

        public string ShowHideChatKeysName
        {
            get
            {
                if (ShowHideChatKeys.IsInitialized)
                    return ShowHideChatKeys.CombinationKeysName();
                else
                    return _NotInitializedHKText;
            }
        }

        public HotKeyCombination ClickThoughtChatKeys
        {
            get { return _ClickThoughtChatKeys; }
            set
            {
                if (_ClickThoughtChatKeys == value) return;

                _ClickThoughtChatKeys = new HotKeyCombination("ClickThoughtChatKeys" + Convert.ToString(WinId), value);

                ReRegisterGlobalHotekey(_HotKeyManager, ref _ClickThoughtChat, _ClickThoughtChatKeys);

                OnPropertyChanged();
            }
        }

        public string ClickThoughtChatKeysName
        {
            get
            {
                if (ClickThoughtChatKeys.IsInitialized)
                    return ClickThoughtChatKeys.CombinationKeysName();
                else
                    return _NotInitializedHKText;
            }
        }

        public HotKeyCombination ClearChatKeys
        {
            get { return _ClearChatKeys; }
            set
            {
                if (_ClearChatKeys == value) return;

                _ClearChatKeys = new HotKeyCombination("ClearChatKeys" + Convert.ToString(WinId), value);

                ReRegisterGlobalHotekey(_HotKeyManager, ref _ClearChat, _ClearChatKeys);

                OnPropertyChanged();
            }
        }

        public string ClearChatKeysName
        {
            get
            {
                if (ClearChatKeys.IsInitialized)
                    return ClearChatKeys.CombinationKeysName();
                else
                    return _NotInitializedHKText;
            }
        }

        public CollectionView TranslationEngines
        {
            get { return _TranslationEngines; }
            set
            {

                if (_TranslationEngines != null)
                    _TranslationEngines.CurrentChanged -= OnTranslationEngineChange;

                _TranslationEngines = value;
                _TranslationEngines.CurrentChanged += OnTranslationEngineChange;
                OnPropertyChanged();
            }
        }

        public TranslationEngine CurrentTransaltionEngine
        {
            get { return (TranslationEngine)TranslationEngines.CurrentItem; }
        }

        public CollectionView TranslateFromLanguagues
        {
            get { return _TranslateFromLanguagues; }
            set
            {
                if (_TranslateFromLanguagues == value) return;
                _TranslateFromLanguagues = value;
            }
        }

        public TranslatorLanguague CurrentTranslateFromLanguague
        {
            get { return (TranslatorLanguague)TranslateFromLanguagues.CurrentItem; }
        }

        public CollectionView TranslateToLanguagues
        {
            get { return _TranslateToLanguagues; }
            set
            {
                if (_TranslateToLanguagues == value) return;
                TranslateToLanguagues = value;
            }
        }

        public TranslatorLanguague CurrentTranslateToLanguague
        {
            get { return (TranslatorLanguague)TranslateToLanguagues.CurrentItem; }
        }

        public bool ShowTimestamps
        {
            get => _ShowTimestamps;
            set
            {
                if (_ShowTimestamps == value) return;

                _ShowTimestamps = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get { return _IsSelected; }
            set
            {
                _IsSelected = value;
                OnPropertyChanged();
            }
        }

        public bool IsWindowVisible
        {
            get { return _IsWindowVisible; }
            set
            {
                if (_IsWindowVisible == value) return;

                _IsWindowVisible = value;
                OnPropertyChanged();
            }

        }

        public TataruUICommand ShowChatWindowCommand { get; set; }

        public TataruUICommand RestChatWindowPositionCommand { get; set; }

        #endregion

        #region **LocalVariables.

        string _Name;
        long _WinId;

        double _ChatFontSize;
        double _LineBreakHeight;
        int _SpacingCount;

        FontFamily _ChatFont;

        bool _IsAlwaysOnTop;
        bool _IsClickThrough;
        bool _IsAutoHide;

        TimeSpan _AutoHideTimeout;

        bool _IsHiddenByUser;

        Color _BackGroundColor;

        CollectionView _TranslationEngines;
        CollectionView _TranslateFromLanguagues;
        CollectionView _TranslateToLanguagues;

        System.Drawing.RectangleD _ChatWindowRectangle;

        BindingList<ChatCodeViewModel> _ChatCodes;

        HotKeyCombination _ShowHideChatKeys;
        HotKeyCombination _ClickThoughtChatKeys;
        HotKeyCombination _ClearChatKeys;

        GlobalHotKey _ShowHideChat;
        GlobalHotKey _ClickThoughtChat;
        GlobalHotKey _ClearChat;

        HotKeyManager _HotKeyManager;

        bool _ShowTimestamps;

        bool _IsSelected;

        bool _IsWindowVisible;

        string _NotInitializedHKText = "Empty";

        bool _disposed = false;
        #endregion

        public ChatWindowViewModel()
        {
            this._AsyncPropertyChanged = new AsyncEvent<AsyncPropertyChangedEventArgs>(this.EventErrorHandler, "ChatWindowViewModel \n AsyncPropertyChanged");
            ShowChatWindowCommand = new TataruUICommand(ShowChatWindow);
            RestChatWindowPositionCommand = new TataruUICommand(ResetChatWindowPosition);

            Random rnd = new Random(DateTime.UtcNow.Date.Millisecond);
            _WinId = (DateTime.Now.Ticks * 100u) + ((uint)rnd.Next(0, 100));

            Name = "2";

            ChatFontSize = 1;
            LineBreakHeight = 1;
            SpacingCount = 1;
            BackGroundColor = Color.FromArgb(255, 0, 255, 128);
            ChatWindowRectangle = new System.Drawing.RectangleD(0, 0, 480, 320);

            ChatCodes = new BindingList<ChatCodeViewModel>()
            {
                new ChatCodeViewModel("0039","System", Colors.Aqua, true),
            };
        }

        public ChatWindowViewModel(ChatWindowViewModelSettings settings, List<TranslationEngine> translationEngines, List<ChatMsgType> allChatCodes, HotKeyManager hotKeyManager)
        {
            this._AsyncPropertyChanged = new AsyncEvent<AsyncPropertyChangedEventArgs>(this.EventErrorHandler, "ChatWindowViewModel \n AsyncPropertyChanged");
            this._RequestChatClear = new AsyncEvent<TatruEventArgs>(this.EventErrorHandler, "ChatWindowViewModel \n RequsetChatClear");
            ShowChatWindowCommand = new TataruUICommand(ShowChatWindow);
            RestChatWindowPositionCommand = new TataruUICommand(ResetChatWindowPosition);

            _IsWindowVisible = true;

            Name = settings.Name;
            _WinId = settings.WinId;

            ChatFontSize = settings.ChatFontSize;
            LineBreakHeight = settings.LineBreakHeight;
            SpacingCount = settings.SpacingCount;

            ChatFont = settings.ChatFont;

            IsAlwaysOnTop = settings.IsAlwaysOnTop;
            IsClickThrough = settings.IsClickThrough;
            IsAutoHide = settings.IsAutoHide;

            AutoHideTimeout = settings.AutoHideTimeout;

            IsHiddenByUser = false;

            ShowTimestamps = settings.ShowTimestamps;

            BackGroundColor = settings.BackGroundColor;

            TranslationEngines = new CollectionView(translationEngines);

            var tmpEngine = translationEngines.FirstOrDefault(x => x.EngineName == settings.TranslationEngineName);
            TranslationEngines.MoveCurrentToFirst();
            if (tmpEngine != null)
                if (TranslationEngines.Contains(tmpEngine))
                    TranslationEngines.MoveCurrentTo(tmpEngine);
                else
                    TranslationEngines.MoveCurrentToFirst();

            OnTranslationEngineChange(this, null);

            TrySetLangugue(_TranslateFromLanguagues, settings.FromLanguague);
            TrySetLangugue(_TranslateToLanguagues, settings.ToLanguague);

            ChatWindowRectangle = settings.ChatWindowRectangle;

            ChatCodes = LoadChatCodes(settings.ChatCodes, allChatCodes);

            _HotKeyManager = hotKeyManager;

            ShowHideChatKeys = new HotKeyCombination(settings.ShowHideChatKeys);
            ClickThoughtChatKeys = new HotKeyCombination(settings.ClickThoughtChatKeys);
            ClearChatKeys = new HotKeyCombination(settings.ClearChatKeys);

            _HotKeyManager.GlobalHotKeyPressed += OnGlobalHotKeyPressed;
        }

        public ChatWindowViewModelSettings GetSettings()
        {
            ChatWindowViewModelSettings settings = new ChatWindowViewModelSettings();

            settings.Name = this.Name;
            settings.WinId = this._WinId;

            settings.ChatFontSize = this.ChatFontSize;
            settings.LineBreakHeight = this.LineBreakHeight;
            settings.SpacingCount = this.SpacingCount;

            settings.IsAlwaysOnTop = this.IsAlwaysOnTop;
            settings.IsClickThrough = this.IsClickThrough;
            settings.IsAutoHide = this.IsAutoHide;

            settings.AutoHideTimeout = this.AutoHideTimeout;

            //settings.IsHiddenByUser = this.IsHiddenByUser;

            settings.BackGroundColor = this.BackGroundColor;

            TranslationEngine engine = (TranslationEngine)this.TranslationEngines.CurrentItem;
            if (engine != null)
                settings.TranslationEngineName = engine.EngineName;
            else
                settings.TranslationEngineName = TranslationEngineName.GoogleTranslate;

            settings.FromLanguague = (TranslatorLanguague)TranslateFromLanguagues.CurrentItem;
            settings.ToLanguague = (TranslatorLanguague)TranslateToLanguagues.CurrentItem;

            settings.ChatWindowRectangle = this.ChatWindowRectangle;

            settings.ChatCodes = this.ChatCodes.ToList().Select(entry => new ChatCodeViewModel(entry)).ToList();

            settings.ShowHideChatKeys = new HotKeyCombination(this.ShowHideChatKeys);
            settings.ClickThoughtChatKeys = new HotKeyCombination(this.ClickThoughtChatKeys);
            settings.ClearChatKeys = new HotKeyCombination(this.ClearChatKeys);

            return settings;
        }

        public void RegisterHotKeyDown(TatruHotkeyType type, KeyEventArgs e)
        {
            switch (type)
            {
                case TatruHotkeyType.ShowHideChatWindow:
                    RegisterHotKeyDown(ref _ShowHideChat, _ShowHideChatKeys, e);
                    OnPropertyChanged("ShowHideChatKeys");
                    OnPropertyChanged("ShowHideChatKeysName");
                    break;
                case TatruHotkeyType.ClickThrought:
                    RegisterHotKeyDown(ref _ClickThoughtChat, _ClickThoughtChatKeys, e);
                    OnPropertyChanged("ClickThoughtChatKeys");
                    OnPropertyChanged("ClickThoughtChatKeysName");
                    break;
                case TatruHotkeyType.ClearChat:
                    RegisterHotKeyDown(ref _ClearChat, _ClearChatKeys, e);
                    OnPropertyChanged("ClearChatKeys");
                    OnPropertyChanged("ClearChatKeysName");
                    break;
            }

        }

        public void RegisterHotKeyUp(TatruHotkeyType type, KeyEventArgs e)
        {
            switch (type)
            {
                case TatruHotkeyType.ShowHideChatWindow:
                    RegisterHotKeyUp(ref _ShowHideChat, _ShowHideChatKeys, e);
                    OnPropertyChanged("ShowHideChatKeys");
                    OnPropertyChanged("ShowHideChatKeysName");
                    break;
                case TatruHotkeyType.ClickThrought:
                    RegisterHotKeyUp(ref _ClickThoughtChat, _ClickThoughtChatKeys, e);
                    OnPropertyChanged("ClickThoughtChatKeys");
                    OnPropertyChanged("ClickThoughtChatKeysName");
                    break;
                case TatruHotkeyType.ClearChat:
                    RegisterHotKeyUp(ref _ClearChat, _ClearChatKeys, e);
                    OnPropertyChanged("ClearChatKeys");
                    OnPropertyChanged("ClearChatKeysName");
                    break;
            }
        }

        private void RegisterHotKeyDown(ref GlobalHotKey globalHotKey, HotKeyCombination hotKeyCombination, KeyEventArgs e)
        {
            if (_disposed)
                return;

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

                if (!hotKeyCombination.IsInitialized)
                {
                    if (globalHotKey != null)
                    {
                        _HotKeyManager.RemoveGlobalHotKey(globalHotKey);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }
        }

        private void RegisterHotKeyUp(ref GlobalHotKey globalHotKey, HotKeyCombination hotKeyCombination, KeyEventArgs e)
        {
            if (_disposed)
                return;

            try
            {
                var pressedKeys = Keys.GetPressdKeys();

                pressedKeys = Keys.ClearRepeatedKeys(pressedKeys);

                pressedKeys = Keys.ClearMousKeys(pressedKeys);

                if (pressedKeys.Length == 0)
                {
                    Keyboard.ClearFocus();

                    if (hotKeyCombination.IsInitialized)
                    {

                        if (globalHotKey != null)
                        {
                            _HotKeyManager.RemoveGlobalHotKey(globalHotKey);
                        }

                        var k = Keys.ConvertFromWpfKey(hotKeyCombination.NormalKey);
                        globalHotKey = new GlobalHotKey(hotKeyCombination.Name, hotKeyCombination.ModifierKey, k);

                        _HotKeyManager.AddGlobalHotKey(globalHotKey);

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

        private void ReRegisterGlobalHotekey(HotKeyManager hotKeyManager, ref GlobalHotKey globalHotKey, HotKeyCombination hotKeyCombination)
        {
            if (_disposed)
                return;
            if (globalHotKey != null)
            {
                hotKeyManager.RemoveGlobalHotKey(globalHotKey);
                globalHotKey = null;
            }

            if (hotKeyCombination.IsInitialized)
            {
                var k = Keys.ConvertFromWpfKey(hotKeyCombination.NormalKey);
                globalHotKey = new GlobalHotKey(hotKeyCombination.Name, hotKeyCombination.ModifierKey, k);

                hotKeyManager.AddGlobalHotKey(globalHotKey);
            }
        }

        private void OnTranslationEngineChange(object sender, EventArgs e)
        {
            TranslatorLanguague prevFrom = null, prevTo = null;
            if (TranslateFromLanguagues != null && TranslateToLanguagues != null)
            {
                prevFrom = (TranslatorLanguague)TranslateFromLanguagues.CurrentItem;
                prevTo = (TranslatorLanguague)TranslateToLanguagues.CurrentItem;
            }
            OnPropertyChanged("TranslationEngineSelected");
            OnPropertyChanged("TranslationEngines");

            if (_TranslateFromLanguagues != null)
                _TranslateFromLanguagues.CurrentChanged -= OnTranslateFromLanguageChange;

            if (_TranslateToLanguagues != null)
                _TranslateToLanguagues.CurrentChanged -= OnTranslateToLanguageChange;

            _TranslateFromLanguagues = new CollectionView(((TranslationEngine)_TranslationEngines.CurrentItem).SupportedLanguages.ToList());
            _TranslateFromLanguagues.CurrentChanged += OnTranslateFromLanguageChange;
            OnPropertyChanged("TranslateFromLanguagues");

            List<TranslatorLanguague> supportedToLanguages = ((TranslationEngine)_TranslationEngines.CurrentItem).SupportedLanguages.ToList();
            var lang = supportedToLanguages.FirstOrDefault(x => x.SystemName == "Auto");
            if (lang != null)
                supportedToLanguages.Remove(lang);

            _TranslateToLanguagues = new CollectionView(supportedToLanguages.ToList());
            _TranslateToLanguagues.CurrentChanged += OnTranslateToLanguageChange;
            OnPropertyChanged("TranslateToLanguagues");

            if (prevFrom != null && prevTo != null)
            {
                TrySetLangugue(_TranslateFromLanguagues, prevFrom);
                TrySetLangugue(_TranslateToLanguagues, prevTo);
            }
        }

        private void OnTranslateFromLanguageChange(object sender, EventArgs e)
        {
            OnPropertyChanged("TranslateFromLanguagues");
        }

        private void OnTranslateToLanguageChange(object sender, EventArgs e)
        {
            OnPropertyChanged("TranslateToLanguagues");
        }

        private void OnChatCodesChange(object sender, ListChangedEventArgs e)
        {
            OnPropertyChanged("ChatCodes");
        }

        private void OnGlobalHotKeyPressed(object sender, GlobalHotKeyEventArgs e)
        {
            if (ShowHideChatKeys != null)
            {
                if (e.HotKey.Name == ShowHideChatKeys.Name)
                {
                    if (this.IsWindowVisible)
                        IsHiddenByUser = true;
                    else
                        IsHiddenByUser = false;

                    IsWindowVisible = !IsWindowVisible;
                }
            }
            if (ClickThoughtChatKeys != null)
            {
                if (e.HotKey.Name == ClickThoughtChatKeys.Name)
                {
                    IsClickThrough = !IsClickThrough;
                }
            }
            if (ClearChatKeys != null)
            {
                if (e.HotKey.Name == ClearChatKeys.Name)
                {
                    _RequestChatClear.InvokeAsync(new TatruEventArgs(this));
                }
            }
        }

        private BindingList<ChatCodeViewModel> LoadChatCodes(List<ChatCodeViewModel> UserChatCodes, List<ChatMsgType> allChatCodes)
        {
            List<ChatMsgType> chatCodes = allChatCodes.Select(entry => new ChatMsgType(entry)).ToList();
            List<ChatCodeViewModel> chatCodesViewMode = new List<ChatCodeViewModel>();

            foreach (var code in allChatCodes)
            {
                bool isCheked = (code.MsgType == MsgType.Translate) ? true : false;
                chatCodesViewMode.Add(new ChatCodeViewModel(code.ChatCode, code.Name, code.Color, isCheked));
            }

            foreach (var userCode in UserChatCodes)
            {
                var code = chatCodesViewMode.FirstOrDefault(x => x.Equals(userCode));
                if (code != null)
                {
                    code.IsChecked = userCode.IsChecked;
                    code.Color = userCode.Color;
                }
            }

            return new BindingList<ChatCodeViewModel>(chatCodesViewMode);
        }

        private void TrySetLangugue(CollectionView collection, TranslatorLanguague languague)
        {
            if (languague == null)
                collection.MoveCurrentToFirst();
            else
            {
                if (collection.Contains(languague))
                    collection.MoveCurrentTo(languague);
                else
                    collection.MoveCurrentToFirst();
            }
        }

        private void ShowChatWindow()
        {
            if (this.IsWindowVisible)
                IsHiddenByUser = true;
            else
                IsHiddenByUser = false;

            this.IsWindowVisible = !this.IsWindowVisible;
        }

        private void ResetChatWindowPosition()
        {
            var rect = ChatWindowRectangle;
            rect.X = 0;
            rect.Y = 0;

            ChatWindowRectangle = rect;
        }

        public override string ToString()
        {
            return Name;
        }

        private void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            var ea = new AsyncPropertyChangedEventArgs(this, prop);
            _AsyncPropertyChanged.InvokeAsync(ea).Forget();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            Logger.WriteLog(text);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        if (_ShowHideChat != null)
                            _HotKeyManager.RemoveGlobalHotKey(_ShowHideChat);

                        if (_ClickThoughtChat != null)
                            _HotKeyManager.RemoveGlobalHotKey(_ClickThoughtChat);

                        if (_ClearChat != null)
                            _HotKeyManager.RemoveGlobalHotKey(_ClearChat);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(e);
                    }
                }

                // Indicate that the instance has been disposed.
                _disposed = true;
            }
        }
    }
}
