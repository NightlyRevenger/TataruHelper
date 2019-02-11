// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FFXIITataruHelper
{
    class LogWriter
    {
        //Thread _WriteThread;
        bool _keepWorking;
        TextWriter tsw;

        TextWriter chatsw = null;

        public LogWriter()
        {
            //_WriteThread = new Thread(EntryPoint);
            _keepWorking = true;

            tsw = new StreamWriter(@"Log.txt", true);
        }

        public void StartWriting()
        {
            //_WriteThread.Start();

            Task.Factory.StartNew(() =>
            {
                try
                {
                    EntryPoint();
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                }

            }, TaskCreationOptions.LongRunning);

        }

        private void EntryPoint()
        {
            Logger.WriteLog("Started Logging");

            string str = null;

            bool dequeueFalg = false;
            while (_keepWorking)
            {
                dequeueFalg = false;
                if (Logger.LogQueue.TryDequeue(out str))
                {
                    tsw.WriteLine(str);
                    tsw.Flush();
                    dequeueFalg = true;
                }
                if (Logger.ConsoleLogQueue.TryDequeue(out str))
                {
                    Console.WriteLine(str);
                    dequeueFalg = true;
                }

                if (Logger.ChatLogQueue.TryDequeue(out str))
                {
                    if (chatsw == null)
                        chatsw = new StreamWriter(@"ChatLog.txt", true);

                    chatsw.WriteLine(str);
                    chatsw.Flush();
                    dequeueFalg = true;
                }

                if (!dequeueFalg)
                {
                    //Thread.Sleep(33);
                    SpinWait.SpinUntil(() => Logger.LogQueue.IsEmpty == false || Logger.ConsoleLogQueue.IsEmpty == false || Logger.ChatLogQueue.IsEmpty == false);
                }

            }

        }

        public void Stop()
        {
            SpinWait.SpinUntil(() => Logger.LogQueue.IsEmpty == true && Logger.ConsoleLogQueue.IsEmpty == true, GlobalSettings.SpiWaitTimeOutMS);

            _keepWorking = false;

            try
            {
                tsw.Flush();
                tsw.Close();

                if (chatsw != null)
                {
                    chatsw.Flush();
                    chatsw.Close();
                }

            }
            catch { }
        }

        ~LogWriter()
        {
            try
            {
                _keepWorking = false;
                tsw.Flush();
                tsw.Close();

                if (chatsw != null)
                {
                    chatsw.Flush();
                    chatsw.Close();
                }
            }
            catch { }
        }
    }
}
