// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIITataruHelper.EventArguments;
using FFXIITataruHelper.FFHandlers;
using FFXIITataruHelper.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using static FFXIITataruHelper.Translation.WebTranslator;

namespace FFXIITataruHelper
{
    public class ChatProcessor
    {
        #region **Events.

        public event AsyncEventHandler<TranslationArrivedEventArgs> TranslationArrived
        {
            add { this._TranslationArrived.Register(value); }
            remove { this._TranslationArrived.Unregister(value); }
        }
        private AsyncEvent<TranslationArrivedEventArgs> _TranslationArrived;

        #endregion

        #region **LocalVariables.

        WebTranslator _WebTranslator;

        TataruUIModel _TataruUIModel;

        DateTime _LastTranslationTime;

        Dictionary<string, ChatMsgType> _ChatCodesTypes;

        List<EngineDescription> _TranslationEngines;

        List<string> MsgBlackList;

        int _EnginIndex = 0;

        #endregion

        public ChatProcessor(FFMemoryReader fFMemoryReader, WebTranslator webTranslator, TataruUIModel tataruUIModel)
        {
            this._TranslationArrived = new AsyncEvent<TranslationArrivedEventArgs>(this.EventErrorHandler, "TranslationArrived");

            _ChatCodesTypes = tataruUIModel.ChatCodes;
            _TranslationEngines = new List<EngineDescription>();
            MsgBlackList = new List<string>();

            _WebTranslator = webTranslator;
            _TataruUIModel = tataruUIModel;

            Init();

            SubscribeToEvents(fFMemoryReader);

            _LastTranslationTime = DateTime.UtcNow;
        }

        private void Init()
        {
            _TranslationEngines.Add(new EngineDescription(TranslationEngine.DeepL, 10));
            _TranslationEngines.Add(new EngineDescription(TranslationEngine.GoogleTranslate, 9));
            _TranslationEngines.Add(new EngineDescription(TranslationEngine.Yandex, 5));
            _TranslationEngines.Add(new EngineDescription(TranslationEngine.Multillect, 1));

            var tmpMsgBlackList = new List<string>();
            tmpMsgBlackList.Add("Triple Triad matches not allowed in current area.");
            tmpMsgBlackList.Add("Triple Triad matches allowed in current area.");
            tmpMsgBlackList.Add("You have left the sanctuary.");
            tmpMsgBlackList.Add("You have entered a sanctuary.");
            tmpMsgBlackList.Add("Updating online status to Away from Keyboard.");
            tmpMsgBlackList.Add("Updating online status. No longer away from keyboard.");

            MsgBlackList = Helper.LoadJsonData<List<string>>(GlobalSettings.BlackList);
            if (MsgBlackList == null)
                MsgBlackList = new List<string>();

            foreach (var st in tmpMsgBlackList)
            {
                if (!MsgBlackList.Contains(st))
                    MsgBlackList.Add(st);
            }
            MsgBlackList = MsgBlackList.Distinct().ToList();

            Helper.SaveJson(MsgBlackList, GlobalSettings.BlackList);

            for (int i = 0; i < MsgBlackList.Count; i++)
            {
                MsgBlackList[i] = Helper.ClearBlackListString(MsgBlackList[i]);
            }
        }

        private void SubscribeToEvents(FFMemoryReader fFMemoryReader)
        {
            fFMemoryReader.FFChatMessageArrived += OnFFChatMessageArrived;

            _TataruUIModel.TranslationEngineChanged += OnTranslationEngineChange;
            _TataruUIModel.ChatCodesChanged += OnChatCodesChange;

            _TataruUIModel.FFLanguageChanged += OnFFLanguageChange;
            _TataruUIModel.TranslateToLanguageChanged += OnTranslateToLanguageChange;
        }

        private async Task OnChatCodesChange(ChatMsgTypeChangeEventArgs ea)
        {
            _ChatCodesTypes = ((TataruUIModel)ea.Sender).ChatCodes;
        }

