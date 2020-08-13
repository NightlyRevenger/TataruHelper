// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translation.Papago
{
    class PapagoTranslationRequest
    {
        public string deviceId { get; set; }
        public bool dict { get; set; }
        public int dictDisplay { get; set; }
        public bool honorific { get; set; }
        public bool instant { get; set; }
        public bool paging { get; set; }
        public string source { get; set; }
        public string target { get; set; }
        public string text { get; set; }
    }
}
