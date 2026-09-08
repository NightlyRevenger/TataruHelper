using System;

using FFXIVTataruHelper.Services.GameMemory;

namespace FFXIVTataruHelper.Services.UI
{
    /// <summary>
    /// Whether the game's box is opening, sitting still, or closing - and so
    /// whether a copy of it belongs on screen this moment.
    ///
    /// The game does not show and hide its dialogue window, it grows and
    /// shrinks it. Measured off a running client: the window opens through
    /// three frames at 0.85, 0.98 and 1.0 of its size in about eighty
    /// milliseconds, sits at 1.0 for as long as the player reads, and closes
    /// back down the same way. Clicking on through a conversation does not
    /// animate at all - the window stays at full size from the first line to
    /// the last.
    ///
    /// So the two directions want opposite things. Growing is the window
    /// opening, and a copy that grows with it covers the original from the
    /// first frame instead of letting it be read. Shrinking is the window
    /// closing, and a copy that follows it down is a box that jumps smaller and
    /// then vanishes, which is what it did.
    ///
    /// Kept apart from the window that draws, the way the placement and the
    /// hold are.
    /// </summary>
    internal sealed class DialogueOverlayMotion
    {
        /// <summary>
        /// How long a smaller box has to stay smaller before it is taken for
        /// the size the box is now rather than a window on its way out. This is
        /// what happens when the player changes the interface scale mid
        /// conversation: without it the copy would sit out the rest of the
        /// conversation waiting for a size that is not coming back.
        /// </summary>
        public static readonly TimeSpan Settles = TimeSpan.FromMilliseconds(400);

        /// <summary>
        /// Half a pixel. The client reports the box in floating point and the
        /// last digits wander, so "the same size" cannot mean equal.
        /// </summary>
        private const double SameWidth = 0.5;

        private DialogueSurface _on = DialogueSurface.None;
        private double _widest;
        private DateTime _smallerSince = DateTime.MinValue;

        public bool ShouldDraw(DialogueSurface surface, double width, DateTime nowUtc)
        {
            // The widest is remembered per surface. A cutscene subtitle is the
            // width of the screen and a dialogue box is not, so measured
            // against one another the box always looks like a window still
            // closing - and a conversation following a subtitle would go
            // uncovered from beginning to end.
            if (surface != _on)
            {
                _on = surface;
                _widest = 0;
                _smallerSince = DateTime.MinValue;
            }

            if (width > _widest + SameWidth)
            {
                // Opening, or the first sighting. Followed rather than waited
                // out: the eighty milliseconds spent waiting are eighty
                // milliseconds of the original being readable.
                _widest = width;
                _smallerSince = DateTime.MinValue;
                return true;
            }

            if (width > _widest - SameWidth)
            {
                _smallerSince = DateTime.MinValue;
                return true;
            }

            if (_smallerSince == DateTime.MinValue)
            {
                _smallerSince = nowUtc;
                return false;
            }

            if (nowUtc - _smallerSince < Settles)
            {
                return false;
            }

            // It has stayed smaller. Not a window closing, then - the box is
            // this size now.
            _widest = width;
            _smallerSince = DateTime.MinValue;
            return true;
        }

        /// <summary>
        /// Forgets the size, for when the conversation ends. The next one is
        /// measured rather than assumed to open to the same width.
        /// </summary>
        public void Forget()
        {
            _on = DialogueSurface.None;
            _widest = 0;
            _smallerSince = DateTime.MinValue;
        }
    }
}
