// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace FFXIITataruHelper.EventArguments
{
    public class ColorListChangeEventArgs : TatruEventArgs
    {
        public List<Color> Colors { get; internal set; }

        public int ColorsId { get; internal set; }

        internal ColorListChangeEventArgs(Object sender) : base(sender) { }
    }
}
