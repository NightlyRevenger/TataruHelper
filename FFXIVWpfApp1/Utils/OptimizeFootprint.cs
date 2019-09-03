// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FFXIVTataruHelper
{
    public class OptimizeFootprint
    {
        [DllImport("KERNEL32.DLL", EntryPoint = "SetProcessWorkingSetSize", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool SetProcessWorkingSetSize32(IntPtr pProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize);

        [DllImport("KERNEL32.DLL", EntryPoint = "SetProcessWorkingSetSize", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static extern bool SetProcessWorkingSetSize64(IntPtr pProcess, long dwMinimumWorkingSetSize, long dwMaximumWorkingSetSize);

        bool _KeepWorking;

        CancellationTokenSource source;
        CancellationToken token;

        public OptimizeFootprint()
        {
            _KeepWorking = true;
        }

        public void Start()
        {
            Task.Factory.StartNew(async () =>
            {
                _KeepWorking = true;

                source = new CancellationTokenSource();
                token = source.Token;

                try
                {
                    await EntryPoint();
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                }
            }, TaskCreationOptions.LongRunning);
        }

        async Task EntryPoint()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                _KeepWorking = false;

            while (_KeepWorking)
            {
                if (_KeepWorking)
                {
                    try
                    {
                        await Task.Delay(10000, token);
                    }
                    catch (Exception) { }
                }

                if (_KeepWorking)
                {
                    try
                    {
                        FlushMemory();
                        MinimizeFootprint();
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(ex);
                    }
                }

                if (_KeepWorking)
                {
                    try
                    {
                        await Task.Delay(60000, token);
                    }
                    catch (Exception) { }
                }
            }
            source.Dispose();
        }

        public void Stop()
        {
            _KeepWorking = false;
            source.Cancel();
        }

        private void FlushMemory()
        {
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (IntPtr.Size == 8)
                {
                    SetProcessWorkingSetSize64(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
                }
                else
                {
                    SetProcessWorkingSetSize32(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
                }
            }
        }

        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        private void MinimizeFootprint()
        {
            using (var proc = Process.GetCurrentProcess())
            {
                EmptyWorkingSet(proc.Handle);
            }
        }
    }
}
