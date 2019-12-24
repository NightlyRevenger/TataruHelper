using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater
{
    static class TaskExtensions
    {
        public static void Forget(this Task task)
        {
            task.ContinueWith(
                t => { },
                TaskContinuationOptions.OnlyOnFaulted);
        }

        public static void Forget(this Task task, ILog logger=null)
        {
            task.ContinueWith(
                t => { logger?.WriteLog(t?.Exception?.ToString() ?? "Exception" ); },
                TaskContinuationOptions.OnlyOnFaulted);
        }

        public static void EndWith(this Task task, Action _action)
        {
            task.ContinueWith(
                t => {  },
                TaskContinuationOptions.OnlyOnFaulted);

            task.ContinueWith(t => { _action(); });

            task.ContinueWith(
                t => { },
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
