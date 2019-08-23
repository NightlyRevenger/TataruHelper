// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIITataruHelper.EventArguments;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;


namespace FFXIITataruHelper.ViewModel
{
    public class TataruViewModel : INotifyPropertyChanged
    {
        private BindingList<ChatCodeViewModel> _ChatCodes { get; set; }

        public BindingList<ChatCodeViewModel> ChatCodes
        {
            get { return _ChatCodes; }
            set
            {
                _ChatCodes = value;
                OnPropertyChanged("ChatCodes");
            }
        }

        private TataruUIModel _TataruUIModel;

        public TataruUICommand SwitchLanguageCommand { get; set; }

        public TataruViewModel(TataruUIModel tataruUIModel)
        {
            _TataruUIModel = tataruUIModel;
            _TataruUIModel.ChatCodesChanged += OnChatCodesChange;

            SwitchLanguageCommand = new TataruUICommand(ChangeUILanguageCommand);

            ChatCodes = new BindingList<ChatCodeViewModel>()
            {
                new ChatCodeViewModel("0039","System", Colors.Aqua, true),
            };
        }

        private async Task OnChatCodesChange(ChatMsgTypeChangeEventArgs ea)
        {
            var tmpCodeList = ea.ChatCodes.Values.ToList();

            var tmpViewModel = new List<ChatCodeViewModel>();

            foreach (var msg in tmpCodeList)
            {
                bool isCheked = (msg.MsgType == MsgType.Translate) ? true : false;

                tmpViewModel.Add(new ChatCodeViewModel(msg.ChatCode, msg.Name, msg.Color, isCheked));
            }

            ChatCodes = new BindingList<ChatCodeViewModel>(tmpViewModel);

            _ChatCodes.ListChanged += ChatCodesChanged;
        }

        public void ChatCodesChanged(object sender, ListChangedEventArgs e)
        {
            var chatCode = ChatCodes[e.NewIndex];

            if (_TataruUIModel.ChatCodes.ContainsKey(chatCode.Code))
            {
                var msgType = _TataruUIModel.ChatCodes[chatCode.Code];
                msgType.Color = chatCode.Color;

                var isCheked = (chatCode.IsChecked) ? MsgType.Translate : MsgType.Skip;
                msgType.MsgType = isCheked;
            }
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


            //_TataruUIModel.UiLanguage = (int)LanguagueWrapper.Languages.Korean;
            _TataruUIModel.UiLanguage = lang;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
