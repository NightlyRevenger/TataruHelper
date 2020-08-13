// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.ObjectModel;
using Translation;

namespace FFXIVTataruHelper.EventArguments
{
    public class TranslationEngineChangeEventArgs : TatruEventArgs
    {
        public int OldEngine { get; internal set; }

        public int NewEngine { get; internal set; }

        public ReadOnlyCollection<TranslatorLanguague> SupportedLanguages { get; internal set; }

        internal TranslationEngineChangeEventArgs(Object sender) : base(sender) { }
    }
}
