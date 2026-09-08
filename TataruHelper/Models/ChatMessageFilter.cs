using System;
using System.Collections.Generic;
using System.Linq;

namespace FFXIVTataruHelper
{
    public class ChatMessageFilter
    {
        /// <summary>
        /// The two codes story dialogue arrives under. Every other code that
        /// carries a name in front of the line carries a player's.
        /// </summary>
        private static readonly HashSet<string> StoryDialogueCodes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "003D", "0044" };

        private readonly HashSet<string> _blackList;
        private readonly HashSet<string> _chatCodesWithNickNames;

        public ChatMessageFilter(IEnumerable<string> blackList, IEnumerable<string> chatCodesWithNickNames)
        {
            _blackList = new HashSet<string>(
                (blackList ?? Enumerable.Empty<string>()).Select(NormalizeBlackListEntry),
                StringComparer.Ordinal);

            _chatCodesWithNickNames = new HashSet<string>(
                chatCodesWithNickNames ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
        }

        public bool ShouldTranslate(string text)
        {
            return !_blackList.Contains(NormalizeBlackListEntry(text));
        }

        public bool TrySplitNickname(string chatCode, string input, out string nickName, out string textToTranslate)
        {
            nickName = String.Empty;
            textToTranslate = input ?? String.Empty;

            if (!_chatCodesWithNickNames.Contains(chatCode))
                return false;

            if (String.IsNullOrEmpty(textToTranslate))
                return false;

            var separatorIndex = textToTranslate.IndexOf(':');
            if (separatorIndex < 0)
            {
                separatorIndex = textToTranslate.IndexOf('\uFF1A');
            }
            if (separatorIndex <= 0)
                return false;

            if (!LooksLikeSpeakerName(textToTranslate.Substring(0, separatorIndex)))
                return false;

            separatorIndex++;
            nickName = textToTranslate.Substring(0, separatorIndex);
            textToTranslate = textToTranslate.Remove(0, separatorIndex);
            return true;
        }

        /// <summary>
        /// Whether a code belongs to player-created chat rather than story
        /// dialogue.
        ///
        /// Worked out from the codes that carry a name in front of the line
        /// rather than from a roster of its own. There was such a roster, and
        /// it held the same codes as IgnoreNickNameChatCodes.json less the two
        /// story ones - a second list to remember to edit every time a channel
        /// is added, and nothing to notice when somebody forgot.
        /// </summary>
        internal bool IsPlayerChatCode(string chatCode)
        {
            return !string.IsNullOrEmpty(chatCode) &&
                   _chatCodesWithNickNames.Contains(chatCode) &&
                   !StoryDialogueCodes.Contains(chatCode);
        }

        /// <summary>
        /// Guards the speaker split against colons that belong to the sentence.
        ///
        /// Cutscene subtitles carry no speaker at all, so a line such as
        /// "For the sake of all, I beseech thee: deliver us from this fate!" had
        /// everything before the colon treated as a name and left untranslated.
        /// A character name is short and carries no sentence punctuation.
        /// </summary>
        internal static bool LooksLikeSpeakerName(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                return false;

            candidate = candidate.Trim();

            const int maxSpeakerNameLength = 40;
            if (candidate.Length > maxSpeakerNameLength)
                return false;

            var hasSpace = candidate.IndexOf(' ') >= 0;

            foreach (var c in candidate)
            {
                if (c == ',' || c == '.' || c == ';')
                    return false;

                // "???" is a real speaker for an NPC whose name is not known yet,
                // so ? and ! only disqualify a candidate that reads like a sentence.
                if ((c == '!' || c == '?') && hasSpace)
                    return false;
            }

            const int maxSpeakerNameWords = 5;
            return candidate.Split(' ').Length <= maxSpeakerNameWords;
        }

        public static string NormalizeBlackListEntry(string text)
        {
            return Helper.ClearBlackListString(text ?? String.Empty);
        }
    }
}
