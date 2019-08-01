// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIITataruHelper.EventArguments;
using FFXIITataruHelper.WinUtils;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FFXIITataruHelper.Translation.WebTranslator;
using System.Collections.ObjectModel;
using FFXIITataruHelper.Translation;

namespace FFXIITataruHelper
{
    public class TataruUIModel
    {

        #region **Events.

        public event AsyncEventHandler<IntegerValueChangeEventArgs> ChatFontSizeChanged
        {
            add { this._ChatFontSizeChanged.Register(value); }
            remove { this._ChatFontSizeChanged.Unregister(value); }
        }
        private AsyncEvent<IntegerValueChangeEventArgs> _ChatFontSizeChanged;

        public event AsyncEventHandler<ColorChangeEventArgs> BackgroundColorChanged
        {
            add { this._BackgroundColorChanged.Register(value); }
            remove { this._BackgroundColorChanged.Unregister(value); }
        }
        private AsyncEvent<ColorChangeEventArgs> _BackgroundColorChanged;

        public event AsyncEventHandler<ColorListChangeEventArgs> ColorListChanged
        {
            add { this._ColorListChanged.Register(value); }
            remove { this._ColorListChanged.Unregister(value); }
        }
        private AsyncEvent<ColorListChangeEventArgs> _ColorListChanged;

        public event AsyncEventHandler<IntegerValueChangeEventArgs> ParagraphSpaceCountChanged
        {
            add { this._ParagraphSpaceCountChanged.Register(value); }
            remove { this._ParagraphSpaceCountChanged.Unregister(value); }
        }
        private AsyncEvent<IntegerValueChangeEventArgs> _ParagraphSpaceCountChanged;

        public event AsyncEventHandler<IntegerValueChangeEventArgs> LineBreakeHeightChanged
        {
            add { this._LineBreakeHeightChanged.Register(value); }
            remove { this._LineBreakeHeightChanged.Unregister(value); }
        }
        private AsyncEvent<IntegerValueChangeEventArgs> _LineBreakeHeightChanged;

        public event AsyncEventHandler<TranslationEngineChangeEventArgs> TranslationEngineChanged
        {
            add { this._TranslationEngineChanged.Register(value); }
            remove { this._TranslationEngineChanged.Unregister(value); }
        }
        private AsyncEvent<TranslationEngineChangeEventArgs> _TranslationEngineChanged;

        public event AsyncEventHandler<StringValueChangeEventArgs> FFLanguageChanged
        {
            add { this._FFLanguageChanged.Register(value); }
            remove { this._FFLanguageChanged.Unregister(value); }
        }
        private AsyncEvent<StringValueChangeEventArgs> _FFLanguageChanged;

        public event AsyncEventHandler<StringValueChangeEventArgs> TranslateToLanguageChanged
        {
            add { this._TranslateToLanguageChanged.Register(value); }
            remove { this._TranslateToLanguageChanged.Unregister(value); }
        }
        private AsyncEvent<StringValueChangeEventArgs> _TranslateToLanguageChanged;

        public event AsyncEventHandler<HotKeyCombinationChangeEventArgs> ShowHideChatCombinationChanged
        {
            add { this._ShowHideChatCombinationChanged.Register(value); }
            remove { this._ShowHideChatCombinationChanged.Unregister(value); }
        }
        private AsyncEvent<HotKeyCombinationChangeEventArgs> _ShowHideChatCombinationChanged;

        public event AsyncEventHandler<HotKeyCombinationChangeEventArgs> ClickThoughtChatCombinationChanged
        {
            add { this._ClickThoughtChatCombinationChanged.Register(value); }
            remove { this._ClickThoughtChatCombinationChanged.Unregister(value); }
        }
        private AsyncEvent<HotKeyCombinationChangeEventArgs> _ClickThoughtChatCombinationChanged;

        public event AsyncEventHandler<HotKeyCombinationChangeEventArgs> ClearChatCombinationChanged
        {
            add { this._ClearChatCombinationChanged.Register(value); }
            remove { this._ClearChatCombinationChanged.Unregister(value); }
        }
        private AsyncEvent<HotKeyCombinationChangeEventArgs> _ClearChatCombinationChanged;

