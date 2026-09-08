using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FFXIVTataruHelper.Services.Logging;

using Sharlayan;
using Sharlayan.Core;
using Sharlayan.Enums;
using Sharlayan.Models;
using Sharlayan.Models.ReadResults;
using Sharlayan.Resources;

namespace FFXIVTataruHelper.Services.GameMemory
{
    public sealed class SharlayanGameMemoryGateway : IGameMemoryGateway, IDisposable
    {
        private const string DirectDialogCode = "003D";
        private const string CutsceneDialogCode = "0044";

        private readonly IDirectDialogReader _directDialogReader;
        private readonly IAppLogger _logger;
        private readonly Func<DateTime> _timestampProvider;
        private readonly Func<TalkAddonRealtimeDialogSnapshot> _realtimeDialogSnapshotOverride;

        private MemoryHandler _memoryHandler;
        private Reader _reader;
        private TalkAddonRealtimeReader _talkAddonRealtimeReader;
        private ChatLogResult _lastChatLogResult = new ChatLogResult();
        private string _lastRealtimeDialogSignature = string.Empty;

        /// <summary>
        /// Guards against the two windows a duty shows one line in. The
        /// signature above cannot: it carries the speaker, and one of the two
        /// windows names nobody.
        /// </summary>
        private readonly RecentUtterance _recentUtterance = new RecentUtterance();

        private const int MaxRememberedRealtimeLines = 64;

        private readonly HashSet<string> _recentRealtimeLines = new HashSet<string>(StringComparer.Ordinal);

        private readonly Queue<string> _recentRealtimeLineOrder = new Queue<string>();

        /// <summary>
        /// Codes this session has managed to read off the screen at least once.
        ///
        /// The chat log repeats every line the moment the player clicks through
        /// it, so while realtime reading works its copy has to go. But whether
        /// realtime reading works is not one answer for the whole application:
        /// the offset for the cutscene subtitle is the one FFXIVClientStructs
        /// cannot supply and has to be re-derived by hand after a patch, so
        /// speech bubbles can be readable on a client whose subtitles are not.
        /// Dropping the chat-log copy of a code we have never once delivered
        /// leaves the player with nothing at all - which is precisely how this
        /// was reported.
        /// </summary>
        private readonly HashSet<string> _codesReadLive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private int _linesReadLive;

        /// <summary>Bare text of the last realtime line, without the speaker prefix.</summary>
        private string _lastEmittedRealtimeText = string.Empty;

        /// <summary>
        /// The line the game is drawing in its dialogue window at the last
        /// sweep, in the form it reaches the translation pipeline. Refreshed
        /// on every sweep, including the one that finds the window gone.
        /// </summary>
        private string _currentDialogueLine = string.Empty;

        /// <summary>
        /// Who the game says is speaking that line, kept apart from it rather
        /// than split back out of it. A line carries the name before a colon
        /// and plenty of lines have a colon of their own - "I'll say that
        /// again: your cares and your troubles" - so splitting is guesswork
        /// where keeping is not.
        /// </summary>
        private string _currentDialogueSpeaker = string.Empty;

        /// <summary>
        /// The question last sent out, so it is sent once rather than twenty
        /// times a second for as long as the player is thinking about it.
        /// </summary>
        private string _lastChoiceSignature = string.Empty;

        public SharlayanGameMemoryGateway(IDirectDialogReader directDialogReader, IAppLogger logger)
            : this(directDialogReader, logger, null, null)
        {
        }

        internal SharlayanGameMemoryGateway(
            IDirectDialogReader directDialogReader,
            IAppLogger logger,
            Func<TalkAddonRealtimeDialogSnapshot> realtimeDialogSnapshotOverride,
            Func<DateTime> timestampProvider)
        {
            _directDialogReader = directDialogReader;
            _logger = logger;
            _realtimeDialogSnapshotOverride = realtimeDialogSnapshotOverride;
            _timestampProvider = timestampProvider ?? (() => DateTime.Now);
        }

