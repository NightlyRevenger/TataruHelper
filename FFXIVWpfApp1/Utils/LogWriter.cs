// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FFXIVTataruHelper
{
    class LogWriter
    {
        //Thread _WriteThread;
        const int _MaxLogFileSize = 5242880;
        //const int _MaxLogFileSize = 10485760;
        //const int _MaxLogFileSize = 2000;//

        bool _KeepWorking;
        TextWriter _logTextWriter;
        StreamWriter _logStreamWriter;

        TextWriter chatsw = null;

        string LogFileName = @"Log.txt";
        string BackUpLogFileName = @"Log_old.txt";


        string ChatLogFileName = @"ChatLog.txt";

        public LogWriter()
        {
            //_WriteThread = new Thread(EntryPoint);
            _KeepWorking = true;

            _logStreamWriter = new StreamWriter(LogFileName, true);
            _logTextWriter = _logStreamWriter;
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
            while (_KeepWorking)
            {
                dequeueFalg = false;
                if (Logger.LogQueue.TryDequeue(out str))
                {
                    _logTextWriter.WriteLine(str);
                    _logTextWriter.Flush();
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
                        chatsw = new StreamWriter(ChatLogFileName, true);

                    chatsw.WriteLine(str);
                    chatsw.Flush();
                    dequeueFalg = true;
                }

                if (!dequeueFalg)
                {
                    //Thread.Sleep(33);
                    //_KeepWorking = false;
                    SpinWait.SpinUntil(() => (Logger.LogQueue.IsEmpty == false || Logger.ConsoleLogQueue.IsEmpty == false || Logger.ChatLogQueue.IsEmpty == false) || !_KeepWorking);
                    if (_KeepWorking)
                    {
                        LimitLogFileSize();
                    }
                }
            }

            ReleaseResources();
        }

        private void LimitLogFileSize()
        {
            if (_logStreamWriter != null && _logTextWriter != null)
            {
                if (_logStreamWriter.BaseStream.Length >= _MaxLogFileSize)
                {
                    try
                    {
                        _logTextWriter.Flush();
                        _logTextWriter.Close();
                        _logTextWriter.Dispose();

                        _logStreamWriter.Close();
                        _logStreamWriter.Dispose();

                        if (File.Exists(BackUpLogFileName))
                            File.Delete(BackUpLogFileName);

                        if (File.Exists(LogFileName))
                        {
                            File.Copy(LogFileName, BackUpLogFileName);
                            File.Delete(LogFileName);
                        }

                        _logStreamWriter = new StreamWriter(LogFileName, true);
                        _logTextWriter = _logStreamWriter;
                    }
                    catch (Exception) { }
                }
            }
        }

        void ReleaseResources()
        {
            try
            {
                if (_logTextWriter != null)
                {
                    _logTextWriter.Flush();
                    _logTextWriter.Close();
                }

                if (chatsw != null)
                {
                    chatsw.Flush();
                    chatsw.Close();
                }

            }
            catch { }
        }

        public void Stop()
        {
            _KeepWorking = false;
        }

        ~LogWriter()
        {
            _KeepWorking = false;
            ReleaseResources();
        }


    }
}
