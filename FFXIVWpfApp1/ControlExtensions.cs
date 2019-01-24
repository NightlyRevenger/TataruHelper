// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FFXIITataruHelper
{
    public static class ControlExtensions
    {
        /// <summary>
        /// Executes the Action asynchronously on the UI thread, does not block execution on the calling thread.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="code"></param>
        public static void UIThread(this Window @this, Action code)
        {

            if (!(@this.Dispatcher.CheckAccess()))
            {
                @this.Dispatcher.Invoke(code);
            }
            else
            {
                code.Invoke();
            }
        }

        public static void UIThreadAsync(this Window @this, Action code)
        {

            if (!(@this.Dispatcher.CheckAccess()))
            {
                @this.Dispatcher.BeginInvoke(code);
            }
            else
            {
                code.Invoke();
            }
        }
    }
}