        public void SetProcess(ProcessModel processModel, string gameLanguage, string patchVersion, bool useLocalCache,
            bool scanAllMemoryRegions)
        {
            var configuration = new SharlayanConfiguration
            {
                ProcessModel = processModel,
                GameLanguage = ParseGameLanguage(gameLanguage),
                ScanAllRegions = scanAllMemoryRegions,
                IgnoreGameVersionMismatch = true,
                ResourceProvider = ResourceProviderKind.FFXIVClientStructsDirect
            };

            UnsetProcessCore();

            _memoryHandler = new MemoryHandler(configuration);
            _reader = _memoryHandler.Reader;
            _talkAddonRealtimeReader = new TalkAddonRealtimeReader(_memoryHandler);
            ResetRealtimeDialogState();
        }

        /// <summary>
        /// Forgets what the previous game process was saying. Separate from
        /// <see cref="SetProcess"/> so it can be exercised without a live game.
        /// </summary>
        internal void ResetRealtimeDialogState()
        {
            _lastChatLogResult = new ChatLogResult();
            _lastRealtimeDialogSignature = string.Empty;
            _lastEmittedRealtimeText = string.Empty;
            _recentUtterance.Forget();
            _recentRealtimeLines.Clear();
            _recentRealtimeLineOrder.Clear();
            _codesReadLive.Clear();
            _linesReadLive = 0;
            _currentDialogueLine = string.Empty;
            _currentDialogueSpeaker = string.Empty;
            _lastChoiceSignature = string.Empty;
        }

        /// <summary>
        /// Whether dialogue under this code has been read off the screen at
        /// least once since attaching to the game, and so whether the chat log's
        /// later copy of it would be a repeat.
        /// </summary>
        public bool HasReadCodeLive(string chatCode)
        {
            return !string.IsNullOrEmpty(chatCode) && _codesReadLive.Contains(chatCode);
        }

        public LiveReadingStats LiveReading =>
            new LiveReadingStats(_linesReadLive, _codesReadLive.OrderBy(code => code).ToArray());

        public void UnsetProcess()
        {
            UnsetProcessCore();
        }

        public string GetPlayerName()
        {
            try
            {
                return _reader?.GetCurrentPlayer()?.Entity?.Name ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                return string.Empty;
            }
        }

