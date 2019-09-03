// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using static FFXIVTataruHelper.WinUtils.MouseHooker;

namespace FFXIVTataruHelper.EventArguments
{
    public class LowLevelMouseEventArgs : TatruEventArgs
    {
        public MouseMessages MouseMessages { get; internal set; }
        public MSLLHOOKSTRUCT MouseEventFlags { get; internal set; }

        internal LowLevelMouseEventArgs(Object sender) : base(sender) { }
    }
}
