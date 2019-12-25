// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIVTataruHelper.EventArguments;
using FFXIVTataruHelper.TataruComponentModel;
using FFXIVTataruHelper.UIModel;
using FFXIVTataruHelper.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using Translation;

namespace FFXIVTataruHelper.ViewModel
{
    public class TataruViewModel : INotifyPropertyChanged
    {
        public event AsyncEventHandler<AsyncListChangedEventHandler<ChatWindowViewModel>> ChatWindowsListChangedAsync
        {
            add { this._ChatWindowsListChangedAsync.Register(value); }
            remove { this._ChatWindowsListChangedAsync.Unregister(value); }
        }
        private AsyncEvent<AsyncListChangedEventHandler<ChatWindowViewModel>> _ChatWindowsListChangedAsync;


        public event EventHandler<EventArgs> ShutdownRequested;

        public int SelectedTabIndex
        {
            get { return _SelectedTabIndex; }
            set
            {
                if (_ChatWindows != null)
                {
                    int oldIndex = _SelectedTabIndex;

                    if (_SelectedTabIndex >= 0 && _SelectedTabIndex < _ChatWindows.Count)
                        _ChatWindows[_SelectedTabIndex].IsSelected = false;

                    _SelectedTabIndex = value;

                    if (_SelectedTabIndex >= 0 && _SelectedTabIndex < _ChatWindows.Count)
                        _ChatWindows[_SelectedTabIndex].IsSelected = true;

                    //if (oldIndex == value) return;

                    OnPropertyChanged();
                    OnPropertyChanged("InnerVisibility");

                }
            }
        }

