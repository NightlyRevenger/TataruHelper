// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Windows.Media;

namespace FFXIVTataruHelper.EventArguments
{
    public class ColorChangeEventArgs : TatruEventArgs
    {
        public Color OldColor { get; internal set; }

        public Color NewColor { get; internal set; }

        public int ColorId { get; internal set; }

        internal ColorChangeEventArgs(Object sender) : base(sender) { }
    }
}
