using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.EventArguments
{
    public class UpdatingState : UpdaterEventArgs
    {
        public string Text { get; internal set; }

        public UpdateSate UpdateSate { get; internal set; }

        public UpdatingState(Object sender) : base(sender) { }
    }

    public enum UpdateSate
    {
        InitializingUpdater,
        LookingForUpdates,
        NoUpdatesFound,
        UpdateFound,
        DownloadingUpdate,
        UpdateDownloaded,
        UpdatingFinished,
        ReadyToRestart,
    }
}
