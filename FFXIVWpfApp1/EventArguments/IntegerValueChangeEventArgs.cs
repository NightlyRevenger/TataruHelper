// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;

namespace FFXIITataruHelper.EventArguments
{
    public class IntegerValueChangeEventArgs : TatruEventArgs
    {
        public int OldValue { get; internal set; }

        public int NewValue { get; internal set; }

        internal IntegerValueChangeEventArgs(Object sender) : base(sender) { }
    }
}
