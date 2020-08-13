using System.Runtime.CompilerServices;

namespace Updater
{
    public interface ILog
    {
        void WriteLog(string InputString, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0);
    }
}
