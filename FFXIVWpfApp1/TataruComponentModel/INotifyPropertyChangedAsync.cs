// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIVTataruHelper.EventArguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVTataruHelper.TataruComponentModel
{
    public interface INotifyPropertyChangedAsync
    {
        event AsyncEventHandler<AsyncPropertyChangedEventArgs> AsyncPropertyChanged;
    }
}
