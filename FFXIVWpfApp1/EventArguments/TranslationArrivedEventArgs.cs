// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Windows.Media;

namespace FFXIVTataruHelper.EventArguments
{
    public class TranslationArrivedEventArgs : TatruEventArgs
    {
        public string Text { get; internal set; }

        public int ErrorCode { get; internal set; }

        internal TranslationArrivedEventArgs(Object sender) : base(sender) { }
    }
}