        public event AsyncEventHandler<BooleanChangeEventArgs> IsChatClickThroughChanged
        {
            add { this._IsChatClickThroughChanged.Register(value); }
            remove { this._IsChatClickThroughChanged.Unregister(value); }
        }
        private AsyncEvent<BooleanChangeEventArgs> _IsChatClickThroughChanged;

        public event AsyncEventHandler<BooleanChangeEventArgs> IsChatAlwaysOnTopChanged
        {
            add { this._IsChatAlwaysOnTopChanged.Register(value); }
            remove { this._IsChatAlwaysOnTopChanged.Unregister(value); }
        }
        private AsyncEvent<BooleanChangeEventArgs> _IsChatAlwaysOnTopChanged;

        public event AsyncEventHandler<BooleanChangeEventArgs> IsHideSettingsToTrayChanged
        {
            add { this._IsHideSettingsToTrayChanged.Register(value); }
            remove { this._IsHideSettingsToTrayChanged.Unregister(value); }
        }
        private AsyncEvent<BooleanChangeEventArgs> _IsHideSettingsToTrayChanged;

        public event AsyncEventHandler<BooleanChangeEventArgs> IsAutoHideChanged
        {
            add { this._IsAutoHideChanged.Register(value); }
            remove { this._IsAutoHideChanged.Unregister(value); }
        }
        private AsyncEvent<BooleanChangeEventArgs> _IsAutoHideChanged;

        public event AsyncEventHandler<TimeSpanChangeEventArgs> AutoHideTimeoutChanged
        {
            add { this._AutoHideTimeoutChanged.Register(value); }
            remove { this._AutoHideTimeoutChanged.Unregister(value); }
        }
        private AsyncEvent<TimeSpanChangeEventArgs> _AutoHideTimeoutChanged;

        public event AsyncEventHandler<PointDValueChangeEventArgs> SettingsWindowSizeChanged
        {
            add { this._SettingsWindowSizeChanged.Register(value); }
            remove { this._SettingsWindowSizeChanged.Unregister(value); }
        }
        private AsyncEvent<PointDValueChangeEventArgs> _SettingsWindowSizeChanged;

        public event AsyncEventHandler<RectangleDValueChangeEventArgs> ChatWindowRectangleChanged
        {
            add { this._ChatWindowRectangleChanged.Register(value); }
            remove { this._ChatWindowRectangleChanged.Unregister(value); }
        }
        private AsyncEvent<RectangleDValueChangeEventArgs> _ChatWindowRectangleChanged;

        public event AsyncEventHandler<ChatMsgTypeChangeEventArgs> ChatCodesChanged
        {
            add { this._ChatCodesChanged.Register(value); }
            remove { this._ChatCodesChanged.Unregister(value); }
        }
        private AsyncEvent<ChatMsgTypeChangeEventArgs> _ChatCodesChanged;

        public event AsyncEventHandler<IntegerValueChangeEventArgs> UiLanguageChanged
        {
            add { this._UiLanguageChanged.Register(value); }
            remove { this._UiLanguageChanged.Unregister(value); }
        }
        private AsyncEvent<IntegerValueChangeEventArgs> _UiLanguageChanged;

        #endregion

        #region **Properties.

        public int ChatFontSize
        {
            get { return _ChatFontSize; }
            set
            {
                if (value < 1)
                    value = 1;

                var oldSize = _ChatFontSize;
                _ChatFontSize = value;

                var ea = new IntegerValueChangeEventArgs(this)
                {
                    OldValue = oldSize,
                    NewValue = value
                };
                _ChatFontSizeChanged.InvokeAsync(ea);
            }
        }

        public Color BackgroundColor
        {
            get { return _BackgroundColor; }

            set
            {
                var oldColor = _BackgroundColor;
                _BackgroundColor = value;

                var ea = new ColorChangeEventArgs(this)
                {
                    OldColor = oldColor,
                    NewColor = value,
                    ColorId = 0
                };
                _BackgroundColorChanged.InvokeAsync(ea);
            }
        }

        public List<Color> RecentBackgroundColors
        {
            get { return _RecentBackgroundColors; }
            set
            {
                _RecentBackgroundColors = value;

                var ea = new ColorListChangeEventArgs(this)
                {
                    Colors = _RecentBackgroundColors,
                    ColorsId = 0
                };
                _ColorListChanged.InvokeAsync(ea);
            }
        }

