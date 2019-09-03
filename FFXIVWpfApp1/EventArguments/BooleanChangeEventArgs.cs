// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;

namespace FFXIVTataruHelper.EventArguments
{
    public class BooleanChangeEventArgs : TatruEventArgs
    {
        public bool OldValue { get; internal set; }

        public bool NewValue { get; internal set; }

        internal BooleanChangeEventArgs(Object sender) : base(sender) { }
    }
}
