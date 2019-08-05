// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIITataruHelper.EventArguments;
using FFXIITataruHelper.Utils;
using System;
using System.Windows;
using System.Windows.Interop;

namespace FFXIITataruHelper.WinUtils
{
    class WinMessagesHandler
    {
        #region **Events.

        public event AsyncEventHandler<BooleanChangeEventArgs> ShowFirstInstance
        {
            add { this._ShowFirstInstance.Register(value); }
            remove { this._ShowFirstInstance.Unregister(value); }
        }
        private AsyncEvent<BooleanChangeEventArgs> _ShowFirstInstance;

        #endregion

        #region **Properties.

        private HwndSource _HwndSource;
        private HwndSourceHook _Hook;

        #endregion

        public WinMessagesHandler(Window window)
        {
            _ShowFirstInstance = new AsyncEvent<BooleanChangeEventArgs>(EventErrorHandler, "ShowFirstInstance");

            _HwndSource = (HwndSource)HwndSource.FromVisual(window);

            _Hook = new HwndSourceHook(WndProc);

            _HwndSource.AddHook(_Hook);
        }

        #region **Listen to Windows messages.
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == TataruSingleInstance.WM_SHOWFIRSTINSTANCE)
            {
                var ea = new BooleanChangeEventArgs(this)
                {
                    OldValue = false,
                    NewValue = true
                };

                _ShowFirstInstance.InvokeAsync(ea);
            }

            return IntPtr.Zero;
        }
        #endregion

        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            Logger.WriteLog(text);
        }

    }
}
