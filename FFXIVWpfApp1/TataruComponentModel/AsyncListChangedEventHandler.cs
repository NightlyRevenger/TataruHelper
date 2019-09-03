// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.ComponentModel;

namespace FFXIVTataruHelper.EventArguments
{
    public class AsyncListChangedEventHandler<T> : TatruEventArgs
    {
        public virtual ListChangedEventArgs ChangedEventArgs { get; internal set; }

        public virtual T ChangedElemnt { get; internal set; }

        internal AsyncListChangedEventHandler(Object sender) : base(sender) { }

        internal AsyncListChangedEventHandler(Object sender, T changedElement, ListChangedEventArgs changedEventArgs) : base(sender)
        {
            ChangedEventArgs = changedEventArgs;
            ChangedElemnt = changedElement;
        }
    }
}
