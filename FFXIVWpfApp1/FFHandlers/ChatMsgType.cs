// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIITataruHelper.FFHandlers
{
    enum MsgType : int { Translate = 1, Check = 2 };

    struct ChatMsgType
    {
        public string ChatCode { get; set; }
        public MsgType MsgType { get; set; }

        public ChatMsgType(string chatCode, MsgType msgType)
        {
            ChatCode = chatCode;
            MsgType = msgType;
        }
    }
}