        public bool InnerVisibility
        {
            get
            {
                if ((SelectedTabIndex < 0 || SelectedTabIndex >= ChatWindows.Count) || _ChatWindows.Count == 0)
                    _InnerVisibility = false;
                else
                    _InnerVisibility = true;

                return _InnerVisibility;
            }
            set
            {
                _InnerVisibility = value;
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////

        public bool UpdateCheckByUser { get; set; }

        public bool UserStartedUpdateTextVisibility
        {
            get => _UserStartedUpdateTextVisibility;
            set
            {
                if (_UserStartedUpdateTextVisibility != value)
                {
                    _UserStartedUpdateTextVisibility = value;
                    OnPropertyChanged();
                    OnPropertyChanged("UpdatingBlockVisiblity");
                }
            }
        }

        public TextToSpeech TextToSpeech { get; }

        public bool IsTextToSpeech
        {
            get => _isTextToSpeech;
            set
            {
                if (_isTextToSpeech == value) return;

                _isTextToSpeech = value;
                OnPropertyChanged();
            }
        }

        public float Speed
        {
            get => _speed;
            set
            {
                if (_speed == value) return;

                _speed = value;
                TextToSpeech.Speed = 0.5f + _speed * 0.1f;
                OnPropertyChanged();
            }
        }

        public float Pitch
        {
            get => _pitch;
            set
            {
                if (_pitch == value) return;

                _pitch = value;
                TextToSpeech.Pitch = _pitch / 8;
                OnPropertyChanged();
            }
        }

        public float Volume
        {
            get => _volume;
            set
            {
                if (_volume == value) return;

                _volume = value;
                TextToSpeech.Volume = _volume / 100;
                OnPropertyChanged();
            }
        }

        public bool DownloadingUpdateVisibility
        {
            get => _DownloadingUpdateVisibility;
            set
            {
                if (_DownloadingUpdateVisibility != value)
                {
                    _DownloadingUpdateVisibility = value;
                    OnPropertyChanged();
                    OnPropertyChanged("UpdatingBlockVisiblity");
                }
            }
        }

        public bool RestartReadyVisibility
        {
            get => _RestartReadyVisibility;
            set
            {
                if (_RestartReadyVisibility != value)
                {
                    _RestartReadyVisibility = value;
                    OnPropertyChanged();
                    OnPropertyChanged("UpdatingBlockVisiblity");
                }
            }
        }

        public bool UpdatingBlockVisiblity { get => DownloadingUpdateVisibility || RestartReadyVisibility || UserStartedUpdateTextVisibility; }

        public AsyncBindingList<ChatWindowViewModel> ChatWindows
        {
            get
            {
                var itemsView = (IEditableCollectionView)CollectionViewSource.GetDefaultView(_ChatWindows);
                itemsView.NewItemPlaceholderPosition = NewItemPlaceholderPosition.AtEnd;

                return _ChatWindows;
            }
            set
            {
                if (_ChatWindows != null)
                    _ChatWindows.AsyncListChanged -= OnChatWindowsListChangeAsync;

                _ChatWindows = value;

                _ChatWindows.AsyncListChanged += OnChatWindowsListChangeAsync;

                OnPropertyChanged();
            }
        }

        public ChatWindowViewModel CurrentChatWindow
        {
            get
            {
                int index = SelectedTabIndex;

                if (index >= 0 && index < ChatWindows.Count)
                    return ChatWindows[index];

                return null;
            }
        }

        public TataruUICommand SwitchLanguageCommand { get; set; }
        public TataruUICommand AddNewChatWindowCommand { get; set; }
        public TataruUICommand DeleteChatWindowCommand { get; set; }

        public TataruUICommand ShowChatWindowCommand { get; set; }

        public TataruUICommand ShutDownRequestedCommand { get; set; }

        AsyncBindingList<ChatWindowViewModel> _ChatWindows { get; set; }

        ReadOnlyCollection<TranslationEngine> TranslationEngines
        {
            get
            {
                var reslut = _TranslationEngines;
                if (reslut == null)
                    _TranslationEngines = _TataruModel.ChatProcessor.TranslationEngines;

                return _TranslationEngines;
            }
            set
            {
                if (_TranslationEngines != value) return;

                _TranslationEngines = value;
            }
        }

        int _SelectedTabIndex;
        bool _InnerVisibility;

        bool _DownloadingUpdateVisibility = false;
        bool _RestartReadyVisibility = false;
        bool _UserStartedUpdateTextVisibility = false;

        bool _isTextToSpeech;
        float _pitch;
        float _speed;
        float _volume;

        TataruUIModel _TataruUIModel;
        TataruModel _TataruModel;

        ReadOnlyCollection<TranslationEngine> _TranslationEngines;

        ReadOnlyCollection<ChatMsgType> _AllChatCodes;

        public TataruViewModel(TataruModel tatruModel)
        {
            this._ChatWindowsListChangedAsync = new AsyncEvent<AsyncListChangedEventHandler<ChatWindowViewModel>>(this.EventErrorHandler, "TataruViewModel \n ChatWindowsListChangedAsync");

            ChatWindows = new AsyncBindingList<ChatWindowViewModel>();
            _TataruModel = tatruModel;

            _TataruUIModel = tatruModel.TataruUIModel;

            TranslationEngines = tatruModel.ChatProcessor.TranslationEngines;
            _AllChatCodes = tatruModel.ChatProcessor.AllChatCodes;

            SwitchLanguageCommand = new TataruUICommand(ChangeUILanguageCommand);
            AddNewChatWindowCommand = new TataruUICommand(AddNewChatWindow);
            DeleteChatWindowCommand = new TataruUICommand(DeleteChatWindow);
            ShowChatWindowCommand = new TataruUICommand(ShowChatWindow);

            TextToSpeech = new TextToSpeech();
            Speed = 5f;
            Pitch = 0f;
            Volume = 100f;

            ShutDownRequestedCommand = new TataruUICommand(ShutDownRequsted);
        }

        private void ChangeUILanguageCommand(object parameter)
        {
            int lang = 2;
            try
            {
                lang = (int)Enum.Parse(typeof(LanguagueWrapper.Languages), (string)parameter);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex);
            }

            _TataruUIModel.UiLanguage = lang;
        }

        public void AddNewChatWindow()
        {
            UiWindow.Window.UIThread(() =>
            {
                if (ChatWindows.Count >= 10)
                    return;

                long winId = 0;
                if (ChatWindows.Count > 0)
                    winId = ChatWindows[ChatWindows.Count - 1].WinId + 1;

                ChatWindowViewModelSettings cws = null;
                ChatWindowViewModel cwm = null;

                var trEng = TranslationEngines;
                cws = new ChatWindowViewModelSettings((winId + 1).ToString(), winId);
                cwm = new ChatWindowViewModel(cws, trEng.ToList(), _AllChatCodes.ToList(), _TataruModel.HotKeyManager);

                ChatWindows.Add(cwm);

                SelectedTabIndex = ChatWindows.Count - 1;
            });
        }

        public void AddNewChatWindow(ChatWindowViewModelSettings settings)
        {
            if (ChatWindows.Count >= 10)
                return;

            ChatWindowViewModel cwm = null;

            var trEng = TranslationEngines;

            UiWindow.Window.UIThread(() =>
            {
                cwm = new ChatWindowViewModel(settings, trEng.ToList(), _AllChatCodes.ToList(), _TataruModel.HotKeyManager);

                try
                {
                    ChatWindows.Add(cwm);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                }

                SelectedTabIndex = ChatWindows.Count - 1;
            });
        }

        public async Task AddNewChatWindowAsync(ChatWindowViewModelSettings settings)
        {
            await Task.Run(() =>
            {
                AddNewChatWindow(settings);
            });
        }

        private void DeleteChatWindow()
        {
            DeleteChatWindow(SelectedTabIndex);
        }

        public void DeleteChatWindow(int index)
        {
            UiWindow.Window.UIThread(() =>
            {
                int ind = index;
                if (ind < ChatWindows.Count && ind >= 0)
                {
                    var wnd = ChatWindows[ind];

                    ChatWindows.RemoveAt(ind);

                    wnd.Dispose();
                }
            });
        }

        private void ShowChatWindow(object name)
        {
            string _name = (string)name;

            var cw = ChatWindows.FirstOrDefault(x => x.Name == _name);

            if (cw != null)
            {
                cw.IsHiddenByUser = !cw.IsHiddenByUser;
                cw.IsWindowVisible = !cw.IsWindowVisible;
            }
        }

        private void ShutDownRequsted()
        {
            ShutdownRequested?.Invoke(this, EventArgs.Empty);
        }

        private async Task OnChatWindowsListChangeAsync(AsyncListChangedEventHandler<ChatWindowViewModel> e)
        {
            await _ChatWindowsListChangedAsync.InvokeAsync(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            Logger.WriteLog(text);
        }
    }
}