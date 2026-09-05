using System;
using System.Collections.Generic;
using System.Linq;

namespace FFXIVTataruHelper
{
    public class ChatMessageFilter
    {
        private static readonly HashSet<string> PlayerChatCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "0048", // Recruitment
            "000A", // Say
            "000B", // Shout
            "000C", // Outgoing tell
            "000E", // Party
            "000D", // Tell
            "0018", // Free Company
            "0019", // PvP team
            "001E", // Yell
            "000F", // Alliance
            "0010", "0011", "0012", "0013", "0014", "0015", "0016", "0017", // Linkshells
            "0025", "0065", "0066", "0067", "0068", "0069", "006A", "006B", // Cross-world Linkshells
            "001B", // Novice Network
            "001D", // Emotes
            "001C"  // Custom emotes
        };

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

        /// <summary>Whether a code belongs to player-created chat rather than story dialogue.</summary>
        internal static bool IsPlayerChatCode(string chatCode)
        {
            return !string.IsNullOrEmpty(chatCode) && PlayerChatCodes.Contains(chatCode);
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
