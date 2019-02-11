// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIITataruHelper.FFHandlers
{
    public struct FFChatMsg
    {
        public string Text { get; internal set; }
        public string Code { get; internal set; }
        public DateTime TimeStamp { get; internal set; }

        public FFChatMsg(string text, string code, DateTime timeStamp)
        {
            Text = text;
            Code = code;
            TimeStamp = timeStamp;
        }
    }
}
