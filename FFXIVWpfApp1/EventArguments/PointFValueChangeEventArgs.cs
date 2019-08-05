// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Drawing;

namespace FFXIITataruHelper.EventArguments
{
    public class PointDValueChangeEventArgs : TatruEventArgs
    {
        public PointD OldValue { get; internal set; }

        public PointD NewValue { get; internal set; }

        internal PointDValueChangeEventArgs(Object sender) : base(sender) { }
    }
}
