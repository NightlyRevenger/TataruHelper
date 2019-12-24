using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Translation;

namespace FFXIVTataruHelper.Utils
{
    class LoggerWrapper : ILog, Updater.ILog
    {
        public LoggerWrapper() { }

        public void WriteLog(string InputString, string memberName = "", int sourceLineNumber = 0)
        {
            Logger.WriteLog(InputString, memberName, sourceLineNumber);
        }
    }
}
