// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIVTataruHelper.FFHandlers;
using System;

namespace FFXIVTataruHelper.EventArguments
{
    public class ChatMessageArrivedEventArgs : TatruEventArgs
    {
        public FFChatMsg ChatMessage { get; internal set; }

        internal ChatMessageArrivedEventArgs(Object sender) : base(sender) { }

        public ChatMessageArrivedEventArgs(ChatMessageArrivedEventArgs msgArgs) : base(msgArgs.Sender)
        {
            ChatMessage = new FFChatMsg(msgArgs.ChatMessage);
        }
    }
}
