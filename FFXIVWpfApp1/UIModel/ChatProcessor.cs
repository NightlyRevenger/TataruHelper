// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIVTataruHelper.EventArguments;
using FFXIVTataruHelper.FFHandlers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Translation;

namespace FFXIVTataruHelper
{
    public class ChatProcessor
    {
        #region **Events.

        public event AsyncEventHandler<ChatMessageArrivedEventArgs> TextArrived
        {
            add { this._TextArrivedArrived.Register(value); }
            remove { this._TextArrivedArrived.Unregister(value); }
        }
        private AsyncEvent<ChatMessageArrivedEventArgs> _TextArrivedArrived;

        #endregion

        #region **Properties.

        public ReadOnlyCollection<TranslationEngine> TranslationEngines
        {
            get { return _WebTranslator.TranslationEngines; }
        }

        public ReadOnlyCollection<ChatMsgType> AllChatCodes
        {
            get
            {
                return new ReadOnlyCollection<ChatMsgType>(_AllChatCodes);
            }
        }

        #endregion

        #region **LocalVariables.

        WebTranslator _WebTranslator;

        DateTime _LastTranslationTime;

        List<ChatMsgType> _AllChatCodes;

        List<string> MsgBlackList;

        List<string> ChatCodesWithNickNames;

        #endregion

        public ChatProcessor(WebTranslator webTranslator)
        {
            this._TextArrivedArrived = new AsyncEvent<ChatMessageArrivedEventArgs>(this.EventErrorHandler, "TranslationArrived");

            _AllChatCodes = Helper.LoadJsonData<List<ChatMsgType>>(GlobalSettings.ChatCodesFilePath);

            _WebTranslator = webTranslator;

            MsgBlackList = new List<string>();

            Init();

            _LastTranslationTime = DateTime.UtcNow;
        }

        private void Init()
        {

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

            var tmpChatCodesWithNickNames = new List<string>(27);
            tmpChatCodesWithNickNames.Add("0048");
            tmpChatCodesWithNickNames.Add("000A");
            tmpChatCodesWithNickNames.Add("000B");
            tmpChatCodesWithNickNames.Add("000E");
            tmpChatCodesWithNickNames.Add("000D");
            tmpChatCodesWithNickNames.Add("001D");
            tmpChatCodesWithNickNames.Add("001C");
            tmpChatCodesWithNickNames.Add("0018");
            tmpChatCodesWithNickNames.Add("001E");
            tmpChatCodesWithNickNames.Add("000F");
            tmpChatCodesWithNickNames.Add("0010");
            tmpChatCodesWithNickNames.Add("0011");
            tmpChatCodesWithNickNames.Add("0012");
            tmpChatCodesWithNickNames.Add("0013");
            tmpChatCodesWithNickNames.Add("0014");
            tmpChatCodesWithNickNames.Add("0015");
            tmpChatCodesWithNickNames.Add("0016");
            tmpChatCodesWithNickNames.Add("0017");
            tmpChatCodesWithNickNames.Add("001B");
            tmpChatCodesWithNickNames.Add("0025");
            tmpChatCodesWithNickNames.Add("0065");
            tmpChatCodesWithNickNames.Add("0066");
            tmpChatCodesWithNickNames.Add("0067");
            tmpChatCodesWithNickNames.Add("0068");
            tmpChatCodesWithNickNames.Add("0069");
            tmpChatCodesWithNickNames.Add("006A");
            tmpChatCodesWithNickNames.Add("006B");

            ChatCodesWithNickNames = Helper.LoadJsonData<List<string>>(GlobalSettings.IgnoreNickNameChatCodes);
            if (ChatCodesWithNickNames == null)
                ChatCodesWithNickNames = new List<string>();

            foreach (var st in tmpChatCodesWithNickNames)
            {
                if (!ChatCodesWithNickNames.Contains(st))
                    ChatCodesWithNickNames.Add(st);
            }
            ChatCodesWithNickNames = ChatCodesWithNickNames.Distinct().ToList();

            Helper.SaveJson(ChatCodesWithNickNames, GlobalSettings.IgnoreNickNameChatCodes);
        }

        public async Task OnFFChatMessageArrived(ChatMessageArrivedEventArgs ea)
        {
            ChatMsgType msgType = new ChatMsgType();

            if (!MsgBlackList.Contains(ea.ChatMessage.Text))
                await ProcessChatMsg(ea, msgType);

            if (CmdArgsStatus.LogAllChat || CmdArgsStatus.LogPlotChat)
                Logger.WriteChatLog(String.Format("{0} {1}: {2}", ea.ChatMessage.TimeStamp, ea.ChatMessage.Code, ea.ChatMessage.Text));
        }

        public async Task<string> Translate(string inSentence, TranslationEngine translationEngine, TranslatorLanguague fromLang, TranslatorLanguague toLang, string chatCode)
        {
            string text = string.Empty;
            string NickName = string.Empty;

            if (ChatCodesWithNickNames.Contains(chatCode))
            {
                var ind1 = inSentence.IndexOf(":");
                if(ind1>0)
                {
                    ind1++;

                    NickName = inSentence.Substring(0, ind1);
                    inSentence=inSentence.Remove(0, ind1);
                }
            }

            text = await _WebTranslator.TranslateAsync(inSentence, translationEngine, fromLang, toLang);

            if (NickName.Length > 0)
                text = NickName +" "+ text;

            return text;
        }

        private async Task ProcessChatMsg(ChatMessageArrivedEventArgs ea, ChatMsgType msgType)
        {
            switch (msgType.MsgType)
            {
                default:
                    {
                        var translation = new ChatMessageArrivedEventArgs(ea);

                        await _TextArrivedArrived.InvokeAsync(translation);

                        break;
                    }
            }
        }

        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            Logger.WriteLog(text);
        }
    }
}
