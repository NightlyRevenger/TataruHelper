// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVTataruHelper
{
    static class TaskExtensions
    {
        public static void Forget(this Task task)
        {
            task.ContinueWith(
                t => { Logger.WriteLog(t.Exception); },
                TaskContinuationOptions.OnlyOnFaulted);
        }

        public static void EndWith(this Task task, Action _action)
        {
            task.ContinueWith(
                t => { Logger.WriteLog(t.Exception); },
                TaskContinuationOptions.OnlyOnFaulted);

            task.ContinueWith(t => { _action(); });

            task.ContinueWith(
                t => { Logger.WriteLog(t.Exception); },
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
