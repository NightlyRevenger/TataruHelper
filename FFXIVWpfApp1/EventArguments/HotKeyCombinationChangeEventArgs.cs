// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIITataruHelper.WinUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIITataruHelper.EventArguments
{
    public class HotKeyCombinationChangeEventArgs : TatruEventArgs
    {
        public HotKeyCombination OldHotKeyCombination;

        public HotKeyCombination NewHotKeyCombination;

        internal HotKeyCombinationChangeEventArgs(Object sender) : base(sender) { }
    }
}
