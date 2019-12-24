using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.EventArguments
{
    public class UpdaterEventArgs : System.EventArgs
    {
        public Object Sender { get; internal set; }

        public UpdaterEventArgs(Object sender)
        {
            Sender = sender;
        }
    }
}
