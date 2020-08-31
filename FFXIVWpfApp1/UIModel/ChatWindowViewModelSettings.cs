// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIVTataruHelper.EventArguments;
using FFXIVTataruHelper.ViewModel;
using FFXIVTataruHelper.WinUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Translation;

namespace FFXIVTataruHelper.UIModel
{
    public class ChatWindowViewModelSettings : INotifyPropertyChanged, FFXIVTataruHelper.TataruComponentModel.INotifyPropertyChangedAsync
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public event AsyncEventHandler<AsyncPropertyChangedEventArgs> AsyncPropertyChanged
        {
            add { this._AsyncPropertyChanged.Register(value); }
            remove { this._AsyncPropertyChanged.Unregister(value); }
        }
        private AsyncEvent<AsyncPropertyChangedEventArgs> _AsyncPropertyChanged;

        private string _name;
        private long _winId;

        private double _chatFontSize;
        private double _lineBreakHeight;
        private int _spacingCount;

        System.Windows.Media.FontFamily _ChatFont;

        private bool _isAlwaysOnTop;
        private bool _isClickThrough;
        private bool _isAutoHide;

        private TimeSpan _autoHideTimeout;

        private Color _backGroundColor;

        private TranslationEngineName _translationEngineName;
        private TranslatorLanguague _fromLanguague;
        private TranslatorLanguague _toLanguague;

        private System.Drawing.RectangleD _chatWindowRectangle;

        private List<ChatCodeViewModel> _chatCodes;

        private bool _ShowTimestamps;

        private HotKeyCombination _showHideChatKeys;
        private HotKeyCombination _clickThoughtChatKeys;
        private HotKeyCombination _clearChatKeys;

        public string Name
        {
            get => _name; set
            {
                if (_name == value) return;

                _name = value;
                OnPropertyChanged();
            }
        }
        public long WinId
        {
            get => _winId;
            set
            {
                if (_winId == value) return;

                _winId = value;

                if (ShowHideChatKeys != null)
                    ShowHideChatKeys = new HotKeyCombination("ShowHideChatKeys" + Convert.ToString(_winId), ShowHideChatKeys);
                else
                    ShowHideChatKeys = new HotKeyCombination("ShowHideChatKeys" + Convert.ToString(_winId));

                if (ClickThoughtChatKeys != null)
                    ClickThoughtChatKeys = new HotKeyCombination("ClickThoughtChatKeys" + Convert.ToString(_winId), ClickThoughtChatKeys);
                else
                    ClickThoughtChatKeys = new HotKeyCombination("ClickThoughtChatKeys" + Convert.ToString(_winId));

                if (ClearChatKeys != null)
                    ClearChatKeys = new HotKeyCombination("ClickThoughtChatKeys" + Convert.ToString(_winId), ClearChatKeys);
                else
                    ClearChatKeys = new HotKeyCombination("ClickThoughtChatKeys" + Convert.ToString(_winId));

                OnPropertyChanged();
            }
        }

        public double ChatFontSize
        {
            get => _chatFontSize;
            set
            {
                if (_chatFontSize == value) return;

                _chatFontSize = value;
                OnPropertyChanged();
            }
        }
        public double LineBreakHeight
        {
            get => _lineBreakHeight;
            set
            {
                if (_lineBreakHeight == value) return;

                _lineBreakHeight = value;
                OnPropertyChanged();
            }
        }
        public int SpacingCount
        {
            get => _spacingCount;
            set
            {
                if (_spacingCount == value) return;

                _spacingCount = value;
                OnPropertyChanged();
            }
        }

        public FontFamily ChatFont
        {
            get => _ChatFont;
            set
            {
                if (_ChatFont == value) return;

                _ChatFont = value;
                OnPropertyChanged();
            }
        }

