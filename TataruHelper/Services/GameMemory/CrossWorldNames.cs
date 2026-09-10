using System;
using System.Collections.Generic;
using System.Text;

namespace FFXIVTataruHelper.Services.GameMemory
{
    /// <summary>
    /// Names of players from another world, which arrive with that world stuck
    /// to the end of them.
    ///
    /// Read off a live client on 2026-09-11, from an emote:
    ///
    ///   Cova Rae: Cova RaeLouisoix keeps time by swishing her baton.
    ///
    /// The game draws a little flower between "Rae" and "Louisoix". Sharlayan
    /// does not carry it, so the two arrive as one word, and a service made
    /// "Кова Раэлуисуа" of them - one Russian word where there were a name and
    /// a world.
    ///
    /// Both halves are kept from the service and the flower is put back
    /// between them afterwards, which is what the game itself shows.
    /// </summary>
    public static class CrossWorldNames
    {
        /// <summary>
        /// The flower. Icon 88 in the game's own sheet, which this application
        /// can draw because it reads that sheet already.
        /// </summary>
        public const int CrossWorldIcon = 88;

        /// <summary>
        /// Where a hidden name is put while a service has the line. Out of the
        /// private-use area, high above anything the game numbers an icon, and
        /// gone again before anything is drawn.
        /// </summary>
        private const char FirstMark = '\uF000';

        private const int MostHidden = 32;

        /// <summary>
        /// The line with every cross-world name taken out, and what was taken.
        ///
        /// A name is only found where the world is written straight onto it -
        /// no space - at the end of a word, with the world's capital where the
        /// join is. A world written with a space in front of it is left alone:
        /// Titan, Shiva and Odin are worlds, and they are all perfectly good
        /// surnames.
        /// </summary>
        public static string Hide(string line, IReadOnlyCollection<string> worlds, out List<string> hidden)
        {
            hidden = new List<string>();

            if (string.IsNullOrEmpty(line) || worlds == null || worlds.Count == 0)
            {
                return line ?? string.Empty;
            }

            var built = new StringBuilder(line.Length);
            var at = 0;

            while (at < line.Length)
            {
                var found = FindJoin(line, at, worlds, out var start, out var world);
                if (found < 0 || hidden.Count >= MostHidden)
                {
                    built.Append(line, at, line.Length - at);
                    break;
                }

                built.Append(line, at, start - at);
                built.Append((char)(FirstMark + hidden.Count));
                hidden.Add(line.Substring(start, found - start) +
                           GameIcons.Mark(CrossWorldIcon) + world);
                at = found + world.Length;
            }

            return built.ToString();
        }

        /// <summary>Puts back what was taken, the flower and all.</summary>
        public static string Show(string line, IReadOnlyList<string> hidden)
        {
            if (string.IsNullOrEmpty(line) || hidden == null || hidden.Count == 0)
            {
                return line ?? string.Empty;
            }

            var built = new StringBuilder(line.Length);

            for (var at = 0; at < line.Length; at++)
            {
                var c = line[at];
                var which = c - FirstMark;
                if (which < 0 || which >= hidden.Count)
                {
                    built.Append(c);
                    continue;
                }

                // A service is free to set its own words hard against the mark,
                // and Yandex does: it answered the emote above with
                // "...Odinнежно" - the world and the verb in one word. The
                // mark stood for a name, and a name is a word of its own.
                if (built.Length > 0 && char.IsLetterOrDigit(built[built.Length - 1]))
                {
                    built.Append(' ');
                }

                built.Append(hidden[which]);

                if (at + 1 < line.Length && char.IsLetterOrDigit(line[at + 1]))
                {
                    built.Append(' ');
                }
            }

            return built.ToString();
        }

        /// <summary>
        /// Where the next world is written onto the end of a name, and how much
        /// of what comes before it is the name.
        ///
        /// The name is the two words the game gives everybody - a forename and
        /// a surname - so what is taken is the word the world is stuck to and
        /// the word before it.
        /// </summary>
        private static int FindJoin(string line, int from, IReadOnlyCollection<string> worlds,
            out int nameStart, out string world)
        {
            nameStart = -1;
            world = string.Empty;

            for (var i = from + 1; i < line.Length; i++)
            {
                // The join: a small letter, then a capital, inside one word.
                if (!char.IsUpper(line[i]) || !char.IsLower(line[i - 1]))
                {
                    continue;
                }

                foreach (var candidate in worlds)
                {
                    if (!EndsAWordHere(line, i, candidate))
                    {
                        continue;
                    }

                    nameStart = StartOfName(line, i);
                    world = candidate;
                    return i;
                }
            }

            return -1;
        }

        private static bool EndsAWordHere(string line, int at, string world)
        {
            if (at + world.Length > line.Length ||
                string.CompareOrdinal(line, at, world, 0, world.Length) != 0)
            {
                return false;
            }

            var after = at + world.Length;
            return after >= line.Length || !char.IsLetter(line[after]);
        }

        /// <summary>
        /// The beginning of the name the world is stuck to: back over the word
        /// it joins, and over the word before that when there is one, since a
        /// character's name is two words.
        /// </summary>
        private static int StartOfName(string line, int join)
        {
            var start = join;
            while (start > 0 && char.IsLetter(line[start - 1]))
            {
                start--;
            }

            if (start >= 2 && line[start - 1] == ' ' && char.IsLetter(line[start - 2]))
            {
                var forename = start - 1;
                while (forename > 0 && (char.IsLetter(line[forename - 1]) || line[forename - 1] == '\''))
                {
                    forename--;
                }

                if (forename < start - 1 && char.IsUpper(line[forename]))
                {
                    return forename;
                }
            }

            return start;
        }
    }
}
