// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Translation
{
    public interface ILog
    {
        void WriteLog(string InputString, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0);
    }
}