        public bool IsAlwaysOnTop
        {
            get => _isAlwaysOnTop;
            set
            {
                if (_isAlwaysOnTop == value) return;

                _isAlwaysOnTop = value;
                OnPropertyChanged();
            }
        }
        public bool IsClickThrough
        {
            get => _isClickThrough;
            set
            {
                if (_isClickThrough == value) return;

                _isClickThrough = value;
                OnPropertyChanged();
            }
        }
        public bool IsAutoHide
        {
            get => _isAutoHide;
            set
            {
                if (_isAutoHide == value) return;

                _isAutoHide = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan AutoHideTimeout
        {
            get => _autoHideTimeout;
            set
            {
                if (_autoHideTimeout == value) return;

                _autoHideTimeout = value;
                OnPropertyChanged();
            }
        }

        public Color BackGroundColor
        {
            get => _backGroundColor;
            set
            {
                if (_backGroundColor == value) return;

                _backGroundColor = value;
                OnPropertyChanged();
            }
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

        public TranslationEngineName TranslationEngineName
        {
            get => _translationEngineName;
            set
            {
                if (_translationEngineName == value) return;

                _translationEngineName = value;
                OnPropertyChanged();
            }
        }
        public TranslatorLanguague FromLanguague
        {
            get => _fromLanguague;
            set
            {
                if (_fromLanguague == value) return;

                _fromLanguague = value;
                OnPropertyChanged();
            }
        }
        public TranslatorLanguague ToLanguague
        {
            get => _toLanguague;
            set
            {
                if (_toLanguague == value) return;

                _toLanguague = value;
                OnPropertyChanged();
            }
        }

        public System.Drawing.RectangleD ChatWindowRectangle
        {
            get => _chatWindowRectangle;
            set
            {
                if (_chatWindowRectangle == value) return;

                _chatWindowRectangle = value;
                OnPropertyChanged();
            }
        }

        public List<ChatCodeViewModel> ChatCodes
        {
            get => _chatCodes;
            set
            {
                if (_chatCodes == value) return;

                _chatCodes = value;
                OnPropertyChanged();
            }
        }

        public HotKeyCombination ShowHideChatKeys
        {
            get => _showHideChatKeys;
            set
            {
                if (_showHideChatKeys == value) return;

                _showHideChatKeys = value;
                OnPropertyChanged();
            }
        }
        public HotKeyCombination ClickThoughtChatKeys
        {
            get => _clickThoughtChatKeys;
            set
            {
                if (_clickThoughtChatKeys == value) return;

                _clickThoughtChatKeys = value;
                OnPropertyChanged();
            }
        }
        public HotKeyCombination ClearChatKeys
        {
            get => _clearChatKeys;
            set
            {
                if (_clearChatKeys == value) return;

                _clearChatKeys = value;
                OnPropertyChanged();
            }
        }

        public ChatWindowViewModelSettings()
        {
            this._AsyncPropertyChanged = new AsyncEvent<AsyncPropertyChangedEventArgs>(this.EventErrorHandler, "ChatWindowViewModelSettings \n AsyncPropertyChanged");

            Name = String.Empty;
            WinId = 0;

            ChatFontSize = 14;
            LineBreakHeight = 0;
            SpacingCount = 0;

            ChatFont = new FontFamily("Segoe UI");
            //ChatFont = new FontFamily("hfghdfg");

            IsAlwaysOnTop = true;
            IsClickThrough = false;
            IsAutoHide = false;

            AutoHideTimeout = new TimeSpan(0, 0, 30);

            BackGroundColor = Color.FromArgb(0x4B, 0, 0, 0);

            TranslationEngineName = TranslationEngineName.GoogleTranslate;
            FromLanguague = null;
            ToLanguague = null;

            ChatWindowRectangle = new System.Drawing.RectangleD(0, 0, 480, 320);

            ChatCodes = new List<ChatCodeViewModel>();

            ShowHideChatKeys = new HotKeyCombination();
            ClickThoughtChatKeys = new HotKeyCombination();
            ClearChatKeys = new HotKeyCombination();
        }

        public ChatWindowViewModelSettings(string name, long winId)
        {
            this._AsyncPropertyChanged = new AsyncEvent<AsyncPropertyChangedEventArgs>(this.EventErrorHandler, "ChatWindowViewModelSettings \n AsyncPropertyChanged");

            Name = name;
            WinId = winId;

            ChatFontSize = 14;
            LineBreakHeight = 0;
            SpacingCount = 0;

            ChatFont = new FontFamily("Segoe UI");

            IsAlwaysOnTop = true;
            IsClickThrough = false;
            IsAutoHide = false;

            AutoHideTimeout = new TimeSpan(0, 0, 30);

            BackGroundColor = Color.FromArgb(0x4B, 0, 0, 0);

            TranslationEngineName = TranslationEngineName.GoogleTranslate;
            FromLanguague = null;
            ToLanguague = null;

            ChatWindowRectangle = new System.Drawing.RectangleD(0, 0, 480, 320);

            ChatCodes = new List<ChatCodeViewModel>();

            ShowHideChatKeys = new HotKeyCombination("ShowHideChatKeys" + Convert.ToString(WinId));
            ClickThoughtChatKeys = new HotKeyCombination("ClickThoughtChatKeys" + Convert.ToString(WinId));
            ClearChatKeys = new HotKeyCombination("ClearChatKeys" + Convert.ToString(WinId));
        }

        public ChatWindowViewModelSettings(ChatWindowViewModelSettings settings)
        {
            this._AsyncPropertyChanged = new AsyncEvent<AsyncPropertyChangedEventArgs>(this.EventErrorHandler, "ChatWindowViewModelSettings \n AsyncPropertyChanged");

            Name = settings.Name;
            WinId = settings.WinId;

            ChatFontSize = settings.ChatFontSize;
            LineBreakHeight = settings.LineBreakHeight;
            SpacingCount = settings.SpacingCount;

            ChatFont = settings.ChatFont;

            IsAlwaysOnTop = settings.IsAlwaysOnTop;
            IsClickThrough = settings.IsClickThrough;
            IsAutoHide = settings.IsAutoHide;

            AutoHideTimeout = new TimeSpan(settings.AutoHideTimeout.Ticks);

            BackGroundColor = settings.BackGroundColor;

            TranslationEngineName = settings.TranslationEngineName;
            if (settings.FromLanguague != null)
                FromLanguague = new TranslatorLanguague(settings.FromLanguague);
            if (settings.ToLanguague != null)
                ToLanguague = new TranslatorLanguague(settings.ToLanguague);

            ChatWindowRectangle = settings.ChatWindowRectangle;

            ChatCodes = settings.ChatCodes.Select(code => new ChatCodeViewModel(code)).ToList();

            ShowTimestamps = settings.ShowTimestamps;

            ShowHideChatKeys = new HotKeyCombination(settings.ShowHideChatKeys);
            ClickThoughtChatKeys = new HotKeyCombination(settings.ClickThoughtChatKeys);
            ClearChatKeys = new HotKeyCombination(settings.ClearChatKeys);
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
    }
}
