// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIITataruHelper.EventArguments
{
    public class WindowStateChangeEventArgs : TatruEventArgs
    {
        public System.Windows.WindowState OldWindowState { get; internal set; }

        public System.Windows.WindowState NewWindowState { get; internal set; }

        public string Text { get; internal set; }

        public bool IsRunningOld { get; internal set; }

        public bool IsRunningNew { get; internal set; }

        internal WindowStateChangeEventArgs(Object sender) : base(sender) { }
    }
}
