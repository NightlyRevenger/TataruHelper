// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;

namespace FFXIITataruHelper.EventArguments
{
    public class TimeSpanChangeEventArgs : TatruEventArgs
    {
        public TimeSpan OldValue { get; internal set; }

        public TimeSpan NewValue { get; internal set; }

        internal TimeSpanChangeEventArgs(Object sender) : base(sender) { }
    }
}
