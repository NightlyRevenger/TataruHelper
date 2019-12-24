// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translation.Papago
{
    public class PapagoResponse
    {
        public string srcLangType { get; set; }
        public string tarLangType { get; set; }
        public string translatedText { get; set; }
        public TlitSrc tlitSrc { get; set; }
        public int delay { get; set; }
        public int delaySmt { get; set; }
        public LangDetection langDetection { get; set; }

        public class TlitResult
        {
            public string token { get; set; }
            public string phoneme { get; set; }
        }

        public class Message
        {
            public List<TlitResult> tlitResult { get; set; }
        }

        public class TlitSrc
        {
            public Message message { get; set; }
        }

        public class Nbest
        {
            public string lang { get; set; }
            public double prob { get; set; }
        }

        public class LangDetection
        {
            public List<Nbest> nbests { get; set; }
        }
    }
}
