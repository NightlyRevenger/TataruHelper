// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIITataruHelper.FFHandlers;
using System;

namespace FFXIITataruHelper.EventArguments
{
    public class ChatMessageArrivedEventArgs : TatruEventArgs
    {
        public FFChatMsg ChatMessage { get; internal set; }

        internal ChatMessageArrivedEventArgs(Object sender) : base(sender) { }
    }
}
