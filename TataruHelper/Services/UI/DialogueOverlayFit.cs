using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace FFXIVTataruHelper.Services.UI
{
    /// <summary>
    /// The largest a line can be set and still fit the box the game drew.
    ///
    /// The game wraps its own line to its own box, in the language it was
    /// written in. The translation is another language and another length -
    /// Russian runs a good deal longer than English - so a copy set at the
    /// game's own size has the tail of a long line hanging out of the bottom
    /// of the box, where it cannot be read at all. Reported on 2026-09-10 from
    /// a conversation about what becomes of the dead, where a paragraph was cut
    /// off mid-sentence.
    ///
    /// Kept apart from the window that draws, the way the placement and the
    /// hold are: this can be checked against a sentence and a rectangle,
    /// without a game or a screen.
    /// </summary>
    internal static class DialogueOverlayFit
    {
        /// <summary>
        /// Below this the copy is doing the reader no favours, and a line that
        /// will not fit even here is better cut off than made unreadable.
        /// </summary>
        public const double SmallestReadable = 9;

        /// <summary>
        /// Down by a twentieth at a time. Smaller steps take longer to arrive
        /// at the same place; larger ones overshoot into text needlessly small
        /// for a box it would have fitted.
        /// </summary>
        private const double StepDown = 0.95;

        private const int MostAttempts = 24;

        public static double LargestThatFits(
            string words, Size room, Typeface typeface, double pixelsPerDip, double wanted)
        {
            if (string.IsNullOrEmpty(words) || typeface == null ||
                room.Width <= 1 || room.Height <= 1 || !(wanted > 0))
            {
                return wanted;
            }

            if (!(pixelsPerDip > 0))
            {
                pixelsPerDip = 1;
            }

            var size = wanted;

            for (var attempt = 0; attempt < MostAttempts && size > SmallestReadable; attempt++)
            {
                if (Fits(words, room, typeface, pixelsPerDip, size))
                {
                    return size;
                }

                size *= StepDown;
            }

            return Math.Max(SmallestReadable, size);
        }

        private static bool Fits(string words, Size room, Typeface typeface, double pixelsPerDip, double size)
        {
            var laid = new FormattedText(
                words,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                size,
                Brushes.Black,
                pixelsPerDip)
            {
                MaxTextWidth = room.Width
            };

            return laid.Height <= room.Height;
        }
    }
}
