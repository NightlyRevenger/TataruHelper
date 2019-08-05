// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;

namespace FFXIITataruHelper.EventArguments
{
    public class ChatMsgTypeChangeEventArgs : TatruEventArgs
    {
        public Dictionary<string, ChatMsgType> ChatCodes { get; internal set; }

        internal ChatMsgTypeChangeEventArgs(Object sender) : base(sender) { }
    }
}