        public int ParagraphSpaceCount
        {
            get { return _ParagraphSpaceCount; }

            set
            {
                if (value < 0)
                    value = 0;

                var oldSize = _ParagraphSpaceCount;
                _ParagraphSpaceCount = value;

                var ea = new IntegerValueChangeEventArgs(this)
                {
                    OldValue = oldSize,
                    NewValue = value
                };
                _ParagraphSpaceCountChanged.InvokeAsync(ea);
            }
        }

        public int LineBreakeHeight
        {
            get { return _LineBreakeHeight; }
            set
            {
                if (value < 0)
                    value = 0;

                var oldSize = _LineBreakeHeight;
                _LineBreakeHeight = value;

                var ea = new IntegerValueChangeEventArgs(this)
                {
                    OldValue = oldSize,
                    NewValue = value
                };
                _LineBreakeHeightChanged.InvokeAsync(ea);
            }
        }

        public TranslationEngine TranslationEngine
        {
            get { return _TranslationEngine; }
            set
            {
                var oldSize = (int)_TranslationEngine;
                _TranslationEngine = value;

                ReadOnlyCollection<TranslatorLanguague> supportedLanguages = null;
                if ((int)value < _TranslatorsLanguages.Count)
                {
                    supportedLanguages = _TranslatorsLanguages[(int)value];
                }

                //var supportedLanguages
                var ea = new TranslationEngineChangeEventArgs(this)
                {
                    OldEngine = oldSize,
                    NewEngine = (int)value,
                    SupportedLanguages = supportedLanguages
                };
                _TranslationEngineChanged.InvokeAsync(ea);
            }
        }

        public string FFLanguage
        {
            get { return _FFLanguage; }
            set
            {
                var oldValue = _FFLanguage;
                _FFLanguage = value;

                var ea = new StringValueChangeEventArgs(this)
                {
                    OldString = oldValue,
                    NewString = value
                };
                _FFLanguageChanged.InvokeAsync(ea);
            }
        }

        public string TranslateToLanguage
        {
            get { return _TranslateToLanguage; }

            set
            {
                var oldValue = _TranslateToLanguage;
                _TranslateToLanguage = value;

                var ea = new StringValueChangeEventArgs(this)
                {
                    OldString = oldValue,
                    NewString = value
                };
                _TranslateToLanguageChanged.InvokeAsync(ea);
            }
        }

        public HotKeyCombination ShowHideChatKeys
        {
            get { return _ShowHideChatKeys; }
            set
            {
                HotKeyCombination oldValue = null;
                if (_ShowHideChatKeys != null)
                    oldValue = new HotKeyCombination(_ShowHideChatKeys);
                else
                    oldValue = new HotKeyCombination();

                _ShowHideChatKeys = value;

                var ea = new HotKeyCombinationChangeEventArgs(this)
                {
                    OldHotKeyCombination = oldValue,
                    NewHotKeyCombination = value
                };

                _ShowHideChatCombinationChanged.InvokeAsync(ea);
            }
        }

        public HotKeyCombination ClickThoughtChatKeys
        {
            get { return _ClickThoughtChatKeys; }
            set
            {
                HotKeyCombination oldValue = null;
                if (_ClickThoughtChatKeys != null)
                    oldValue = new HotKeyCombination(_ClickThoughtChatKeys);
                else
                    oldValue = new HotKeyCombination();

                _ClickThoughtChatKeys = value;

                var ea = new HotKeyCombinationChangeEventArgs(this)
                {
                    OldHotKeyCombination = oldValue,
                    NewHotKeyCombination = value
                };

                _ClickThoughtChatCombinationChanged.InvokeAsync(ea);
            }
        }

        public HotKeyCombination ClearChatKeys
        {
            get { return _ClearChatKeys; }
            set
            {
                HotKeyCombination oldValue = null;
                if (_ClearChatKeys != null)
                    oldValue = new HotKeyCombination(_ClearChatKeys);
                else
                    oldValue = new HotKeyCombination();

                _ClearChatKeys = value;

                var ea = new HotKeyCombinationChangeEventArgs(this)
                {
                    OldHotKeyCombination = oldValue,
                    NewHotKeyCombination = value
                };

                _ClearChatCombinationChanged.InvokeAsync(ea);
            }
        }

