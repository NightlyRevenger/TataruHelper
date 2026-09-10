using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace FFXIVTataruHelper.Services.GameMemory
{
    /// <summary>
    /// The names of the game's worlds, read out of the sheet it keeps them in.
    ///
    /// Wanted because a player from another world has that world's name written
    /// straight onto the end of theirs. The game draws a little flower between
    /// the two; by the time the line reaches this application the flower is
    /// gone and the two are one word - "Cova RaeLouisoix" - which a translation
    /// service turns into one Russian word and hands back as nonsense.
    ///
    /// Read from the player's own installation rather than kept in a list here:
    /// the game gains worlds, and a list would be wrong the week it did. The
    /// sheet also says which of its rows are real, public worlds - it holds
    /// data-centre names and development leftovers besides - so the game
    /// decides that too.
    ///
    /// The parsing is kept apart from the reading so it can be checked against
    /// a handful of bytes, without a game.
    /// </summary>
    public static class GameWorldSheet
    {
        private const int HeaderBytes = 0x20;

        private const int ColumnBytes = 4;

        /// <summary>A column holding a string.</summary>
        private const int StringColumn = 0;

        /// <summary>
        /// A column holding one bit of a byte. The kind says which bit: 25 is
        /// the first, 26 the second, and so on. The one this wants is the flag
        /// the sheet sets on a world people can actually be on.
        /// </summary>
        private const int FirstPackedBool = 25;

        private const int PublicWorldBit = 1;

        /// <summary>
        /// Every public world the sheet names. Empty for anything that is not
        /// the sheet - a file that has moved on to another format says so by
        /// being unreadable, and unreadable has to mean no worlds rather than
        /// wrong ones.
        /// </summary>
        public static HashSet<string> Read(byte[] header, byte[] rows)
        {
            var worlds = new HashSet<string>(StringComparer.Ordinal);

            if (header == null || rows == null ||
                header.Length < HeaderBytes || rows.Length < HeaderBytes ||
                !Is(header, "EXHF") || !Is(rows, "EXDF"))
            {
                return worlds;
            }

            var rowSize = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(0x06));
            var columnCount = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(0x08));

            if (rowSize == 0 || columnCount == 0 ||
                HeaderBytes + columnCount * ColumnBytes > header.Length)
            {
                return worlds;
            }

            var nameAt = -1;
            var publicAt = -1;

            for (var i = 0; i < columnCount; i++)
            {
                var kind = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(HeaderBytes + i * ColumnBytes));
                var at = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(HeaderBytes + i * ColumnBytes + 2));

                if (kind == StringColumn && nameAt < 0)
                {
                    nameAt = at;
                }

                if (kind == FirstPackedBool + PublicWorldBit)
                {
                    publicAt = at;
                }
            }

            if (nameAt < 0)
            {
                return worlds;
            }

            var indexSize = BinaryPrimitives.ReadUInt32BigEndian(rows.AsSpan(0x08));
            var indexed = (int)(indexSize / 8);

            for (var i = 0; i < indexed; i++)
            {
                var entry = HeaderBytes + i * 8;
                if (entry + 8 > rows.Length)
                {
                    break;
                }

                var rowAt = (int)BinaryPrimitives.ReadUInt32BigEndian(rows.AsSpan(entry + 4));
                var fixedAt = rowAt + 6;
                if (fixedAt + rowSize > rows.Length)
                {
                    continue;
                }

                if (publicAt >= 0 && publicAt < rowSize)
                {
                    var flags = rows[fixedAt + publicAt];
                    if (((flags >> PublicWorldBit) & 1) == 0)
                    {
                        continue;
                    }
                }

                var name = StringAt(rows, fixedAt, rowSize, nameAt);
                if (LooksLikeAWorldName(name))
                {
                    worlds.Add(name);
                }
            }

            return worlds;
        }

        /// <summary>
        /// A world is one word of letters beginning with a capital. Asked even
        /// of rows the sheet calls public, because the shape is what the
        /// splitting relies on - and asked instead of the flag when a future
        /// sheet no longer carries one.
        /// </summary>
        internal static bool LooksLikeAWorldName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 3 || name.Length > 20)
            {
                return false;
            }

            if (!char.IsUpper(name[0]))
            {
                return false;
            }

            foreach (var c in name)
            {
                if (!char.IsLetter(c))
                {
                    return false;
                }
            }

            return true;
        }

        private static string StringAt(byte[] rows, int fixedAt, int rowSize, int column)
        {
            if (column + 4 > rowSize || fixedAt + column + 4 > rows.Length)
            {
                return string.Empty;
            }

            var strings = fixedAt + rowSize;
            var start = strings + (int)BinaryPrimitives.ReadUInt32BigEndian(rows.AsSpan(fixedAt + column));
            if (start < 0 || start >= rows.Length)
            {
                return string.Empty;
            }

            var end = Array.IndexOf(rows, (byte)0, start);
            if (end < 0)
            {
                end = rows.Length;
            }

            return end > start ? Encoding.UTF8.GetString(rows, start, end - start) : string.Empty;
        }

        private static bool Is(byte[] data, string magic)
        {
            for (var i = 0; i < magic.Length; i++)
            {
                if (data[i] != (byte)magic[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
