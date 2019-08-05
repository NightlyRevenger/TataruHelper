// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIITataruHelper.WinUtils;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace FFXIITataruHelper.Utils
{
    static public class TataruSingleInstance
    {
        public static readonly int WM_SHOWFIRSTINSTANCE = Win32Interfaces.RegisterWindowMessageM("WM_SHOWFIRSTINSTANCE|{0}", ProgramInfo.AssemblyGuid);

        private static Mutex mutex = null;


        public static bool IsOnlyInstance
        {
            get
            {
                bool onlyInstance = Start();

                if (onlyInstance == false)
                    ShowFirstInstance();

                return onlyInstance;
            }
        }

        private static bool Start()
        {
            bool onlyInstance = true;
            string mutexName = String.Format("Local\\{0}", ProgramInfo.AssemblyGuid);

            // if you want your app to be limited to a single instance
            // across ALL SESSIONS (multiple users & terminal services), then use the following line instead:
            // string mutexName = String.Format("Global\\{0}", ProgramInfo.AssemblyGuid);
            //Logger.WriteLog(ProgramInfo.AssemblyGuid);

            try
            {
                mutex = new Mutex(true, mutexName, out onlyInstance);
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
                onlyInstance = true;
            }

            //Logger.WriteLog("onlyInstance: " + Convert.ToString(onlyInstance));

            return onlyInstance;
        }

        static public void ShowFirstInstance()
        {
            try
            {
                Win32Interfaces.PostMessage(
                    (IntPtr)Win32Interfaces.HWND_BROADCAST,
                    WM_SHOWFIRSTINSTANCE,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }
        }

        static public void Stop()
        {
            try
            {
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }
        }
    }

    static public class ProgramInfo
    {
        static public string AssemblyGuid
        {
            get
            {
                string result = String.Empty;

                var domain = Assembly.GetEntryAssembly();
                var assembly = domain.GetType().Assembly;
                var attribute = (GuidAttribute)assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
                var id = attribute.Value;

                result = id;

                return result;
            }
        }

    }
}
