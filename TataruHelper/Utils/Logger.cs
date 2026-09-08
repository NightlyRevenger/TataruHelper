using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FFXIVTataruHelper
{
    public static class Logger
    {
        public static ConcurrentQueue<string> LogQueue = new ConcurrentQueue<string>();
        public static ConcurrentQueue<string> ConsoleLogQueue = new ConcurrentQueue<string>();
        public static ConcurrentQueue<string> ChatLogQueue = new ConcurrentQueue<string>();
        public static ConcurrentQueue<string> RawDialogLogQueue = new ConcurrentQueue<string>();

        public static volatile bool RawDialogLogEnabled;

        internal static readonly AutoResetEvent QueueSignal = new AutoResetEvent(false);


        public static void WriteLog(string InputString,
            //[CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            WriteInnerLog(InputString, memberName, sourceLineNumber);
        }

        public static void WriteLog(object Input,
            //[CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            WriteInnerLog(Convert.ToString(Input), memberName, sourceLineNumber);
        }

        private static void WriteInnerLog(string InputString, string memberName, int sourceLineNumber)
        {
            string res = string.Empty;

            string time = DateTime.Now.ToString();

            res = time + Environment.NewLine;

            //res += sourceFilePath + Environment.NewLine;
            res += "Member name:" + memberName + Environment.NewLine;
            res += "Source Line Number: " + Convert.ToString(sourceLineNumber) + Environment.NewLine;

            res += InputString + Environment.NewLine;

            LogQueue.Enqueue(res);
            QueueSignal.Set();
        }

        public static void WriteConsoleLog(string InputString)
        {
            string res = "";

            string time = DateTime.Now.ToString();

            res = time + Environment.NewLine;
            res += InputString + Environment.NewLine;

            ConsoleLogQueue.Enqueue(res);
            QueueSignal.Set();
        }

        public static void WriteChatLog(string InputString)
        {
            string res = "";

            //string time = DateTime.Now.ToString();

            //res = time + Environment.NewLine;
            res += InputString; // + Environment.NewLine;

            ChatLogQueue.Enqueue(res);
            QueueSignal.Set();
        }

        public static void WriteRawDialogLog(string InputString)
        {
            if (!RawDialogLogEnabled)
                return;

            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            RawDialogLogQueue.Enqueue(time + " " + InputString);
            QueueSignal.Set();
        }
    }
}