        public bool? GetPlayerIsFeminine()
        {
            try
            {
                var entity = _reader?.GetCurrentPlayer()?.Entity;
                if (entity == null)
                {
                    return null;
                }

                // Sharlayan reports it as an enum; the Russian only ever needs
                // to know which of two wordings to use.
                return string.Equals(entity.Sex.ToString(), "Female", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
                return null;
            }
        }

        public ChatLogResult GetChatLog(int previousArrayIndex, int previousOffset)
        {
            if (_reader == null)
            {
                return new ChatLogResult();
            }

            _lastChatLogResult = _reader.GetChatLog(previousArrayIndex, previousOffset) ?? new ChatLogResult();
            DropLinesAlreadySeenLive(_lastChatLogResult);
            return _lastChatLogResult;
        }

        /// <summary>
        /// Removes dialogue the realtime reader already reported.
        ///
        /// An NPC line reaches us twice: once from the Talk addon while the bubble
        /// is on screen, and again from the chat log once the player clicks through.
        /// Both codes are enabled by default, so every line was translated and shown
        /// twice.
        /// </summary>
        internal void DropLinesAlreadySeenLive(ChatLogResult chatLogResult)
        {
            // No early-out on "nothing read live yet". That was true exactly
            // when this matters: at the first lines of a session the screen
            // has told us nothing, the chat log arrives first, and leaving
            // without looking is how its copy got through unrecorded.
            if (chatLogResult?.ChatLogItems == null)
            {
                return;
            }

            var kept = chatLogResult.ChatLogItems
                .Where(item => !IsDuplicateOfRealtimeLine(item))
                .ToArray();

            if (kept.Length == chatLogResult.ChatLogItems.Count)
            {
                return;
            }

            chatLogResult.ChatLogItems.Clear();
            foreach (var item in kept)
            {
                chatLogResult.ChatLogItems.Enqueue(item);
            }
        }

        /// <summary>
        /// Whether the chat log is repeating something already read off the
        /// screen and shown.
        ///
        /// Judged on the words alone. It used to also require the line to carry
        /// a dialogue code, which quietly assumed we knew every code dialogue
        /// can arrive under - and cutscene narration arrives under 0039, so
        /// every line of it appeared twice, once live and once from the log.
        /// The words are evidence enough: they are the whole of a line we
        /// showed moments ago, and only the last sixty-four are remembered.
        /// </summary>
        private bool IsDuplicateOfRealtimeLine(ChatLogItem item)
        {
            var key = BuildDuplicateKey(item?.Line);
            var seenLive = _recentRealtimeLines.Contains(key) ||
                           _recentUtterance.IsEcho(key, SpeakerOf(item?.Line), _timestampProvider());

            if (Logger.RawDialogLogEnabled)
            {
                Logger.WriteRawDialogLog($"ChatLog code=[{item?.Code}] seenLive={seenLive} key=[{key}]");
            }

            return seenLive;
        }

        /// <summary>
        /// Reduces a dialogue line to just its spoken text so the live copy and the
        /// chat-log copy compare equal. The two render the speaker differently, so
        /// matching whole lines let every NPC line through twice.
        /// </summary>
        /// <summary>
        /// Who the chat log says is speaking - the part it puts before the
        /// colon, which is exactly what the key below drops. Empty when the
        /// line names nobody, as a bubble or a subtitle does.
        /// </summary>
        internal static string SpeakerOf(string line)
        {
            var normalized = NormalizeDialogToken(line);
            var separatorIndex = normalized.IndexOf(':');
            return separatorIndex > 0 && separatorIndex < normalized.Length - 1
                ? normalized.Substring(0, separatorIndex)
                : string.Empty;
        }

        internal static string BuildDuplicateKey(string line)
        {
            // Without the icons. The screen carries them and the chat log does
            // not, so a line with a mentor crown in it would look like two
            // different lines and go out twice.
            var normalized = GameIcons.Strip(NormalizeDialogToken(line));
            if (normalized.Length == 0)
            {
                return string.Empty;
            }

            var separatorIndex = normalized.IndexOf(':');
            if (separatorIndex > 0 && separatorIndex < normalized.Length - 1)
            {
                normalized = normalized.Substring(separatorIndex + 1);
            }

            var builder = new StringBuilder(normalized.Length);
            var lastWasSpace = false;
            foreach (var c in normalized)
            {
                if (char.IsWhiteSpace(c))
                {
                    lastWasSpace = true;
                    continue;
                }

                if (lastWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                lastWasSpace = false;
                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }

        private void RememberRealtimeLine(string line)
        {
            var normalized = BuildDuplicateKey(line);
            if (normalized.Length == 0)
            {
                return;
            }

            if (_recentRealtimeLines.Add(normalized))
            {
                _recentRealtimeLineOrder.Enqueue(normalized);
            }

            while (_recentRealtimeLineOrder.Count > MaxRememberedRealtimeLines)
            {
                _recentRealtimeLines.Remove(_recentRealtimeLineOrder.Dequeue());
            }
        }

        public AddonBounds DialogueBounds =>
            _talkAddonRealtimeReader?.DialogueBounds ?? AddonBounds.Unknown;

        public DialogueSurface DialogueSurface =>
            _talkAddonRealtimeReader?.DialogueSurface ?? DialogueSurface.None;

        public GameChoice CurrentChoice => _talkAddonRealtimeReader?.Choice ?? GameChoice.None;

        public AddonBounds ChoiceBounds => _talkAddonRealtimeReader?.ChoiceBounds ?? AddonBounds.Unknown;

        public string CurrentDialogueLine => _currentDialogueLine;

        public string CurrentDialogueSpeaker => _currentDialogueSpeaker;

        public ChatLogResult GetDirectDialog()
        {
            var asked = SendOutAQuestionOnce();

            var fallbackDirectDialog =
                _directDialogReader.ExtractDirectDialog(_lastChatLogResult) ?? new ChatLogResult();
            var realtimeSnapshot = _realtimeDialogSnapshotOverride != null
                ? _realtimeDialogSnapshotOverride()
                : (_talkAddonRealtimeReader?.TryReadSnapshot(_lastEmittedRealtimeText)
                   ?? TalkAddonRealtimeDialogSnapshot.Unavailable());

            if (!realtimeSnapshot.SourceAvailable)
            {
                // Nothing is being said. Forgetting the last line matters: without
                // it, the same words said again - an NPC repeating a bubble as you
                // walk past - match the signature still held from last time and are
                // taken for an echo, so they only ever get through when somebody
                // else has spoken in between.
                _lastRealtimeDialogSignature = string.Empty;
                _currentDialogueLine = string.Empty;
            _currentDialogueSpeaker = string.Empty;

                // Deliberately not forgetting what was just said. Clearing it
                // here looked tidy and cost the whole guard: the chat log's
                // copy is recorded while nothing is on screen yet, and the
                // screen's copy follows some forty milliseconds later - so the
                // clearing happened in between, every time, and both were
                // shown. What the memory is for is exactly that gap. An NPC
                // repeating a bubble as you walk past is handled by the two
                // seconds, not by wiping the slate.
                return WithQuestion(fallbackDirectDialog, asked);
            }

            var result = new ChatLogResult();
            var talkText = NormalizeDialogToken(realtimeSnapshot.TalkText);
            if (talkText.Length == 0)
            {
                // Nothing is being drawn. Forgetting that matters: a copy of the last line,
                // still on its way, would arrive into a matching state and be
                // shown as though it were still on screen.
                //
                // The held signature goes with it, exactly as it does when no
                // window is loaded at all. A window that is still there but
                // blank says as much about the conversation as no window: it
                // is over. Keeping the signature through it meant an NPC whose
                // first line is the same both times - a greeting, most of them
                // - had that line swallowed on the second telling, because it
                // still matched what was held from the first.
                _lastRealtimeDialogSignature = string.Empty;
                _currentDialogueLine = string.Empty;
            _currentDialogueSpeaker = string.Empty;
                return WithQuestion(fallbackDirectDialog, asked);
            }

            var chatCode = NormalizeDialogToken(realtimeSnapshot.ChatCode);
            if (chatCode.Length == 0)
            {
                chatCode = DirectDialogCode;
            }

            var speakerName = NormalizeDialogToken(realtimeSnapshot.SpeakerName);
            var line = BuildRealtimeDialogLine(speakerName, talkText);
            _currentDialogueLine = line;
            _currentDialogueSpeaker = speakerName;
            var signature = BuildRealtimeSignature(speakerName, talkText);
            if (!string.Equals(_lastRealtimeDialogSignature, signature, StringComparison.Ordinal))
            {
                _lastRealtimeDialogSignature = signature;
                _lastEmittedRealtimeText = talkText;

                // A duty shows the same line in two windows a breath apart -
                // the subtitle strip, which names nobody, and the dialogue
                // window, which names the speaker. The signature above tells
                // them apart because it carries the speaker, so both went out.
                // Compared without him, they are one utterance.
                var echo = _recentUtterance.IsEcho(BuildDuplicateKey(line), speakerName, _timestampProvider());

                // Every line counts, including the first after attaching. Holding
                // that one back used to be how an already-running game avoided
                // reporting the conversation the player had before as though it
                // had just happened; the reader now skips addons the game is not
                // drawing, so a line that reaches here is one that is on screen.
                if (Logger.RawDialogLogEnabled)
                {
                    Logger.WriteRawDialogLog(
                        $"Emit code=[{chatCode}] speaker=[{speakerName}] echo=[{echo}] text=[{talkText}] line=[{line}]");
                }

                if (line.Length > 0 && !echo)
                {
                    result.ChatLogItems.Enqueue(new ChatLogItem
                    {
                        Code = chatCode, Line = line, TimeStamp = _timestampProvider()
                    });

                    _codesReadLive.Add(chatCode);
                    _linesReadLive++;
                }

                // Remembered even when priming swallowed the line, so the chat-log
                // copy of an already-seen conversation is dropped too.
                RememberRealtimeLine(line);
            }

            if (fallbackDirectDialog.ChatLogItems == null || fallbackDirectDialog.ChatLogItems.Count == 0)
            {
                return WithQuestion(result, asked);
            }

            foreach (var chatLogItem in fallbackDirectDialog.ChatLogItems.ToArray())
            {
                if (IsSpecificCode(chatLogItem, CutsceneDialogCode))
                {
                    result.ChatLogItems.Enqueue(chatLogItem);
                }
            }

            return WithQuestion(result, asked);
        }

        /// <summary>
        /// The question a cutscene is asking, as one message, the first time it
        /// is seen.
        ///
        /// One message and not one per answer: the answers are answers to that
        /// question, and an engine that can see them together translates them
        /// better than three engines each shown a fragment. Numbered, because
        /// the answer has to be found again in the translation and a number is
        /// the one part of a sentence no engine rewrites.
        ///
        /// The game does not write these to its chat log, so nothing else will
        /// carry them and there is nothing to be a duplicate of.
        /// </summary>
        private static ChatLogResult WithQuestion(ChatLogResult result, ChatLogItem asked)
        {
            if (asked == null)
            {
                return result;
            }

            var carried = result ?? new ChatLogResult();
            carried.ChatLogItems.Enqueue(asked);
            return carried;
        }

        private ChatLogItem SendOutAQuestionOnce()
        {
            var choice = _talkAddonRealtimeReader?.Choice ?? GameChoice.None;
            if (!choice.IsBeingAsked)
            {
                _lastChoiceSignature = string.Empty;
                return null;
            }

            var signature = choice.Signature();
            if (string.Equals(_lastChoiceSignature, signature, StringComparison.Ordinal))
            {
                return null;
            }

            _lastChoiceSignature = signature;

            var block = choice.AsBlock();
            if (Logger.RawDialogLogEnabled)
            {
                Logger.WriteRawDialogLog($"ChoiceAsked answers=[{choice.Answers.Count}] block=[{block}]");
            }

            _codesReadLive.Add(CutsceneDialogCode);
            _linesReadLive++;

            return new ChatLogItem
            {
                Code = CutsceneDialogCode, Line = block, TimeStamp = _timestampProvider()
            };
        }

        public bool CheckChatEquality(ChatLogItem item1, ChatLogItem item2)
        {
            return _directDialogReader.CheckChatEquality(item1, item2);
        }

        public void Dispose()
        {
            UnsetProcess();
        }

        private void UnsetProcessCore()
        {
            try
            {
                _memoryHandler?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
            finally
            {
                _memoryHandler = null;
                _reader = null;
                _talkAddonRealtimeReader = null;
                _lastChatLogResult = new ChatLogResult();
                _lastRealtimeDialogSignature = string.Empty;
                _currentDialogueLine = string.Empty;
            _currentDialogueSpeaker = string.Empty;
            }
        }

        internal static string BuildRealtimeSignature(string dialogLine)
        {
            return NormalizeDialogToken(dialogLine);
        }

        /// <summary>
        /// What makes one utterance different from another: who said it and
        /// what they said.
        ///
        /// Deliberately not which addon showed it. A cutscene can put the same
        /// words in the dialogue box and in the subtitle at once, and with the
        /// chat code in here that read as two different lines - they arrived in
        /// the window one after the other, in the two different colours the
        /// codes are drawn in.
        /// </summary>
        internal static string BuildRealtimeSignature(string speakerName, string talkText)
        {
            return string.Concat(
                NormalizeDialogToken(speakerName),
                "|",
                NormalizeDialogToken(talkText));
        }

        internal static string SelectBestTalkText(IEnumerable<string> candidates)
        {
            if (candidates == null)
            {
                return string.Empty;
            }

            return candidates
                .Select(NormalizeDialogToken)
                .Where(candidate => candidate.Length > 0)
                .OrderByDescending(candidate => candidate.Length)
                .FirstOrDefault() ?? string.Empty;
        }

        internal static string NormalizeDialogToken(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        internal static string BuildRealtimeDialogLine(string talkText)
        {
            return BuildRealtimeDialogLine(string.Empty, talkText);
        }

        internal static string BuildRealtimeDialogLine(string speakerName, string talkText)
        {
            var normalizedTalkText = NormalizeDialogToken(talkText);
            if (normalizedTalkText.Length == 0)
            {
                return string.Empty;
            }

            var normalizedSpeakerName = NormalizeDialogToken(speakerName);
            if (normalizedSpeakerName.Length == 0)
            {
                return normalizedTalkText;
            }

            return string.Concat(normalizedSpeakerName, ":", normalizedTalkText);
        }

        private static GameLanguage ParseGameLanguage(string gameLanguage)
        {
            if (Enum.TryParse(gameLanguage, true, out GameLanguage language))
            {
                return language;
            }

            return GameLanguage.English;
        }

        private static bool IsSpecificCode(ChatLogItem item, string code)
        {
            return item != null &&
                   !string.IsNullOrEmpty(item.Code) &&
                   string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase);
        }
    }
}