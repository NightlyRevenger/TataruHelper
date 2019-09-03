// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIVTataruHelper.EventArguments;
using FFXIVTataruHelper.TataruComponentModel;
using FFXIVTataruHelper.Translation;
using FFXIVTataruHelper.UIModel;
using FFXIVTataruHelper.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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

        public AsyncBindingList<ChatWindowViewModel> ChatWindows
        {
            get
            {
                var itemsView = (IEditableCollectionView)CollectionViewSource.GetDefaultView(_ChatWindows);
                itemsView.NewItemPlaceholderPosition = NewItemPlaceholderPosition.AtEnd;

                /*
                if (_ChatWindows.Count < 10)
                    itemsView.NewItemPlaceholderPosition = NewItemPlaceholderPosition.AtEnd;
                else
                    itemsView.NewItemPlaceholderPosition = NewItemPlaceholderPosition.None;//*/

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

            ShutDownRequestedCommand = new TataruUICommand(ShutDownRequsted);

            this.PropertyChanged += OnSelectedTabChanged;
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
            UiWindow.Window.UIThread(() =>
            {
                int ind = SelectedTabIndex;
                if (ind < ChatWindows.Count && ind >= 0)
                {
                    var wnd = ChatWindows[ind];

                    ChatWindows.RemoveAt(ind);

                    wnd.Dispose();
                }
            });
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

        private void OnSelectedTabChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedTabIndex")
            {
                /*
                if (_ChatWindows.Count > 0)
                {
                    if (SelectedTabIndex >= _ChatWindows.Count)
                        SelectedTabIndex = _ChatWindows.Count - 1;

                    if (SelectedTabIndex <= 0)
                        SelectedTabIndex = 0;
                }//*/
            }
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