using FFXIVTataruHelper.Services.GameMemory;

namespace FFXIVTataruHelper.Services.UI
{
    /// <summary>
    /// Whether the copy is on screen, and what it is dressed as.
    ///
    /// Kept apart from the window that does the showing, the way the placement
    /// and the hold are: the first line of a conversation once went up undrawn,
    /// because being dressed for a line and being on screen had been one
    /// question, and the first line's answer to it was "already dressed, so
    /// change nothing". Those are separate questions, and the one that decides
    /// the showing could not be watched from inside the window that shows.
    /// </summary>
    internal sealed class DialogueOverlayPresentation
    {
        private bool _shown;
        private DialogueSurface _dressedFor = DialogueSurface.Window;

        /// <summary>
        /// Puts the copy on screen dressed for a line drawn on the given
        /// surface.
        ///
        /// Reports whether it was off the screen and must be shown now, and
        /// separately whether its dress changed. A line already on screen in
        /// the right dress asks for neither.
        /// </summary>
        public bool Present(DialogueSurface surface, out bool restyled)
        {
            restyled = surface != _dressedFor;

            if (restyled)
            {
                _dressedFor = surface;
            }

            var mustShow = !_shown;
            _shown = true;
            return mustShow;
        }

        /// <summary>
        /// The copy leaves the screen. It keeps its dress: coming back is not
        /// a change of what it covers, only of whether it is on screen.
        /// </summary>
        public void Hide()
        {
            _shown = false;
        }
    }
}