// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FFXIITataruHelper.WinUtils.MouseHooker;

namespace FFXIITataruHelper.EventArguments
{
    public class LowLevelMouseEventArgs : TatruEventArgs
    {
        public MouseMessages MouseMessages { get; internal set; }
        public MSLLHOOKSTRUCT MouseEventFlags { get; internal set; }

        internal LowLevelMouseEventArgs(Object sender) : base(sender) { }
    }
}
