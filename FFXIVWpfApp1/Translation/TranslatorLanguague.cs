// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIITataruHelper.Translation
{
    public class TranslatorLanguague
    {
        public string ShownName { get; private set; }
        public string NativeName { get; private set; }
        public string LanguageCode { get; private set; }

        public TranslatorLanguague(string shownName, string nativeName, string languageCode)
        {
            ShownName = shownName;
            NativeName = nativeName;
            LanguageCode = languageCode;
        }
    }
}
