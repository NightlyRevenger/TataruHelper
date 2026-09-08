using System.Text;

namespace FFXIVTataruHelper.Services.GameMemory
{
    /// <summary>
    /// The little pictures the game puts in the middle of a sentence - the
    /// mentor crowns, the new-adventurer leaf, the controller buttons - and how
    /// they are carried through an application that deals in text.
    ///
    /// The game writes one as a payload in the line: <c>02 12 02 50 03</c>,
    /// which is "icon 79". Everything between <c>02</c> and <c>03</c> used to be
    /// thrown away, which is why a line arrived reading "Mentor symbols are as
    /// follows: : Expert in PvE combat" with a hole where each crown should be.
    ///
    /// An icon is kept as a single character out of the private-use area, one
    /// codepoint per icon. A character rather than a bracketed word because it
    /// has to survive a translation engine intact: engines rewrite words and
    /// move them about, and a lone symbol they do not recognise is the thing
    /// they are likeliest to hand back untouched and in place.
    /// </summary>
    public static class GameIcons
    {
        /// <summary>
        /// Where the icons live. The private-use area runs from U+E000 to
        /// U+F8FF and belongs to whoever is using it; nothing else in this
        /// application puts anything there.
        /// </summary>
        private const int Origin = 0xE000;

        private const int Limit = 0xF8FF;

        /// <summary>The character standing for an icon, or nothing when the id is not one.</summary>
        public static string Mark(int iconId)
        {
            return iconId > 0 && Origin + iconId <= Limit
                ? ((char)(Origin + iconId)).ToString()
                : string.Empty;
        }

        public static bool IsMark(char character)
        {
            return character >= Origin && character <= Limit;
        }

        /// <summary>The icon a mark stands for, or zero when it is not one.</summary>
        public static int IdOf(char character)
        {
            return IsMark(character) ? character - Origin : 0;
        }

        /// <summary>
        /// The line with its icons taken out.
        ///
        /// Used everywhere the icons would do harm rather than good: matching a
        /// line against the one the chat log carries, which never had them;
        /// looking a line up among the hand-made translations, which were
        /// written without them; and putting a line in the chat window, where
        /// they would show as empty boxes.
        /// </summary>
        public static string Strip(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return line ?? string.Empty;
            }

            var found = false;
            foreach (var character in line)
            {
                if (IsMark(character))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return line;
            }

            var builder = new StringBuilder(line.Length);
            foreach (var character in line)
            {
                if (!IsMark(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }
    }
}
