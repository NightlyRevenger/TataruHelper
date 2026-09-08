using System;
using System.Collections.Generic;

namespace FFXIVTataruHelper.Services.GameMemory
{
    /// <summary>
    /// Where each of the game's little in-sentence pictures sits on the sheet
    /// they are all drawn from.
    ///
    /// The game keeps this as <c>common/font/gfdata.gfd</c>: sixteen bytes of
    /// header and then one entry of sixteen bytes per icon - its id, then where
    /// it is on the sheet and how big it is. Read off a live client on
    /// 2026-09-08 it held 188 icons, and icon 79 - the mentor's crown for
    /// combat - was twenty by twenty at 208,60.
    ///
    /// Parsed here, apart from anything that reads files or draws, so the
    /// format can be checked against a handful of bytes.
    /// </summary>
    public static class GameIconSheet
    {
        private const int HeaderBytes = 16;

        private const int EntryBytes = 16;

        public readonly struct Place
        {
            public Place(int left, int top, int width, int height)
            {
                Left = left;
                Top = top;
                Width = width;
                Height = height;
            }

            public int Left { get; }

            public int Top { get; }

            public int Width { get; }

            public int Height { get; }

            /// <summary>
            /// False for an entry that describes no picture. The table has
            /// gaps: an id the game has stopped using keeps its row and is
            /// given no size.
            /// </summary>
            public bool IsDrawn => Width > 0 && Height > 0;
        }

        /// <summary>
        /// Every icon the table names, by id. An empty table for anything that
        /// is not one - a file that has moved on to another format says so by
        /// being unreadable, and unreadable means no icons rather than wrong
        /// ones.
        /// </summary>
        public static Dictionary<int, Place> Read(byte[] gfd)
        {
            var places = new Dictionary<int, Place>();
            if (gfd == null || gfd.Length < HeaderBytes + EntryBytes)
            {
                return places;
            }

            for (var at = HeaderBytes; at + EntryBytes <= gfd.Length; at += EntryBytes)
            {
                var id = BitConverter.ToUInt16(gfd, at);
                if (id == 0)
                {
                    continue;
                }

                var place = new Place(
                    BitConverter.ToUInt16(gfd, at + 2),
                    BitConverter.ToUInt16(gfd, at + 4),
                    BitConverter.ToUInt16(gfd, at + 6),
                    BitConverter.ToUInt16(gfd, at + 8));

                if (place.IsDrawn)
                {
                    places[id] = place;
                }
            }

            return places;
        }
    }
}