        public bool IsChatClickThrough
        {
            get { return _IsChatClickThrough; }
            set
            {
                var oldValue = _IsChatClickThrough;
                _IsChatClickThrough = value;

                var ea = new BooleanChangeEventArgs(this)
                {
                    OldValue = oldValue,
                    NewValue = value
                };

                _IsChatClickThroughChanged.InvokeAsync(ea);
            }
        }

        public bool IsChatAlwaysOnTop
        {
            get { return _IsChatAlwaysOnTop; }
            set
            {
                var oldValue = _IsChatAlwaysOnTop;
                _IsChatAlwaysOnTop = value;

                var ea = new BooleanChangeEventArgs(this)
                {
                    OldValue = oldValue,
                    NewValue = value
                };

                _IsChatAlwaysOnTopChanged.InvokeAsync(ea);
            }
        }

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

                _IsHideSettingsToTrayChanged.InvokeAsync(ea);
            }
        }

        public bool IsAutoHide
        {
            get { return _IsAutoHide; }
            set
            {
                var oldValue = _IsAutoHide;
                _IsAutoHide = value;

                var ea = new BooleanChangeEventArgs(this)
                {
                    OldValue = oldValue,
                    NewValue = value
                };

                _IsAutoHideChanged.InvokeAsync(ea);
            }
        }

        public TimeSpan AutoHideTimeout
        {
            get { return _AutoHideTimeout; }

            set
            {
                if (value.TotalSeconds < 10)
                    value = new TimeSpan(0, 0, 10);

                var oldValue = _AutoHideTimeout;
                _AutoHideTimeout = value;

                var ea = new TimeSpanChangeEventArgs(this)
                {
                    OldValue = oldValue,
                    NewValue = value
                };

                _AutoHideTimeoutChanged.InvokeAsync(ea);
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

                _SettingsWindowSizeChanged.InvokeAsync(ea);
            }
        }

        public System.Drawing.RectangleD ChatWindowRectangle
        {
            get { return _ChatWindowRectangle; }
            set
            {
                var oldValue = _ChatWindowRectangle;
                _ChatWindowRectangle = value;

                var ea = new RectangleDValueChangeEventArgs(this)
                {
                    OldValue = oldValue,
                    NewValue = value
                };

                _ChatWindowRectangleChanged.InvokeAsync(ea);
            }
        }

        public Dictionary<string, ChatMsgType> ChatCodes
        {
            get { return _ChatCodes; }
            set
            {
                var tmpDict = value.ToDictionary(entry => entry.Key, entry => new ChatMsgType(entry.Value));

                _ChatCodes = tmpDict;

                var ea = new ChatMsgTypeChangeEventArgs(this)
                {
                    ChatCodes = _ChatCodes
                };

                _ChatCodesChanged.InvokeAsync(ea);
            }
        }

        public int IsFirstTime
        {
            get { return _IsFirstTime; }
            set { _IsFirstTime = value; }
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

                _UiLanguageChanged.InvokeAsync(ea);
            }
        }

        public bool IsHiddenByUser
        {
            get { return _IsHiddenByUser; }
            set
            {
                _IsHiddenByUser = value;
            }
        }

        public bool IsTextReader
        {
            get { return _IsTextReader; }
            set
            {
                _IsTextReader = value;
            }
        }


        #endregion

        #region **LocalVariables.

        int _ChatFontSize;

        Color _BackgroundColor;

        List<Color> _RecentBackgroundColors;

        int _ParagraphSpaceCount;

        int _LineBreakeHeight;

        TranslationEngine _TranslationEngine;

        string _FFLanguage;

        string _TranslateToLanguage;

        HotKeyCombination _ShowHideChatKeys;

        HotKeyCombination _ClickThoughtChatKeys;

        HotKeyCombination _ClearChatKeys;

        bool _IsChatClickThrough;

        bool _IsChatAlwaysOnTop;

        bool _IsHideSettingsToTray;

        bool _IsAutoHide;

        TimeSpan _AutoHideTimeout;

        System.Drawing.PointD _SettingsWindowSize = new System.Drawing.PointD(0.0, 0.0);

        System.Drawing.RectangleD _ChatWindowRectangle = new System.Drawing.RectangleD(0.0, 0.0, 0.0, 0.0);

        private ReadOnlyCollection<ReadOnlyCollection<TranslatorLanguague>> _TranslatorsLanguages;

        int _IsFirstTime;

        Dictionary<string, ChatMsgType> _ChatCodes;

        int _UiLanguage;

        bool _IsHiddenByUser;

        bool _IsTextReader;

        #endregion

        public TataruUIModel(ReadOnlyCollection<ReadOnlyCollection<TranslatorLanguague>> translatorsLanguages)
        {
            _TranslatorsLanguages = translatorsLanguages;

            this._ChatFontSizeChanged = new AsyncEvent<IntegerValueChangeEventArgs>(this.EventErrorHandler, "ChatFontSizeChanged");

            this._BackgroundColorChanged = new AsyncEvent<ColorChangeEventArgs>(this.EventErrorHandler, "BackgroundColorChanged");

            this._ColorListChanged = new AsyncEvent<ColorListChangeEventArgs>(this.EventErrorHandler, "ColorListChanged");

            this._ParagraphSpaceCountChanged = new AsyncEvent<IntegerValueChangeEventArgs>(this.EventErrorHandler, "ParagraphSpaceCountChanged");
            this._LineBreakeHeightChanged = new AsyncEvent<IntegerValueChangeEventArgs>(this.EventErrorHandler, "LineBreakeHeightChanged");

            this._TranslationEngineChanged = new AsyncEvent<TranslationEngineChangeEventArgs>(this.EventErrorHandler, "TranslationEngineChanged");

            this._FFLanguageChanged = new AsyncEvent<StringValueChangeEventArgs>(this.EventErrorHandler, "FFLanguageChanged");
            this._TranslateToLanguageChanged = new AsyncEvent<StringValueChangeEventArgs>(this.EventErrorHandler, "TranslateToLanguageChanged");

            this._ShowHideChatCombinationChanged = new AsyncEvent<HotKeyCombinationChangeEventArgs>(this.EventErrorHandler, "ShowHideChatCombinationChanged");
            this._ClickThoughtChatCombinationChanged = new AsyncEvent<HotKeyCombinationChangeEventArgs>(this.EventErrorHandler, "ClickThoughtChatCombinationChanged");
            this._ClearChatCombinationChanged = new AsyncEvent<HotKeyCombinationChangeEventArgs>(this.EventErrorHandler, "ClearChatCombinationChanged");

            this._IsChatClickThroughChanged = new AsyncEvent<BooleanChangeEventArgs>(this.EventErrorHandler, "IsChatClickThroughChanged");
            this._IsChatAlwaysOnTopChanged = new AsyncEvent<BooleanChangeEventArgs>(this.EventErrorHandler, "IsChatAlwaysOnTopChanged");
            this._IsHideSettingsToTrayChanged = new AsyncEvent<BooleanChangeEventArgs>(this.EventErrorHandler, "IsHideSettingsToTrayChanged");
            this._IsAutoHideChanged = new AsyncEvent<BooleanChangeEventArgs>(this.EventErrorHandler, "IsAutoHideChanged");

            this._AutoHideTimeoutChanged = new AsyncEvent<TimeSpanChangeEventArgs>(this.EventErrorHandler, "AutoHideTimeoutChanged");

            this._SettingsWindowSizeChanged = new AsyncEvent<PointDValueChangeEventArgs>(this.EventErrorHandler, "SettingsWindowSizeChanged");
            this._ChatWindowRectangleChanged = new AsyncEvent<RectangleDValueChangeEventArgs>(this.EventErrorHandler, "ChatWindowRectangleChanged");

            this._ChatCodesChanged = new AsyncEvent<ChatMsgTypeChangeEventArgs>(this.EventErrorHandler, "ChatCodesChanged");

            this._UiLanguageChanged = new AsyncEvent<IntegerValueChangeEventArgs>(this.EventErrorHandler, "UiLanguageChanged");

            _RecentBackgroundColors = new List<Color>();

            _ShowHideChatKeys = new HotKeyCombination("ShowHideChatWin");
            _ClickThoughtChatKeys = new HotKeyCombination("ClickThoughtChatWin");

            _IsHiddenByUser = false;
        }

        public void SetSettings(UserSettings userSettings)
        {
            UserSettings tmpSettings = new UserSettings();

            UiLanguage = userSettings.CurentUILanguague;

            ChatFontSize = userSettings.FontSize;

            BackgroundColor = userSettings.BackgroundColor;

            ParagraphSpaceCount = userSettings.InsertSpaceCount;

            LineBreakeHeight = userSettings.LineBreakHeight;

            TranslationEngine = (TranslationEngine)userSettings.CurrentTranslationEngine;

            FFLanguage = userSettings.CurrentFFXIVLanguage;

            TranslateToLanguage = userSettings.CurrentTranslateToLanguage;

            IsChatClickThrough = userSettings.IsClickThrough;

            IsChatAlwaysOnTop = userSettings.IsAlwaysOnTop;

            IsHideSettingsToTray = userSettings.IsHideToTray;

            IsAutoHide = userSettings.IsAutoHide;

            AutoHideTimeout = userSettings.AutoHideTimeout;

            if (userSettings.ShowHideChatKeys != null)
                ShowHideChatKeys = new HotKeyCombination(userSettings.ShowHideChatKeys);
            else
                ShowHideChatKeys = new HotKeyCombination(tmpSettings.ShowHideChatKeys);

            if (userSettings.ClickThoughtChatKeys != null)
                ClickThoughtChatKeys = new HotKeyCombination(userSettings.ClickThoughtChatKeys);
            else
                ClickThoughtChatKeys = new HotKeyCombination(tmpSettings.ClickThoughtChatKeys);

            if (userSettings.ClearChatKeys != null)
                ClearChatKeys = new HotKeyCombination(userSettings.ClearChatKeys);
            else
                ClearChatKeys = new HotKeyCombination(tmpSettings.ClearChatKeys);

            SettingsWindowSize = userSettings.SettingsWindowSize;

            ChatWindowRectangle = userSettings.ChatWindowLocation;

            if (userSettings.RecentBackgroundColors != null)
                RecentBackgroundColors = userSettings.RecentBackgroundColors.Distinct().ToList();
            else
                RecentBackgroundColors = tmpSettings.RecentBackgroundColors.Distinct().ToList();

            var tmpChatList = Helper.LoadJsonData<List<ChatMsgType>>(GlobalSettings.ChatCodesFilePath);

            var tmpChatCodes = new Dictionary<string, ChatMsgType>();
            for (int i = 0; i < tmpChatList.Count; i++)
            {
                tmpChatCodes.TryAdd(tmpChatList[i].ChatCode, tmpChatList[i]);
            }

            var userCodes = userSettings.ChatCodes.Values.ToList();
            for (int i = 0; i < userCodes.Count; i++)
            {
                if (tmpChatCodes.ContainsKey(userCodes[i].ChatCode))
                {
                    tmpChatCodes[userCodes[i].ChatCode] = userCodes[i];
                }
            }

            ChatCodes = tmpChatCodes;

            IsFirstTime = userSettings.IsFirstTime;
        }

        public UserSettings GetSettings()
        {
            UserSettings userSettings = new UserSettings();

            userSettings.CurentUILanguague = UiLanguage;

            userSettings.FontSize = ChatFontSize;

            userSettings.BackgroundColor = BackgroundColor;

            userSettings.RecentBackgroundColors = RecentBackgroundColors.Distinct().ToList();

            userSettings.InsertSpaceCount = ParagraphSpaceCount;

            userSettings.LineBreakHeight = LineBreakeHeight;

            userSettings.CurrentTranslationEngine = (int)TranslationEngine;

            userSettings.CurrentFFXIVLanguage = FFLanguage;

            userSettings.CurrentTranslateToLanguage = TranslateToLanguage;

            userSettings.IsClickThrough = IsChatClickThrough;

            userSettings.IsAlwaysOnTop = IsChatAlwaysOnTop;

            userSettings.IsHideToTray = IsHideSettingsToTray;

            userSettings.IsAutoHide = IsAutoHide;

            userSettings.AutoHideTimeout = AutoHideTimeout;

            userSettings.ShowHideChatKeys = new HotKeyCombination(ShowHideChatKeys);

            userSettings.ClickThoughtChatKeys = new HotKeyCombination(ClickThoughtChatKeys);

            userSettings.ClearChatKeys = new HotKeyCombination(ClearChatKeys);

            userSettings.SettingsWindowSize = SettingsWindowSize;

            userSettings.ChatWindowLocation = ChatWindowRectangle;

            userSettings.ChatCodes = ChatCodes.ToDictionary(entry => entry.Key, entry => new ChatMsgType(entry.Value));

            userSettings.IsFirstTime = IsFirstTime;

            return userSettings;
        }

        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            Logger.WriteLog(text);
        }
    }
}