        private async Task OnFFChatMessageArrived(ChatMessageArrivedEventArgs ea)
        {
            bool logged = false;
            ChatMsgType msgType = new ChatMsgType();

            if (_ChatCodesTypes != null)
            {
                if (_ChatCodesTypes.TryGetValue(ea.ChatMessage.Code, out msgType))
                {
                    if (!MsgBlackList.Contains(ea.ChatMessage.Text))
                        await ProcessChatMsg(ea.ChatMessage, msgType);

                    if (CmdArgsStatus.LogPlotChat)
                    {
                        Logger.WriteChatLog(String.Format("{0} {1}: {2}", ea.ChatMessage.TimeStamp, ea.ChatMessage.Code, ea.ChatMessage.Text));
                        logged = true;
                    }
                }

                if (CmdArgsStatus.LogAllChat && !logged)
                    Logger.WriteChatLog(String.Format("{0} {1}: {2}", ea.ChatMessage.TimeStamp, ea.ChatMessage.Code, ea.ChatMessage.Text));
            }

        }

        private async Task OnTranslationEngineChange(TranslationEngineChangeEventArgs ea)
        {
            var trEng = (TranslationEngine)ea.NewEngine;

            _TranslationEngines = _TranslationEngines.OrderBy(x => x.Value * -1).ToList();

            var ind = _TranslationEngines.FindIndex(x => x.TranslationEngine == trEng);
            _TranslationEngines.Swap(ind, 0);

            _WebTranslator.CurrentTranslationEngine = (WebTranslator.TranslationEngine)ea.NewEngine;
        }

        private async Task OnFFLanguageChange(StringValueChangeEventArgs ea)
        {
            _WebTranslator.SourceLanguage = ea.NewString;
        }

        private async Task OnTranslateToLanguageChange(StringValueChangeEventArgs ea)
        {
            _WebTranslator.TargetLanguage = ea.NewString;
        }

        private async Task ProcessChatMsg(FFChatMsg msg, ChatMsgType msgType)
        {
            switch (msgType.MsgType)
            {
                case MsgType.Translate:
                    {
                        var translation = await TranslateMessage(msg.Text, msgType.Color);

                        await _TranslationArrived.InvokeAsync(translation);

                        break;
                    }
                default:
                    {
                        return;
                    }
            }
        }

        private async Task<TranslationArrivedEventArgs> TranslateMessage(string text, Color color)
        {
            var translationEA = new TranslationArrivedEventArgs(this)
            {
                Text = "",
                ErrorCode = -1,
                Color = color,
            };


            int translateTryCount = 0;

            bool notTransalted = true;
            int errorCode = 0;

            while (translateTryCount < GlobalSettings.MaxTranslateTryCount && notTransalted)
            {
                var diffTime = (int)Math.Round((DateTime.UtcNow - _LastTranslationTime).TotalMilliseconds);

                if (diffTime < GlobalSettings.TranslationDelay)
                    await Task.Delay(GlobalSettings.TranslationDelay - diffTime);


                string translation = string.Empty;
                Task.Run(async () =>
                {
                    translation = await _WebTranslator.TranslateAsync(text);
                }).Wait(GlobalSettings.TranslatorWaitTime);
                //}).Wait();

                _LastTranslationTime = DateTime.UtcNow;

                if (translation.Length < 1)
                {
                    notTransalted = true;
                    errorCode = 1;

                    if (_EnginIndex < _TranslationEngines.Count)
                        _EnginIndex++;
                    else
                        _EnginIndex = 1;

                    _TataruUIModel.TranslationEngine = _TranslationEngines[_EnginIndex].TranslationEngine;

                    var ea = new TranslationArrivedEventArgs(this)
                    {
                        Text = "",
                        ErrorCode = errorCode,
                        Color = color,
                    };
                    _TranslationArrived.InvokeAsync(ea);
                }
                else
                {
                    notTransalted = false;
                    errorCode = 0;
                }

                translateTryCount++;

                translationEA = new TranslationArrivedEventArgs(this)
                {
                    Text = translation,
                    ErrorCode = errorCode,
                    Color = color,
                };
            }

            return translationEA;
        }

        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            Logger.WriteLog(text);
        }
    }
}
