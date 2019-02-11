// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FFXIITataruHelper.EventArguments;
using System.Threading;

namespace FFXIITataruHelper.WinUtils
{
    public class MouseHooker : IDisposable
    {
        public event AsyncEventHandler<LowLevelMouseEventArgs> LowLevelMouseEvent
        {
            add { this._LowLevelMouseEvent.Register(value); }
            remove { this._LowLevelMouseEvent.Unregister(value); }
        }
        private AsyncEvent<LowLevelMouseEventArgs> _LowLevelMouseEvent;

        public MouseHooker()
        {
            _proc = HookCallback;

            _hookID = SetHook(_proc);

            _LowLevelMouseEvent = new AsyncEvent<LowLevelMouseEventArgs>(EventErrorHandler, "LowLevelMouseEvent");
        }

        private LowLevelMouseProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                //return SetWindowsHookEx(WH_MOUSE, proc, GetModuleHandle(curModule.ModuleName), (uint)Thread.CurrentThread.ManagedThreadId);
                //return SetWindowsHookEx(WH_MOUSE, proc, GetModuleHandle(curModule.ModuleName), (uint)AppDomain.GetCurrentThreadId());
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public void UnHook()
        {
            try
            {
                UnhookWindowsHookEx(_hookID);
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [Flags]
        public enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }

        public enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                var ea = new LowLevelMouseEventArgs(this)
                {
                    MouseMessages = (MouseMessages)wParam,
                    MouseEventFlags = hookStruct
                };
                _LowLevelMouseEvent.InvokeAsync(ea);

            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int WH_MOUSE_LL = 14;
        private const int WH_MOUSE = 7;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public void Dispose()
        {
            UnHook();
        }

        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            Logger.WriteLog(text);
        }

        ~MouseHooker()
        {
            Dispose();
        }
    }
}
