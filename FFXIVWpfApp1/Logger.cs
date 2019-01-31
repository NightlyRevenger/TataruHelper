// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FFXIITataruHelper
{
    public static class Logger
    {
        public static ConcurrentQueue<string> LogQueue = new ConcurrentQueue<string>();
        public static ConcurrentQueue<string> ConsoleLogQueue = new ConcurrentQueue<string>();

        static Mutex mut1 = new Mutex();
        static Mutex mut2 = new Mutex();

        public static void WriteLog(string InputString)
        {
            mut1.WaitOne();

            string res = "";

            string time = DateTime.Now.ToString();

            res = time + Environment.NewLine;
            res += InputString + Environment.NewLine;

            LogQueue.Enqueue(res);

            mut1.ReleaseMutex();
        }

        public static void WriteLog(object Input)
        {
            mut1.WaitOne();

            string InputString = Convert.ToString(Input);

            string res = "";

            string time = DateTime.Now.ToString();

            res = time + Environment.NewLine;
            res += InputString + Environment.NewLine;

            LogQueue.Enqueue(res);

            mut1.ReleaseMutex();
        }

        public static void WriteConsoleLog(string InputString)
        {
            mut2.WaitOne();

            string res = "";

            string time = DateTime.Now.ToString();

            res = time + Environment.NewLine;
            res += InputString + Environment.NewLine;

            ConsoleLogQueue.Enqueue(res);

            mut2.ReleaseMutex();
        }
    }
}
