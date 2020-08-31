using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Translation.HttpUtils
{
    public class HttpILogWrapper : HttpUtilities.ILog
    {
        ILog _Logger = null;
        public HttpILogWrapper(ILog log)
        {
            _Logger = log;
        }

        public void WriteLog(string InputString, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0) => _Logger.WriteLog(InputString, memberName, sourceLineNumber);
    }
}
