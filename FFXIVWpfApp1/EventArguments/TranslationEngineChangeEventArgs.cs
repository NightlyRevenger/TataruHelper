// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIITataruHelper.Translation;
using System;
using System.Collections.ObjectModel;

namespace FFXIITataruHelper.EventArguments
{
    public class TranslationEngineChangeEventArgs : TatruEventArgs
    {
        public int OldEngine { get; internal set; }

        public int NewEngine { get; internal set; }

        public ReadOnlyCollection<TranslatorLanguague> SupportedLanguages { get; internal set; }

        internal TranslationEngineChangeEventArgs(Object sender) : base(sender) { }
    }
}
