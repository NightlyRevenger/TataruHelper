// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVTataruHelper.EventArguments
{
    public class AsyncPropertyChangedEventArgs : TatruEventArgs
    {
        //
        // Summary:
        //     Gets the name of the property that changed.
        //
        // Returns:
        //     The name of the property that changed.
        public virtual string PropertyName { get; internal set; }

        internal AsyncPropertyChangedEventArgs(Object sender) : base(sender) { }

        internal AsyncPropertyChangedEventArgs(Object sender, string propertyName) : base(sender)
        {
            PropertyName = propertyName;
        }
    }
}
