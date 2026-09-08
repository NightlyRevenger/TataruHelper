using System;

namespace FFXIVTataruHelper.Services.UI
{
    /// <summary>
    /// Whether there is a copy of the game's dialogue box at all, and which
    /// chat window fills it.
    ///
    /// There is one game and one dialogue box, so there is one copy of it - but
    /// the translation is done per chat window, each with its own engine and
    /// its own pair of languages, and every one of them is offered every line.
    /// Left to themselves they would each raise a copy and stack them over the
    /// same box.
    ///
    /// So the first window to ask takes the copy and the rest are told no. It
    /// gives up the claim when it closes, and the next window to ask takes it
    /// over - which is what happens when the window holding the claim is the
    /// one the player closes.
    /// </summary>
    public sealed class DialogueOverlayHost
    {
        private readonly object _gate = new object();

        private WeakReference _owner;

        private bool _isWanted;

        /// <summary>
        /// Raised when the setting is turned on or off, so a window already
        /// open puts its copy up or takes it down there and then rather than
        /// at whatever is said next.
        /// </summary>
        public event EventHandler WantedChanged;

        /// <summary>Whether the player has asked for the copy at all.</summary>
        public bool IsWanted
        {
            get
            {
                lock (_gate)
                {
                    return _isWanted;
                }
            }

            set
            {
                lock (_gate)
                {
                    if (_isWanted == value)
                    {
                        return;
                    }

                    _isWanted = value;
                }

                WantedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Whether this window is the one that fills the copy, claiming it if
        /// nobody holds it.
        /// </summary>
        public bool BelongsTo(object window)
        {
            if (window == null)
            {
                return false;
            }

            lock (_gate)
            {
                var held = _owner?.Target;

                // A window that was closed without saying so - or collected -
                // holds nothing. Asking the reference rather than trusting the
                // release keeps one crash from costing the copy for the session.
                if (held == null || (_owner != null && !_owner.IsAlive))
                {
                    _owner = new WeakReference(window);
                    return true;
                }

                return ReferenceEquals(held, window);
            }
        }

        /// <summary>Gives up the claim, if this window is holding it.</summary>
        public void Release(object window)
        {
            lock (_gate)
            {
                if (window != null && ReferenceEquals(_owner?.Target, window))
                {
                    _owner = null;
                }
            }
        }
    }
}
