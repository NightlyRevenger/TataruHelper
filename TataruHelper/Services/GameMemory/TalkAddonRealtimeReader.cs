using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using Sharlayan;

namespace FFXIVTataruHelper.Services.GameMemory
{
    internal sealed class TalkAddonRealtimeReader
    {
        private const string TalkAddonName = "Talk";
        private const string MiniTalkAddonName = "MiniTalk";
        private const string AlternateMiniTalkAddonName = "_MiniTalk";
        private const string TalkSubtitleAddonName = "TalkSubtitle";

        /// <summary>
        /// Offset of the subtitle Utf8String inside AddonTalkSubtitle.
        ///
        /// FFXIVClientStructs has no AddonTalkSubtitle type, so unlike every other
        /// addon here this offset cannot be resolved by reflection and was derived
        /// from the running client (verified against 2026.07.16). If cutscene
        /// subtitles stop appearing after a game patch, this is the value to
        /// re-check.
        /// </summary>
        private const long TalkSubtitleTextOffset = 0x238;
        private const string DirectDialogCode = "003D";
        private const string CutsceneDialogCode = "0044";
        private const string UiNamespace = "FFXIVClientStructs.FFXIV.Client.UI.";
        private const long Utf8StringPointerOffset = 0;
        private const long Utf8StringBufUsedOffset = 16;
        private const long Utf8StringLengthOffset = 24;
        private const long Utf8StringInlineFlagOffset = 33;
        private const long Utf8StringInlineBufferOffset = 34;
        private const int MaxUtf8StringByteLength = 4096;
        private const int MaxAtkUnitListEntries = 256;

        private static readonly Lazy<UiDirectDialogOffsets> _uiDirectDialogOffsets =
            new Lazy<UiDirectDialogOffsets>(ResolveUiDirectDialogOffsets);

        private readonly MemoryHandler _memoryHandler;

        private string _lastLoggedLastTalk = string.Empty;
        private string _lastLoggedAddonNodes = string.Empty;

        private string _lastLoggedLoadedAddons = string.Empty;

        private string _lastLoggedNodeList = string.Empty;

        private string _lastLoggedBubbleFlags = string.Empty;

        /// <summary>
        /// Which bubble on screen was just said. The addon shows several at
        /// once and keeps them long after they were spoken.
        /// </summary>
        private readonly SpeechBubbles _speechBubbles = new SpeechBubbles();

        private string _lastLoggedBubblePick = string.Empty;

        private string _lastLoggedBounds = string.Empty;

        private string _lastLoggedNodeGeometry = string.Empty;

        // The client works out where every node lands and keeps the answer, so
        // the drawn place is read rather than accumulated from the tree.
        private const long NodeScreenXOffset = 0x70;
        private const long NodeScreenYOffset = 0x74;

        // What each addon was showing on the previous sweep, as name-and-text
        // pairs rather than keyed by the candidate key: that key carries the
        // addon's address, and the game destroys and recreates Talk for every
        // line of a conversation. Keyed by address, a buffer that has not
        // changed at all - a subtitle still holding the last line of a finished
        // cutscene - comes back under a new key, reads as fresh dialogue, and is
        // announced again between every single line.
        private HashSet<string> _lastAddonText = new HashSet<string>(StringComparer.Ordinal);

        private string _stickyCandidateKey;

        private DateTime _lastInlineDiscovery = DateTime.MinValue;

        private string _lastDiscoveredInlineText = string.Empty;

        private bool _knownInlineOffsetHasWorked;

        private readonly HashSet<string> _provenNodeIdAddons = new HashSet<string>(StringComparer.Ordinal);

        private static readonly TimeSpan InlineDiscoveryInterval = TimeSpan.FromSeconds(2);

        private const int InlineDiscoveryScanBytes = 0x600;

        // AtkResNode.Type for a text node, and the NodeFlags bit that says it is
        // actually on screen.
        private const int TextNodeType = 3;

        private const int VisibleNodeFlag = 0x10;

        private const int MaxNodeListEntries = 512;

        // The subtitle's text lives in node 2; 3 and 4 are the alternates the
        // layout uses, which is how Echoglossian addresses the same addon.
        private static readonly uint[] SubtitleTextNodeIds = { 2, 3, 4 };

        // Read off the running client: Talk keeps the speaker in node 2 and the
        // line in node 3. Naming them beats inferring them from the order the
        // text node fields are declared in.
        private const int TalkSpeakerNodeId = 2;

        private const int TalkTextNodeId = 3;

        // SeString wraps its formatting payloads in these.
        private const byte SeStringPayloadStart = 0x02;

        private const byte SeStringPayloadEnd = 0x03;

        private const int MaxCachedAddonNames = 512;

        private readonly Dictionary<IntPtr, string> _addonNameCache = new Dictionary<IntPtr, string>();

        private static bool IsWantedAddonName(string addonName)
        {
            if (string.IsNullOrEmpty(addonName))
            {
                return false;
            }

            return string.Equals(addonName, TalkAddonName, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(addonName, TalkSubtitleAddonName, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(addonName, MiniTalkAddonName, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(addonName, AlternateMiniTalkAddonName, StringComparison.OrdinalIgnoreCase);
        }

        private TalkAddonRealtimeDialogSnapshot _lastSelectedSnapshot;

        private bool _hasLastSelectedSnapshot;

        /// <summary>
        /// Where the game is drawing the line it is showing, as of the last
        /// sweep, or unknown when it is showing none. Read so a translation can
        /// be put over that line rather than beside it.
        /// </summary>
        public AddonBounds DialogueBounds { get; private set; }

        /// <summary>
        /// Whether what is being shown is a cutscene subtitle, which the game
        /// draws as bare text over the picture rather than inside a window.
        /// </summary>
        public bool DialogueIsSubtitle { get; private set; }

        public TalkAddonRealtimeReader(MemoryHandler memoryHandler)
        {
            _memoryHandler = memoryHandler;
        }

        private static void WriteDistinctRawDialogLog(ref string lastPayload, string payload)
        {
            if (!Logger.RawDialogLogEnabled || string.IsNullOrEmpty(payload))
            {
                return;
            }

            if (string.Equals(lastPayload, payload, StringComparison.Ordinal))
            {
                return;
            }

            lastPayload = payload;
            Logger.WriteRawDialogLog(payload);
        }

        /// <param name="lastEmittedText">
        /// Text the gateway reported last. The Talk addon holds its line long after
        /// the box is gone, so without this a stale Talk line wins over a cutscene
        /// subtitle that is actually on screen and subtitles never surface.
        /// </param>
        public TalkAddonRealtimeDialogSnapshot TryReadSnapshot(string lastEmittedText = null)
        {
            if (_memoryHandler == null)
            {
                return TalkAddonRealtimeDialogSnapshot.Unavailable();
            }

            var locations = _memoryHandler.Scanner?.Locations;
            if (locations == null || !_uiDirectDialogOffsets.Value.IsValid)
            {
                return TalkAddonRealtimeDialogSnapshot.Unavailable();
            }

            if (!locations.TryGetValue(Signatures.CHATLOG_KEY, out var chatLogLocation) || chatLogLocation == null)
            {
                return TalkAddonRealtimeDialogSnapshot.Unavailable();
            }

            var chatLogAddress = chatLogLocation.GetAddress();
            if (chatLogAddress == IntPtr.Zero)
            {
                return TalkAddonRealtimeDialogSnapshot.Unavailable();
            }

            var uiModuleAddress = SubtractAddress(chatLogAddress, _uiDirectDialogOffsets.Value.RaptureLogModuleOffset);
            if (uiModuleAddress == IntPtr.Zero)
            {
                return TalkAddonRealtimeDialogSnapshot.Unavailable();
            }

            TryReadLastTalk(uiModuleAddress, out var lastTalkName, out var lastTalkText);

            var raptureAtkModuleAddress =
                AddAddress(uiModuleAddress, _uiDirectDialogOffsets.Value.RaptureAtkModuleOffset);
            if (raptureAtkModuleAddress == IntPtr.Zero)
            {
                return SelectRealtimeSnapshot(lastTalkName, lastTalkText,
                    Array.Empty<TalkAddonRealtimeDialogSnapshot>());
            }

            var atkUnitManagerAddress = _memoryHandler.ReadPointer(raptureAtkModuleAddress,
                _uiDirectDialogOffsets.Value.AtkUnitManagerOffset);
            if (atkUnitManagerAddress == IntPtr.Zero)
            {
                return SelectRealtimeSnapshot(lastTalkName, lastTalkText,
                    Array.Empty<TalkAddonRealtimeDialogSnapshot>());
            }

            if (!TryReadLoadedAddonSnapshot(atkUnitManagerAddress, lastTalkName, lastTalkText, lastEmittedText,
                    out var snapshot))
            {
                return SelectRealtimeSnapshot(lastTalkName, lastTalkText,
                    Array.Empty<TalkAddonRealtimeDialogSnapshot>());
            }

            return SelectRealtimeSnapshot(lastTalkName, lastTalkText, new[] { snapshot });
        }

        private bool TryReadLastTalk(IntPtr uiModuleAddress, out string speakerName, out string talkText)
        {
            speakerName = string.Empty;
            talkText = string.Empty;

            var readName = TryReadUtf8String(uiModuleAddress, _uiDirectDialogOffsets.Value.LastTalkNameOffset,
                out speakerName);
            var readText = TryReadUtf8String(uiModuleAddress, _uiDirectDialogOffsets.Value.LastTalkTextOffset,
                out talkText);

            if (Logger.RawDialogLogEnabled)
            {
                WriteDistinctRawDialogLog(ref _lastLoggedLastTalk,
                    $"LastTalk name=[{speakerName}] text=[{talkText}]");
            }

            speakerName = SharlayanGameMemoryGateway.NormalizeDialogToken(speakerName);
            talkText = SharlayanGameMemoryGateway.NormalizeDialogToken(talkText);
            return readName || readText;
        }

        private bool TryReadLoadedAddonSnapshot(
            IntPtr atkUnitManagerAddress,
            string speakerName,
            string lastTalkText,
            string lastEmittedText,
            out TalkAddonRealtimeDialogSnapshot snapshot)
        {
            snapshot = TalkAddonRealtimeDialogSnapshot.Unavailable();

            var allLoadedUnitsListAddress =
                AddAddress(atkUnitManagerAddress, _uiDirectDialogOffsets.Value.AllLoadedUnitsListOffset);
            if (allLoadedUnitsListAddress == IntPtr.Zero)
            {
                return false;
            }

            var loadedUnitsCount = _memoryHandler.GetUInt16(allLoadedUnitsListAddress,
                _uiDirectDialogOffsets.Value.AtkUnitListCountOffset);
            if (loadedUnitsCount <= 0)
            {
                return false;
            }

            var entriesAddress = AddAddress(allLoadedUnitsListAddress,
                _uiDirectDialogOffsets.Value.AtkUnitListEntriesOffset);
            if (entriesAddress == IntPtr.Zero)
            {
                return false;
            }

            var loadedAddons = new List<LoadedAddon>();
            var safeCount = Math.Min((int)loadedUnitsCount, MaxAtkUnitListEntries);

            // One read for the whole pointer array instead of one per entry.
            var entryBytes = _memoryHandler.GetByteArray(entriesAddress, safeCount * IntPtr.Size);
            if (entryBytes == null || entryBytes.Length < IntPtr.Size)
            {
                return false;
            }

            var diagnosticNames = Logger.RawDialogLogEnabled ? new List<string>() : null;
            var boundsByAddon = new Dictionary<IntPtr, AddonBounds>();
            var boundsByCandidate = new Dictionary<string, AddonBounds>(StringComparer.Ordinal);

            var readable = entryBytes.Length / IntPtr.Size;
            for (var i = 0; i < Math.Min(safeCount, readable); i++)
            {
                var addonAddress = new IntPtr(BitConverter.ToInt64(entryBytes, i * IntPtr.Size));
                if (addonAddress == IntPtr.Zero)
                {
                    continue;
                }

                // Addon objects sit at stable addresses for as long as they live, so
                // their names are cached: re-reading all ~120 of them thirty times a
                // second was the bulk of this reader's cost. Names that matter are
                // re-read below before being acted on, in case an address was freed
                // and handed to a different addon.
                if (!_addonNameCache.TryGetValue(addonAddress, out var addonName))
                {
                    if (!TryReadAddonName(addonAddress, out addonName))
                    {
                        continue;
                    }

                    if (_addonNameCache.Count >= MaxCachedAddonNames)
                    {
                        _addonNameCache.Clear();
                    }

                    _addonNameCache[addonAddress] = addonName;
                }

                // The full roster is what makes it possible to spot a renamed addon
                // after a patch, so it is still gathered when raw logging is on.
                diagnosticNames?.Add(addonName);

                if (!IsWantedAddonName(addonName))
                {
                    continue;
                }

                if (!TryReadAddonName(addonAddress, out var verifiedName) || verifiedName != addonName)
                {
                    _addonNameCache.Remove(addonAddress);
                    if (string.IsNullOrEmpty(verifiedName) || !IsWantedAddonName(verifiedName))
                    {
                        continue;
                    }

                    _addonNameCache[addonAddress] = verifiedName;
                    addonName = verifiedName;
                }

                loadedAddons.Add(new LoadedAddon(addonAddress, addonName));

                // Where the window stands, taken on the sweep that is already
                // walking these addons rather than on a second one of its own.
                // The first is the one worth keeping: the game shows a line in
                // one place at a time, and a bubble left over from a passer-by
                // should not move a copy off the conversation being read.
                if (TryReadAddonBounds(addonAddress, out var addonBounds))
                {
                    // Kept against the addon it belongs to rather than taken as
                    // "the first one drawn". Which window is speaking is
                    // settled further down, and the two are not the same during
                    // the changeover between lines: a subtitle spanning the
                    // screen was lending its width to a line coming from the
                    // little talk window, and the copy stretched across the
                    // display for a moment on every line.
                    if (!IsAddonOffScreen(addonAddress))
                    {
                        boundsByAddon[addonAddress] = addonBounds;
                    }

                    if (Logger.RawDialogLogEnabled)
                    {
                        WriteDistinctRawDialogLog(ref _lastLoggedBounds,
                            FormattableString.Invariant(
                                $"AddonBounds {addonName} x={addonBounds.X} y={addonBounds.Y} w={addonBounds.Width} h={addonBounds.Height}"));
                    }
                }
            }


            if (diagnosticNames != null)
            {
                WriteDistinctRawDialogLog(ref _lastLoggedLoadedAddons,
                    "LoadedAddons=" + string.Join(",", diagnosticNames.OrderBy(n => n)));
            }

            var matchedEmptySource = false;

            // Candidates are ranked after the sweep rather than taking the first
            // with any text: the Talk addon holds its line forever, so first-wins
            // let a finished conversation hide a subtitle that is on screen.
            var candidates = new List<(string Key, TalkAddonRealtimeDialogSnapshot Snapshot, string Text)>();

            foreach (var addonSpec in _uiDirectDialogOffsets.Value.AddonSpecs)
            {
                // A cutscene keeps several TalkSubtitle addons loaded at once and
                // leaves earlier lines sitting in the ones it is not using, so every
                // match has to be considered rather than just the first.
                var matchingAddons = loadedAddons
                    .Where(addon =>
                        string.Equals(addonSpec.AddonName, addon.AddonName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                foreach (var loadedAddon in matchingAddons)
                {
                    if (loadedAddon.AddonAddress == IntPtr.Zero)
                    {
                        continue;
                    }

                    if (IsAddonOffScreen(loadedAddon.AddonAddress))
                    {
                        continue;
                    }

                    LogNodeList(addonSpec.AddonName, loadedAddon.AddonAddress);

                    if (!TryReadAddonNodeTexts(loadedAddon.AddonAddress, addonSpec, out var nodeTexts))
                    {
                        continue;
                    }

                    if (Logger.RawDialogLogEnabled)
                    {
                        var joinedNodes = string.Join(" | ",
                            (nodeTexts ?? Array.Empty<string>()).Select(text => $"[{text}]"));
                        WriteDistinctRawDialogLog(ref _lastLoggedAddonNodes,
                            $"Addon=[{addonSpec.AddonName}] code=[{addonSpec.ChatCode}] nodes={{ {joinedNodes} }}");
                    }

                    var addonSnapshot = BuildAddonSnapshot(addonSpec, nodeTexts, speakerName, lastTalkText);
                    var addonText = SharlayanGameMemoryGateway.NormalizeDialogToken(addonSnapshot.TalkText);
                    if (addonText.Length == 0)
                    {
                        if (!matchedEmptySource)
                        {
                            snapshot = addonSnapshot;
                            matchedEmptySource = true;
                        }

                        continue;
                    }

                    var candidateKey =
                        addonSpec.AddonName + "@" + loadedAddon.AddonAddress.ToInt64().ToString("X");

                    boundsByCandidate[candidateKey] =
                        boundsByAddon.TryGetValue(loadedAddon.AddonAddress, out var known)
                            ? known
                            : AddonBounds.Unknown;

                    candidates.Add((candidateKey, addonSnapshot, addonText));
                }
            }

            var announced = TrySelectActiveCandidate(candidates, out snapshot);

            // Settled on every sweep, not only on the sweeps that find a new
            // line to announce. A line is announced once and then stays on
            // screen for as long as the player takes to read it; tying the
            // rectangle to the announcement meant it was known for one sweep in
            // fifty and unknown for the rest, and nothing could be drawn over.
            //
            // The window that is speaking is the one to draw over, and a
            // cutscene subtitle is not drawn in a window at all - it is bare
            // text over the picture, so anything covering it has to be bare
            // too.
            if (_stickyCandidateKey != null &&
                boundsByCandidate.TryGetValue(_stickyCandidateKey, out var speaking))
            {
                DialogueBounds = speaking;
                DialogueIsSubtitle =
                    _stickyCandidateKey.StartsWith(TalkSubtitleAddonName, StringComparison.Ordinal);
            }
            else
            {
                DialogueBounds = AddonBounds.Unknown;
                DialogueIsSubtitle = false;
            }

            return announced || matchedEmptySource;
        }

        /// <summary>
        /// Picks the addon that is actually speaking.
        ///
        /// An addon counts as active when its own text changed since the last poll.
        /// Comparing against the last reported line instead would flip between a
        /// stale Talk line and a live subtitle on alternate polls, re-emitting both
        /// about twenty times a second. When nothing changed the previous choice is
        /// kept, so the signature stays put and nothing is re-emitted.
        /// </summary>
        /// <summary>
        /// Identifies "this addon is showing this text" without the address the
        /// candidate key carries, so a recreated addon holding unchanged text is
        /// recognised as the same thing. Two bubbles showing different lines
        /// still count separately, which is what the address was there for.
        /// </summary>
        private static string AddonTextIdentity(string candidateKey, string text)
        {
            var name = candidateKey ?? string.Empty;
            var separator = name.IndexOf('@');
            if (separator >= 0)
            {
                name = name.Substring(0, separator);
            }

            return name + "" + text;
        }

        internal bool TrySelectActiveCandidate(
            IReadOnlyList<(string Key, TalkAddonRealtimeDialogSnapshot Snapshot, string Text)> candidates,
            out TalkAddonRealtimeDialogSnapshot snapshot)
        {
            snapshot = TalkAddonRealtimeDialogSnapshot.Unavailable();

            if (candidates.Count == 0)
            {
                _stickyCandidateKey = null;
                _lastAddonText.Clear();
                _hasLastSelectedSnapshot = false;

                // Nothing on screen, and with the addons' own visibility to go by
                // that is now the truth rather than a guess. Holding the previous
                // line here used to keep the signature from changing, which meant
                // an NPC saying the same thing again - a bubble as you walk past -
                // read as an echo of itself and was dropped. It only got through
                // when somebody else had spoken in between.
                return false;
            }

            string changedKey = null;
            TalkAddonRealtimeDialogSnapshot changedSnapshot = default;

            // Rebuilt from this sweep so entries for addons the game unloaded do not
            // pile up. Trimming by size instead would make every addon look new
            // again the moment the cap was hit, replaying finished dialogue.
            var seenNow = new HashSet<string>(StringComparer.Ordinal);

            foreach (var candidate in candidates)
            {
                var identity = AddonTextIdentity(candidate.Key, candidate.Text);
                var isNew = !_lastAddonText.Contains(identity);

                seenNow.Add(identity);

                if (isNew && changedKey == null)
                {
                    changedKey = candidate.Key;
                    changedSnapshot = candidate.Snapshot;
                }
            }

            _lastAddonText = seenNow;

            if (changedKey != null)
            {
                _stickyCandidateKey = changedKey;
                _lastSelectedSnapshot = changedSnapshot;
                _hasLastSelectedSnapshot = true;
                snapshot = changedSnapshot;
                return true;
            }

            if (_stickyCandidateKey != null)
            {
                foreach (var candidate in candidates)
                {
                    if (string.Equals(candidate.Key, _stickyCandidateKey, StringComparison.Ordinal))
                    {
                        snapshot = candidate.Snapshot;
                        return true;
                    }
                }
            }

            // The chosen addon went away - a subtitle clears itself between lines -
            // and nothing else changed. Repeating the previous choice keeps the
            // signature stable so the gap stays silent; falling back to whatever
            // else has text would announce a finished conversation again.
            if (_hasLastSelectedSnapshot)
            {
                snapshot = _lastSelectedSnapshot;
                return true;
            }

            _stickyCandidateKey = candidates[0].Key;
            _lastSelectedSnapshot = candidates[0].Snapshot;
            _hasLastSelectedSnapshot = true;
            snapshot = candidates[0].Snapshot;
            return true;
        }

        internal static TalkAddonRealtimeDialogSnapshot BuildAddonSnapshot(
            string chatCode,
            string[] nodeTexts,
            string lastTalkName,
            string lastTalkText,
            bool allowNodeSpeaker)
        {
            var slots = (nodeTexts ?? Array.Empty<string>())
                .Select(SharlayanGameMemoryGateway.NormalizeDialogToken)
                .ToArray();

            var normalizedNodeTexts = slots.Where(text => text.Length > 0).ToArray();

            var talkText = string.Empty;
            var speakerName = string.Empty;

            // AddonTalk keeps the speaker in its first text node and the line in the
            // second. Picking the longest text instead used to swap them whenever the
            // name happened to be as long as the line - "Short-tempered Thaumaturge"
            // and "Is this our dark stranger?" are both 26 characters.
            if (allowNodeSpeaker && slots.Length >= 2 && slots[1].Length > 0)
            {
                speakerName = slots[0];
                talkText = slots[1];
            }
            else
            {
                talkText = SharlayanGameMemoryGateway.SelectBestTalkText(normalizedNodeTexts);

                if (allowNodeSpeaker)
                {
                    speakerName = normalizedNodeTexts
                        .FirstOrDefault(text =>
                            !string.Equals(text, talkText, StringComparison.Ordinal)
                        ) ?? string.Empty;
                }
            }

            if (speakerName.Length == 0 && DialogTextMatches(lastTalkText, talkText))
            {
                speakerName = SharlayanGameMemoryGateway.NormalizeDialogToken(lastTalkName);
            }

            return TalkAddonRealtimeDialogSnapshot.Available(chatCode, speakerName, talkText);
        }

        private static TalkAddonRealtimeDialogSnapshot BuildAddonSnapshot(
            AddonRealtimeTextSpec addonSpec,
            string[] nodeTexts,
            string lastTalkName,
            string lastTalkText)
        {
            return BuildAddonSnapshot(
                addonSpec.ChatCode,
                nodeTexts,
                lastTalkName,
                lastTalkText,
                addonSpec.AllowNodeSpeaker);
        }

        internal static TalkAddonRealtimeDialogSnapshot SelectRealtimeSnapshot(
            string speakerName,
            string lastTalkText,
            IEnumerable<TalkAddonRealtimeDialogSnapshot> addonSnapshots)
        {
            var normalizedSpeakerName = SharlayanGameMemoryGateway.NormalizeDialogToken(speakerName);
            TalkAddonRealtimeDialogSnapshot firstEmptyAddonSnapshot = default;
            var hasEmptyAddonSnapshot = false;

            foreach (var addonSnapshot in addonSnapshots ?? Enumerable.Empty<TalkAddonRealtimeDialogSnapshot>())
            {
                if (!addonSnapshot.SourceAvailable)
                {
                    continue;
                }

                var addonText = SharlayanGameMemoryGateway.NormalizeDialogToken(addonSnapshot.TalkText);
                var addonSpeakerName = SharlayanGameMemoryGateway.NormalizeDialogToken(addonSnapshot.SpeakerName);
                if (addonText.Length > 0)
                {
                    if (addonSpeakerName.Length == 0 && DialogTextMatches(lastTalkText, addonText))
                    {
                        addonSpeakerName = normalizedSpeakerName;
                    }

                    return TalkAddonRealtimeDialogSnapshot.Available(
                        addonSnapshot.ChatCode,
                        addonSpeakerName,
                        addonText);
                }

                if (!hasEmptyAddonSnapshot)
                {
                    firstEmptyAddonSnapshot = addonSnapshot;
                    hasEmptyAddonSnapshot = true;
                }
            }

            var fallbackText = SharlayanGameMemoryGateway.NormalizeDialogToken(lastTalkText);
            if (fallbackText.Length > 0)
            {
                return TalkAddonRealtimeDialogSnapshot.Available(DirectDialogCode, normalizedSpeakerName, fallbackText);
            }

            return hasEmptyAddonSnapshot ? firstEmptyAddonSnapshot : TalkAddonRealtimeDialogSnapshot.Unavailable();
        }

        private static bool DialogTextMatches(string left, string right)
        {
            var normalizedLeft = SharlayanGameMemoryGateway.NormalizeDialogToken(left);
            var normalizedRight = SharlayanGameMemoryGateway.NormalizeDialogToken(right);
            return normalizedLeft.Length > 0 &&
                   normalizedRight.Length > 0 &&
                   string.Equals(normalizedLeft, normalizedRight, StringComparison.Ordinal);
        }

        private bool TryReadAddonName(IntPtr addonAddress, out string addonName)
        {
            addonName = string.Empty;
            var nameAddress = AddAddress(addonAddress, _uiDirectDialogOffsets.Value.AtkUnitBaseNameOffset);
            if (nameAddress == IntPtr.Zero || _uiDirectDialogOffsets.Value.AtkUnitBaseNameLength <= 0)
            {
                return false;
            }

            var buffer = _memoryHandler.GetByteArray(nameAddress, _uiDirectDialogOffsets.Value.AtkUnitBaseNameLength);
            if (buffer == null || buffer.Length == 0)
            {
                return false;
            }

            var terminatorIndex = Array.IndexOf(buffer, (byte)0);
            var length = terminatorIndex >= 0 ? terminatorIndex : buffer.Length;
            if (length <= 0)
            {
                return true;
            }

            addonName = Encoding.ASCII.GetString(buffer, 0, length).Trim();
            return true;
        }

        private bool TryReadAddonNodeTexts(
            IntPtr addonAddress,
            AddonRealtimeTextSpec addonSpec,
            out string[] nodeTexts)
        {
            if (addonSpec.InlineTextOffset >= 0)
            {
                // Preferred: ask the addon which node holds its text. Everything
                // below is the older path, kept as the fallback for a client whose
                // node list cannot be walked.
                foreach (var nodeId in SubtitleTextNodeIds)
                {
                    if (TryReadTextNodeById(addonAddress, nodeId, out var nodeText))
                    {
                        _knownInlineOffsetHasWorked = true;
                        nodeTexts = new[] { nodeText };
                        return true;
                    }
                }

                // The known offset always wins. A discovered one is never latched
                // onto: a single false positive would otherwise keep feeding
                // whatever it landed on for the rest of the session, in place of
                // the subtitles it was supposed to rescue.
                if (TryReadUtf8String(addonAddress, addonSpec.InlineTextOffset, out var inlineText) &&
                    inlineText.Length > 0)
                {
                    _knownInlineOffsetHasWorked = true;
                    nodeTexts = new[] { inlineText };
                    return true;
                }

                // Nothing there. Usually that just means no subtitle is showing - by
                // far the common case - but it is also what a patch moving the field
                // looks like, and this is the one offset FFXIVClientStructs cannot
                // supply. Telling the two apart is only possible in hindsight: once
                // the offset has produced a subtitle it is demonstrably still right,
                // and scanning from then on can only turn up scratch buffers. A whole
                // cutscene read 1943 lines through the known offset and every scan in
                // that session fired after it ended, on emptiness, and found a stale
                // copy of the last line.
                if (!_knownInlineOffsetHasWorked &&
                    TryDiscoverInlineTextOffset(addonAddress, out _, out var discoveredText))
                {
                    // The scan cannot tell a field a patch moved from a scratch
                    // buffer still holding the last line of a finished cutscene,
                    // and it re-runs every couple of seconds for as long as the
                    // known offset stays empty. A find that has not changed since
                    // the last one is the buffer: announcing it repeated a single
                    // subtitle after every line of the conversation that followed.
                    if (!string.Equals(discoveredText, _lastDiscoveredInlineText, StringComparison.Ordinal))
                    {
                        _lastDiscoveredInlineText = discoveredText;
                        nodeTexts = new[] { discoveredText };
                        return true;
                    }
                }

                nodeTexts = Array.Empty<string>();
                return true;
            }

            if (TryReadNamedNodeTexts(addonAddress, addonSpec, out nodeTexts))
            {
                return true;
            }

            if (addonSpec.TextNodeOffsets != null && addonSpec.TextNodeOffsets.Length > 0)
            {
                return TryReadDirectTextNodeTexts(addonAddress, addonSpec.TextNodeOffsets, out nodeTexts);
            }

            return TryReadTalkBubbleNodeTexts(addonAddress, addonSpec, out nodeTexts);
        }

        private bool TryReadDirectTextNodeTexts(
            IntPtr addonAddress,
            long[] textNodeOffsets,
            out string[] nodeTexts)
        {
            var textCandidates = new List<string>();

            // Slots stay aligned with textNodeOffsets, empties included: for
            // AddonTalk the first offset is the speaker node and the second the
            // dialogue node, and dropping blanks here would shift that mapping.
            foreach (var textNodeOffset in textNodeOffsets)
            {
                var textNodeAddress = _memoryHandler.ReadPointer(addonAddress, textNodeOffset);
                if (textNodeAddress == IntPtr.Zero)
                {
                    textCandidates.Add(string.Empty);
                    continue;
                }

                if (!TryReadUtf8String(textNodeAddress, _uiDirectDialogOffsets.Value.AtkTextNodeNodeTextOffset,
                        out var candidate))
                {
                    textCandidates.Add(string.Empty);
                    continue;
                }

                textCandidates.Add(SharlayanGameMemoryGateway.NormalizeDialogToken(candidate));
            }

            nodeTexts = textCandidates.ToArray();
            return true;
        }

        private bool TryReadTalkBubbleNodeTexts(
            IntPtr addonAddress,
            AddonRealtimeTextSpec addonSpec,
            out string[] nodeTexts)
        {
            var textCandidates = new List<string>();

            if (addonSpec.TalkBubbleEntriesOffset < 0 ||
                addonSpec.TalkBubbleEntrySize <= 0 ||
                addonSpec.TalkBubbleTextNodeOffset < 0 ||
                addonSpec.TalkBubbleEntryCount <= 0)
            {
                nodeTexts = Array.Empty<string>();
                return false;
            }

            var talkBubblesAddress = AddAddress(addonAddress, addonSpec.TalkBubbleEntriesOffset);
            if (talkBubblesAddress == IntPtr.Zero)
            {
                nodeTexts = Array.Empty<string>();
                return false;
            }

            for (var i = 0; i < addonSpec.TalkBubbleEntryCount; i++)
            {
                var talkBubbleAddress = AddAddress(talkBubblesAddress, (long)i * addonSpec.TalkBubbleEntrySize);
                if (talkBubbleAddress == IntPtr.Zero)
                {
                    continue;
                }

                var textNodeAddress = _memoryHandler.ReadPointer(talkBubbleAddress, addonSpec.TalkBubbleTextNodeOffset);
                if (textNodeAddress == IntPtr.Zero)
                {
                    continue;
                }

                if (!TryReadUtf8String(textNodeAddress, _uiDirectDialogOffsets.Value.AtkTextNodeNodeTextOffset,
                        out var candidate))
                {
                    continue;
                }

                if (Logger.RawDialogLogEnabled && candidate.Length > 0)
                {
                    var walk = _uiDirectDialogOffsets.Value.NodeWalk;
                    if (walk.IsValid)
                    {
                        var resNodeAddress = addonSpec.TalkBubbleResNodeOffset >= 0
                            ? _memoryHandler.ReadPointer(talkBubbleAddress, addonSpec.TalkBubbleResNodeOffset)
                            : IntPtr.Zero;

                        var textFlags = _memoryHandler.GetUInt16(textNodeAddress, walk.NodeFlagsOffset);
                        var resFlags = resNodeAddress != IntPtr.Zero
                            ? _memoryHandler.GetUInt16(resNodeAddress, walk.NodeFlagsOffset)
                            : (ushort)0;

                        var addonVisibility = _uiDirectDialogOffsets.Value.AtkUnitBaseVisibilityFlagsOffset >= 0
                            ? _memoryHandler.GetUInt16(addonAddress,
                                _uiDirectDialogOffsets.Value.AtkUnitBaseVisibilityFlagsOffset)
                            : (ushort)0xFFFF;

                        var rootNodeAddress = _uiDirectDialogOffsets.Value.AtkUnitBaseRootNodeOffset >= 0
                            ? _memoryHandler.ReadPointer(addonAddress,
                                _uiDirectDialogOffsets.Value.AtkUnitBaseRootNodeOffset)
                            : IntPtr.Zero;

                        var rootFlags = rootNodeAddress != IntPtr.Zero
                            ? _memoryHandler.GetUInt16(rootNodeAddress, walk.NodeFlagsOffset)
                            : (ushort)0;

                        WriteDistinctRawDialogLog(ref _lastLoggedBubbleFlags,
                            $"Bubble[{i}] textVis={(textFlags & VisibleNodeFlag) != 0} " +
                            $"resVis={(resFlags & VisibleNodeFlag) != 0} " +
                            $"addonVisFlags=0x{addonVisibility:X} rootFlags=0x{rootFlags:X} " +
                            $"rootVis={(rootFlags & VisibleNodeFlag) != 0} [{candidate}]");
                    }
                }

                var normalized = SharlayanGameMemoryGateway.NormalizeDialogToken(candidate);
                if (normalized.Length > 0)
                {
                    textCandidates.Add(normalized);
                }
            }

            // Narrowed to the one just spoken. The addon holds every bubble
            // on screen at once, and asked for the longest of them the reader
            // answered with a passer-by's stale sentence for minutes on end,
            // because it was longer than every live one.
            var spoken = _speechBubbles.Pick(textCandidates);

            // Logged before the narrowing, and separately from it: printing
            // only what was picked hid what was on screen to pick from, which
            // is the whole of what has to be seen to judge the picking.
            if (Logger.RawDialogLogEnabled)
            {
                WriteDistinctRawDialogLog(ref _lastLoggedBubblePick,
                    "Bubbles on screen={ " +
                    string.Join(" | ", textCandidates.Select(text => "[" + text + "]")) +
                    " } spoken=[" + spoken + "]");
            }

            nodeTexts = spoken.Length > 0 ? new[] { spoken } : Array.Empty<string>();
            return true;
        }

        /// <summary>
        /// Looks through the addon for a Utf8String holding readable text, so a
        /// patch that moves the subtitle field is recovered from automatically
        /// instead of silently ending cutscene translation.
        ///
        /// Throttled, because while no subtitle is on screen there is genuinely
        /// nothing to find and the search would otherwise run every poll.
        /// </summary>
        private bool TryDiscoverInlineTextOffset(IntPtr addonAddress, out long offset, out string text)
        {
            offset = -1;
            text = string.Empty;

            var now = DateTime.UtcNow;
            if (now - _lastInlineDiscovery < InlineDiscoveryInterval)
            {
                return false;
            }

            _lastInlineDiscovery = now;

            byte[] block;
            try
            {
                block = _memoryHandler.GetByteArray(addonAddress, InlineDiscoveryScanBytes);
            }
            catch (Exception)
            {
                return false;
            }

            if (block == null || block.Length < (int)Utf8StringInlineBufferOffset)
            {
                return false;
            }

            var bestLength = 0;

            for (var candidate = 0; candidate + (int)Utf8StringInlineBufferOffset <= block.Length; candidate += 8)
            {
                if (!TryParseUtf8StringHeader(block, candidate, out var byteCount, out var isInline,
                        out var dataPointer))
                {
                    continue;
                }

                string value;
                if (isInline)
                {
                    var start = candidate + (int)Utf8StringInlineBufferOffset;
                    var available = Math.Min((int)byteCount, block.Length - start);
                    if (available <= 0)
                    {
                        continue;
                    }

                    value = DecodeUtf8(block, start, available);
                }
                else
                {
                    var data = _memoryHandler.GetByteArray(new IntPtr(dataPointer), (int)byteCount);
                    if (data == null || data.Length == 0)
                    {
                        continue;
                    }

                    value = DecodeUtf8(data, 0, data.Length);
                }

                if (!LooksLikeDialogueText(value) || value.Length <= bestLength)
                {
                    continue;
                }

                bestLength = value.Length;
                offset = candidate;
                text = value;
            }

            if (offset < 0)
            {
                return false;
            }

            if (Logger.RawDialogLogEnabled)
            {
                Logger.WriteRawDialogLog($"Discovered subtitle Utf8String at +0x{offset:X}");
            }

            return true;
        }

        /// <summary>
        /// Reads a text node out of an addon by its node id, walking the addon's
        /// own node list rather than reaching a fixed offset into the addon.
        ///
        /// Node ids belong to the UI layout and survive the patches that shift
        /// struct fields about - and it is exactly such a fixed offset, the one
        /// for the subtitle, that this project has had to re-derive by hand and
        /// then guard with a scan that produced nothing but false positives.
        ///
        /// A hidden node is skipped, which the offset could never tell us: a
        /// finished cutscene leaves its last subtitle in memory, and reading it
        /// as though it were on screen is what announced one line over and over.
        /// </summary>
        private bool TryReadTextNodeById(IntPtr addonAddress, uint nodeId, out string text)
        {
            text = string.Empty;

            var offsets = _uiDirectDialogOffsets.Value;
            var walk = offsets.NodeWalk;
            if (!walk.IsValid || addonAddress == IntPtr.Zero)
            {
                return false;
            }

            var uldManagerAddress = AddAddress(addonAddress, walk.UldManagerOffset);
            if (uldManagerAddress == IntPtr.Zero)
            {
                return false;
            }

            var nodeCount = _memoryHandler.GetUInt16(uldManagerAddress, walk.NodeListCountOffset);
            if (nodeCount <= 0 || nodeCount > MaxNodeListEntries)
            {
                return false;
            }

            var nodeListAddress = _memoryHandler.ReadPointer(uldManagerAddress, walk.NodeListOffset);
            if (nodeListAddress == IntPtr.Zero)
            {
                return false;
            }

            for (int index = 0; index < nodeCount; index++)
            {
                var nodeAddress = _memoryHandler.ReadPointer(nodeListAddress, index * IntPtr.Size);
                if (nodeAddress == IntPtr.Zero)
                {
                    continue;
                }

                if ((_memoryHandler.GetInt64(nodeAddress, walk.NodeIdOffset) & 0xFFFFFFFF) != nodeId)
                {
                    continue;
                }

                if (_memoryHandler.GetUInt16(nodeAddress, walk.NodeTypeOffset) != TextNodeType)
                {
                    continue;
                }

                if ((_memoryHandler.GetUInt16(nodeAddress, walk.NodeFlagsOffset) & VisibleNodeFlag) == 0)
                {
                    return false;
                }

                return TryReadUtf8String(nodeAddress, offsets.AtkTextNodeNodeTextOffset, out text)
                       && text.Length > 0;
            }

            return false;
        }

        /// <summary>
        /// Whether the addon is not being drawn, judged by its root node.
        ///
        /// The text buffers keep their contents long after the game has finished
        /// with them - a subtitle holds the last line of a cutscene, a speech
        /// bubble holds what an NPC said as you walked past - and reading those
        /// as though they were on screen is behind every repeated line this
        /// reader has produced. The bubble's own nodes are no help: they go on
        /// claiming to be visible. The addon's root node is the one that stops.
        ///
        /// Applied from the first sweep. Waiting for an addon to be seen visible
        /// once before trusting the flag sounds safer and is not: until then the
        /// buffer flows through unfiltered, so its contents are already the text
        /// on record, and when the line is genuinely said it reads as unchanged
        /// and is never announced. That is what let one bubble through out of
        /// five walks past the same NPC.
        ///
        /// Should the flag ever read differently on another client, the failure
        /// is dialogue going missing - loud, and reported - rather than lines
        /// silently repeating, which took all day to track down.
        /// </summary>
        private bool IsAddonOffScreen(IntPtr addonAddress)
        {
            var offsets = _uiDirectDialogOffsets.Value;
            if (!offsets.NodeWalk.IsValid || offsets.AtkUnitBaseRootNodeOffset < 0)
            {
                return false;
            }

            var rootNodeAddress = _memoryHandler.ReadPointer(addonAddress, offsets.AtkUnitBaseRootNodeOffset);
            if (rootNodeAddress == IntPtr.Zero)
            {
                return false;
            }

            return (_memoryHandler.GetUInt16(rootNodeAddress, offsets.NodeWalk.NodeFlagsOffset)
                    & VisibleNodeFlag) == 0;
        }

        /// <summary>
        /// The rectangle an addon occupies, in the coordinates the game client
        /// draws in, or unknown when the client does not say.
        ///
        /// The window carries its own position and the interface scale the
        /// player chose; the node it draws into carries the size it was
        /// designed at, unscaled. So the size on screen is the one multiplied
        /// by the other, and the position is taken as it stands.
        /// </summary>
        internal bool TryReadAddonBounds(IntPtr addonAddress, out AddonBounds bounds)
        {
            bounds = AddonBounds.Unknown;

            var offsets = _uiDirectDialogOffsets.Value;
            if (addonAddress == IntPtr.Zero ||
                !offsets.Bounds.IsValid ||
                offsets.AtkUnitBaseRootNodeOffset < 0)
            {
                return false;
            }

            var rootNodeAddress = _memoryHandler.ReadPointer(addonAddress, offsets.AtkUnitBaseRootNodeOffset);
            if (rootNodeAddress == IntPtr.Zero)
            {
                return false;
            }

            bounds = AddonBounds.From(
                ReadSingle(rootNodeAddress, offsets.Bounds.NodeScreenXOffset),
                ReadSingle(rootNodeAddress, offsets.Bounds.NodeScreenYOffset),
                _memoryHandler.GetUInt16(rootNodeAddress, offsets.Bounds.NodeWidthOffset),
                _memoryHandler.GetUInt16(rootNodeAddress, offsets.Bounds.NodeHeightOffset),
                ReadSingle(rootNodeAddress, offsets.Bounds.NodeScaleXOffset));

            return bounds.IsKnown;
        }

        private short ReadInt16(IntPtr address, long offset)
        {
            var buffer = _memoryHandler.GetByteArray(IntPtr.Add(address, (int)offset), sizeof(short));
            return buffer == null || buffer.Length < sizeof(short) ? (short)0 : BitConverter.ToInt16(buffer, 0);
        }

        private float ReadSingle(IntPtr address, long offset)
        {
            var buffer = _memoryHandler.GetByteArray(IntPtr.Add(address, (int)offset), sizeof(float));
            return buffer == null || buffer.Length < sizeof(float) ? 0f : BitConverter.ToSingle(buffer, 0);
        }

        /// <summary>
        /// Fills the speaker and line slots from the addon's named nodes.
        ///
        /// The slots themselves are unchanged - everything downstream still reads
        /// slot 0 as the speaker and slot 1 as the line - but which node goes in
        /// which slot is now stated by the layout instead of inferred from the
        /// order the text node fields happen to be declared in. That inference is
        /// what put "Is this our dark stranger?" in the speaker slot: it and
        /// "Short-tempered Thaumaturge" are both 26 characters long.
        ///
        /// Falls back until the ids have proved themselves once. Until then an
        /// empty read might mean the ids are wrong on this client rather than
        /// that nobody is speaking, and the difference matters: trusting it too
        /// early would silently swallow every line.
        /// </summary>
        private bool TryReadNamedNodeTexts(IntPtr addonAddress, AddonRealtimeTextSpec addonSpec, out string[] nodeTexts)
        {
            nodeTexts = Array.Empty<string>();

            if (addonSpec.TextNodeId <= 0 || !_uiDirectDialogOffsets.Value.NodeWalk.IsValid)
            {
                return false;
            }

            TryReadTextNodeById(addonAddress, (uint)addonSpec.TextNodeId, out var talkText);

            if (talkText.Length == 0)
            {
                if (!_provenNodeIdAddons.Contains(addonSpec.AddonName))
                {
                    return false;
                }

                // The ids are known good here, so nothing on the node means
                // nothing is being said - which the offsets could not tell us,
                // and reading a line the game had finished with is what repeated
                // dialogue that had already gone by.
                nodeTexts = Array.Empty<string>();
                return true;
            }

            _provenNodeIdAddons.Add(addonSpec.AddonName);

            var speakerName = string.Empty;
            if (addonSpec.SpeakerNodeId > 0)
            {
                TryReadTextNodeById(addonAddress, (uint)addonSpec.SpeakerNodeId, out speakerName);
            }

            nodeTexts = new[] { speakerName, talkText };
            return true;
        }

        /// <summary>
        /// Dumps an addon's node list so the walk can be checked against the
        /// running client: what ids it has, which are text, which are on screen.
        /// </summary>
        private void LogNodeList(string addonName, IntPtr addonAddress)
        {
            var offsets = _uiDirectDialogOffsets.Value;
            var walk = offsets.NodeWalk;
            if (!Logger.RawDialogLogEnabled || !walk.IsValid || addonAddress == IntPtr.Zero)
            {
                return;
            }

            var uldManagerAddress = AddAddress(addonAddress, walk.UldManagerOffset);
            if (uldManagerAddress == IntPtr.Zero)
            {
                return;
            }

            var nodeCount = _memoryHandler.GetUInt16(uldManagerAddress, walk.NodeListCountOffset);
            var nodeListAddress = _memoryHandler.ReadPointer(uldManagerAddress, walk.NodeListOffset);
            if (nodeCount <= 0 || nodeCount > MaxNodeListEntries || nodeListAddress == IntPtr.Zero)
            {
                WriteDistinctRawDialogLog(ref _lastLoggedNodeList,
                    $"NodeList addon=[{addonName}] unusable count={nodeCount}");
                return;
            }

            var described = new List<string>(nodeCount);
            var geometry = new List<string>(nodeCount);
            for (int index = 0; index < nodeCount; index++)
            {
                var nodeAddress = _memoryHandler.ReadPointer(nodeListAddress, index * IntPtr.Size);
                if (nodeAddress == IntPtr.Zero)
                {
                    continue;
                }

                var id = _memoryHandler.GetInt64(nodeAddress, walk.NodeIdOffset) & 0xFFFFFFFF;
                var type = _memoryHandler.GetUInt16(nodeAddress, walk.NodeTypeOffset);
                var visible = (_memoryHandler.GetUInt16(nodeAddress, walk.NodeFlagsOffset) & VisibleNodeFlag) != 0;

                if (visible)
                {
                    var bounds = offsets.Bounds;
                    geometry.Add(FormattableString.Invariant(
                        $"id={id} t={type} sxy={ReadSingle(nodeAddress, NodeScreenXOffset)},{ReadSingle(nodeAddress, NodeScreenYOffset)} wh={_memoryHandler.GetUInt16(nodeAddress, bounds.NodeWidthOffset)}x{_memoryHandler.GetUInt16(nodeAddress, bounds.NodeHeightOffset)} sc={ReadSingle(nodeAddress, bounds.NodeScaleXOffset)}"));
                }

                if (type != TextNodeType)
                {
                    continue;
                }

                TryReadUtf8String(nodeAddress, offsets.AtkTextNodeNodeTextOffset, out var nodeText);
                described.Add($"id={id} vis={(visible ? 1 : 0)} [{nodeText}]");
            }

            WriteDistinctRawDialogLog(ref _lastLoggedNodeList,
                $"NodeList addon=[{addonName}] count={nodeCount} text={{ {string.Join(" | ", described)} }}");

            WriteDistinctRawDialogLog(ref _lastLoggedNodeGeometry,
                $"NodeGeometry addon=[{addonName}] { string.Join(" | ", geometry) }");
        }

        private static string DecodeUtf8(byte[] data, int start, int count)
        {
            var terminator = Array.IndexOf(data, (byte)0, start, count);
            if (terminator >= 0)
            {
                count = terminator - start;
            }

            return DecodeGameString(data, start, count);
        }

        /// <summary>
        /// Decodes dialogue text, dropping the formatting payloads the game
        /// embeds in it - italics, highlights, auto-translate - which are
        /// delimited by 0x02 and 0x03.
        ///
        /// They have to go before the text is decoded rather than after: a
        /// payload carries raw bytes that are not valid UTF-8, and decoding
        /// first replaces them with U+FFFD, which can no longer be told apart
        /// from a replacement character in the dialogue itself. Left in, the
        /// payload's own kind byte is printable and reaches the translator as a
        /// stray letter - an emphasised line arrived as "H&#65533;#O mournful voice of
        /// creation!...H".
        /// </summary>
        internal static string DecodeGameString(byte[] data, int start, int count)
        {
            if (data == null || count <= 0 || start < 0 || start >= data.Length)
            {
                return string.Empty;
            }

            var end = Math.Min(start + count, data.Length);
            var cleaned = new byte[end - start];
            var written = 0;

            for (int i = start; i < end; i++)
            {
                if (data[i] != SeStringPayloadStart)
                {
                    cleaned[written++] = data[i];
                    continue;
                }

                // A payload that never closes is malformed; nothing after it can
                // be trusted to be text, so the line ends here.
                var payloadEnd = Array.IndexOf(data, SeStringPayloadEnd, i, end - i);
                if (payloadEnd < 0)
                {
                    break;
                }

                i = payloadEnd;
            }

            return written == 0 ? string.Empty : Encoding.UTF8.GetString(cleaned, 0, written);
        }

        /// <summary>
        /// Recognises a Utf8String header inside a block of addon memory.
        ///
        /// Used to rediscover where a string lives when the offset baked into the
        /// code stops working - the layout is distinctive enough to find by shape:
        /// a byte count, a matching capacity, an inline flag, and either an inline
        /// buffer or a pointer.
        /// </summary>
        internal static bool TryParseUtf8StringHeader(byte[] buffer, int offset, out long byteCount,
            out bool isInline, out long dataPointer)
        {
            byteCount = 0;
            isInline = false;
            dataPointer = 0;

            if (buffer == null || offset < 0 || offset + (int)Utf8StringInlineBufferOffset > buffer.Length)
            {
                return false;
            }

            var bufUsed = BitConverter.ToInt64(buffer, offset + (int)Utf8StringBufUsedOffset);
            var length = BitConverter.ToInt64(buffer, offset + (int)Utf8StringLengthOffset);

            byteCount = bufUsed > 0 ? bufUsed : length;
            if (byteCount <= 0 || byteCount > MaxUtf8StringByteLength)
            {
                return false;
            }

            // Capacity has to be able to hold what the length claims.
            if (bufUsed > 0 && length > 0 && length < bufUsed - 1)
            {
                return false;
            }

            isInline = buffer[offset + (int)Utf8StringInlineFlagOffset] != 0;
            dataPointer = BitConverter.ToInt64(buffer, offset + (int)Utf8StringPointerOffset);

            if (!isInline && (dataPointer <= 0x10000 || dataPointer > 0x7FFFFFFFFFFF))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// True when the bytes read like a spoken line rather than something else
        /// that happens to decode.
        ///
        /// An addon is full of readable strings that are not dialogue - animation
        /// and layout identifiers such as "cbbp_a_deact", short labels such as
        /// "ПЭ 2" - and a loose test let those reach the translator as if they were
        /// subtitles. A line of dialogue is long, has several words, and does not
        /// look like a code identifier.
        /// </summary>
        internal static bool LooksLikeDialogueText(string value)
        {
            const int minDialogueLength = 12;

            if (string.IsNullOrWhiteSpace(value) || value.Length < minDialogueLength)
            {
                return false;
            }

            if (value.IndexOf('_') >= 0)
            {
                return false;
            }

            var letters = 0;
            var visible = 0;
            var words = 1;

            foreach (var c in value)
            {
                if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                {
                    return false;
                }

                if (char.IsWhiteSpace(c))
                {
                    words++;
                    continue;
                }

                visible++;

                if (char.IsLetter(c))
                {
                    letters++;
                }
            }

            if (words < 3 || visible == 0)
            {
                return false;
            }

            return letters * 10 >= visible * 7;
        }

        private bool TryReadUtf8String(IntPtr baseAddress, long structOffset, out string value)
        {
            value = string.Empty;
            var utf8StringAddress = AddAddress(baseAddress, structOffset);
            if (utf8StringAddress == IntPtr.Zero)
            {
                return false;
            }

            var stringLength = _memoryHandler.GetInt64(utf8StringAddress, Utf8StringLengthOffset);
            var bytesUsed = _memoryHandler.GetInt64(utf8StringAddress, Utf8StringBufUsedOffset);
            var byteCount = bytesUsed > 0 ? bytesUsed : stringLength;

            if (byteCount <= 0)
            {
                return true;
            }

            if (byteCount > MaxUtf8StringByteLength)
            {
                return false;
            }

            var isUsingInlineBuffer = _memoryHandler.GetByte(utf8StringAddress, Utf8StringInlineFlagOffset) != 0;
            var dataAddress = isUsingInlineBuffer
                ? AddAddress(utf8StringAddress, Utf8StringInlineBufferOffset)
                : _memoryHandler.ReadPointer(utf8StringAddress, Utf8StringPointerOffset);

            if (dataAddress == IntPtr.Zero)
            {
                return false;
            }

            var effectiveByteCount = (int)byteCount;
            var data = _memoryHandler.GetByteArray(dataAddress, effectiveByteCount);
            if (data == null || data.Length == 0)
            {
                return true;
            }

            var zeroTerminatorIndex = Array.IndexOf(data, (byte)0);
            if (zeroTerminatorIndex >= 0)
            {
                effectiveByteCount = zeroTerminatorIndex;
            }

            if (effectiveByteCount <= 0)
            {
                return true;
            }

            value = DecodeGameString(data, 0, effectiveByteCount);
            return true;
        }

        private static IntPtr AddAddress(IntPtr address, long offset)
        {
            var target = address.ToInt64() + offset;
            return target <= 0 ? IntPtr.Zero : new IntPtr(target);
        }

        private static IntPtr SubtractAddress(IntPtr address, long offset)
        {
            var target = address.ToInt64() - offset;
            return target <= 0 ? IntPtr.Zero : new IntPtr(target);
        }

        private static UiDirectDialogOffsets ResolveUiDirectDialogOffsets()
        {
            var uiModuleType = Type.GetType("FFXIVClientStructs.FFXIV.Client.UI.UIModule, Sharlayan");
            if (uiModuleType == null)
            {
                return UiDirectDialogOffsets.Empty;
            }

            var raptureLogModuleOffset = ResolveFieldOffset(uiModuleType, "RaptureLogModule");
            var raptureAtkModuleOffset = ResolveFieldOffset(uiModuleType, "RaptureAtkModule");
            var lastTalkNameOffset = ResolveFieldOffset(uiModuleType, "LastTalkName");
            var lastTalkTextOffset = ResolveFieldOffset(uiModuleType, "LastTalkText");

            var raptureAtkModuleType = Type.GetType("FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkModule, Sharlayan");
            var atkUnitManagerType = Type.GetType("FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitManager, Sharlayan");
            var atkUnitListType = Type.GetType("FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitList, Sharlayan");
            var atkUnitBaseType = Type.GetType("FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase, Sharlayan");
            var addonTalkType = Type.GetType("FFXIVClientStructs.FFXIV.Client.UI.AddonTalk, Sharlayan");
            var atkTextNodeType = Type.GetType("FFXIVClientStructs.FFXIV.Component.GUI.AtkTextNode, Sharlayan");
            var atkResNodeType = Type.GetType("FFXIVClientStructs.FFXIV.Component.GUI.AtkResNode, Sharlayan");

            if (raptureAtkModuleType == null ||
                atkUnitManagerType == null ||
                atkUnitListType == null ||
                atkUnitBaseType == null ||
                addonTalkType == null ||
                atkTextNodeType == null)
            {
                return UiDirectDialogOffsets.Empty;
            }

            var atkUnitManagerOffset = ResolveFieldOffset(raptureAtkModuleType, "AtkUnitManager");
            var allLoadedUnitsListOffset = ResolveFieldOffset(atkUnitManagerType, "AllLoadedUnitsList");
            var atkUnitListEntriesOffset = ResolveFieldOffset(atkUnitListType, "_entries");
            var atkUnitListCountOffset = ResolveFieldOffset(atkUnitListType, "Count");
            var atkUnitBaseNameOffset = ResolveFieldOffset(atkUnitBaseType, "_name");
            var atkUnitBaseNameLength = ResolveFixedBufferLength(atkUnitBaseType, "_name");
            var atkTextNodeNodeTextOffset = ResolveFieldOffset(atkTextNodeType, "NodeText");
            var addonSpecs = ResolveAddonSpecs(addonTalkType);

            if (raptureLogModuleOffset < 0 ||
                raptureAtkModuleOffset < 0 ||
                lastTalkNameOffset < 0 ||
                lastTalkTextOffset < 0 ||
                atkUnitManagerOffset < 0 ||
                allLoadedUnitsListOffset < 0 ||
                atkUnitListEntriesOffset < 0 ||
                atkUnitListCountOffset < 0 ||
                atkUnitBaseNameOffset < 0 ||
                atkUnitBaseNameLength <= 0 ||
                atkTextNodeNodeTextOffset < 0 ||
                addonSpecs.Length == 0)
            {
                return UiDirectDialogOffsets.Empty;
            }

            return new UiDirectDialogOffsets(
                raptureLogModuleOffset,
                raptureAtkModuleOffset,
                lastTalkNameOffset,
                lastTalkTextOffset,
                atkUnitManagerOffset,
                allLoadedUnitsListOffset,
                atkUnitListEntriesOffset,
                atkUnitListCountOffset,
                atkUnitBaseNameOffset,
                atkUnitBaseNameLength,
                atkTextNodeNodeTextOffset,
                ResolveFieldOffset(atkUnitBaseType, "VisibilityFlags"),
                ResolveFieldOffset(atkUnitBaseType, "RootNode"),
                ResolveNodeWalkOffsets(atkUnitBaseType),
                new AddonBoundsOffsets(
                    ResolveFieldOffset(atkResNodeType, "ScreenX"),
                    ResolveFieldOffset(atkResNodeType, "ScreenY"),
                    ResolveFieldOffset(atkResNodeType, "Width"),
                    ResolveFieldOffset(atkResNodeType, "Height"),
                    ResolveFieldOffset(atkResNodeType, "ScaleX")),
                addonSpecs);
        }

        private static AtkNodeWalkOffsets ResolveNodeWalkOffsets(Type atkUnitBaseType)
        {
            var uldManagerType = Type.GetType("FFXIVClientStructs.FFXIV.Component.GUI.AtkUldManager, Sharlayan");
            var atkResNodeType = Type.GetType("FFXIVClientStructs.FFXIV.Component.GUI.AtkResNode, Sharlayan");

            if (uldManagerType == null || atkResNodeType == null)
            {
                return AtkNodeWalkOffsets.Empty;
            }

            return new AtkNodeWalkOffsets(
                ResolveFieldOffset(atkUnitBaseType, "UldManager"),
                ResolveFieldOffset(uldManagerType, "NodeList"),
                ResolveFieldOffset(uldManagerType, "NodeListCount"),
                ResolveFieldOffset(atkResNodeType, "NodeId"),
                ResolveFieldOffset(atkResNodeType, "Type"),
                ResolveFieldOffset(atkResNodeType, "NodeFlags"));
        }

        private static long ResolveFieldOffset(Type type, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return -1;
            }

            var fieldOffsetAttribute = field.GetCustomAttributes(typeof(FieldOffsetAttribute), false)
                .OfType<FieldOffsetAttribute>()
                .FirstOrDefault();
            if (fieldOffsetAttribute != null)
            {
                return fieldOffsetAttribute.Value;
            }

            return -1;
        }

        private static int ResolveFixedBufferLength(Type ownerType, string fieldName)
        {
            var field = ownerType.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || field.FieldType == null)
            {
                return 0;
            }

            var typeName = field.FieldType.Name ?? string.Empty;
            const string prefix = "FixedSizeArray";
            if (!typeName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return 0;
            }

            var digits = new string(typeName.Skip(prefix.Length).TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var length) ? length : 0;
        }

        private static AddonRealtimeTextSpec[] ResolveAddonSpecs(Type addonTalkType)
        {
            var addonSpecs = new List<AddonRealtimeTextSpec>();
            addonSpecs.Add(AddonRealtimeTextSpec.Direct(
                TalkAddonName,
                DirectDialogCode,
                ResolveAddonTalkTextNodeOffsets(addonTalkType),
                true,
                TalkSpeakerNodeId,
                TalkTextNodeId));

            var miniTalkSpec = ResolveMiniTalkAddonSpec(MiniTalkAddonName);
            if (miniTalkSpec != null)
            {
                addonSpecs.Add(miniTalkSpec);
            }

            var alternateMiniTalkSpec = ResolveMiniTalkAddonSpec(AlternateMiniTalkAddonName);
            if (alternateMiniTalkSpec != null)
            {
                addonSpecs.Add(alternateMiniTalkSpec);
            }

            addonSpecs.Add(AddonRealtimeTextSpec.InlineText(
                TalkSubtitleAddonName,
                CutsceneDialogCode,
                TalkSubtitleTextOffset));

            return addonSpecs.ToArray();
        }

        private static AddonRealtimeTextSpec ResolveMiniTalkAddonSpec(string addonName)
        {
            var addonMiniTalkType = Type.GetType(UiNamespace + "AddonMiniTalk, Sharlayan");
            var talkBubbleEntryType = Type.GetType(UiNamespace + "AddonMiniTalk+TalkBubbleEntry, Sharlayan");
            if (addonMiniTalkType == null || talkBubbleEntryType == null)
            {
                return null;
            }

            var talkBubbleEntriesOffset = ResolveFieldOffset(addonMiniTalkType, "_talkBubbles");
            var talkBubbleEntrySize = Marshal.SizeOf(talkBubbleEntryType);
            var talkBubbleTextNodeOffset = ResolveFieldOffset(talkBubbleEntryType, "BubbleTextNode");
            var talkBubbleEntryCount = ResolveFixedBufferLength(addonMiniTalkType, "_talkBubbles");

            if (talkBubbleEntriesOffset < 0 ||
                talkBubbleEntrySize <= 0 ||
                talkBubbleTextNodeOffset < 0 ||
                talkBubbleEntryCount <= 0)
            {
                return null;
            }

            return AddonRealtimeTextSpec.TalkBubbles(
                addonName,
                CutsceneDialogCode,
                talkBubbleEntriesOffset,
                talkBubbleEntrySize,
                talkBubbleTextNodeOffset,
                talkBubbleEntryCount,
                ResolveFieldOffset(talkBubbleEntryType, "BubbleResNode"));
        }

        private static long[] ResolveAddonTalkTextNodeOffsets(Type addonTalkType)
        {
            return addonTalkType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.Name.StartsWith("AtkTextNode", StringComparison.Ordinal))
                .Where(field => string.Equals(field.FieldType.Name, "AtkTextNode*", StringComparison.Ordinal))
                .Select(field =>
                {
                    var offsetAttribute = field.GetCustomAttributes(typeof(FieldOffsetAttribute), false)
                        .OfType<FieldOffsetAttribute>()
                        .FirstOrDefault();
                    return (long)(offsetAttribute?.Value ?? -1);
                })
                .Where(offset => offset >= 0)
                .OrderBy(offset => offset)
                .ToArray();
        }

        private readonly struct LoadedAddon
        {
            public LoadedAddon(IntPtr addonAddress, string addonName)
            {
                AddonAddress = addonAddress;
                AddonName = addonName ?? string.Empty;
            }

            public IntPtr AddonAddress { get; }
            public string AddonName { get; }
        }

        /// <summary>
        /// Everything needed to walk an addon's own node list and pick a node out
        /// by its id, instead of reaching a hardcoded offset into the addon.
        ///
        /// Optional: when these cannot be resolved the reader falls back to the
        /// offsets it used before, so a change here can never cost us dialogue.
        /// </summary>
        private readonly struct AtkNodeWalkOffsets
        {
            public static AtkNodeWalkOffsets Empty => new AtkNodeWalkOffsets(-1, -1, -1, -1, -1, -1);

            public long UldManagerOffset { get; }
            public long NodeListOffset { get; }
            public long NodeListCountOffset { get; }
            public long NodeIdOffset { get; }
            public long NodeTypeOffset { get; }
            public long NodeFlagsOffset { get; }

            public bool IsValid =>
                UldManagerOffset >= 0 &&
                NodeListOffset >= 0 &&
                NodeListCountOffset >= 0 &&
                NodeIdOffset >= 0 &&
                NodeTypeOffset >= 0 &&
                NodeFlagsOffset >= 0;

            public AtkNodeWalkOffsets(
                long uldManagerOffset,
                long nodeListOffset,
                long nodeListCountOffset,
                long nodeIdOffset,
                long nodeTypeOffset,
                long nodeFlagsOffset)
            {
                UldManagerOffset = uldManagerOffset;
                NodeListOffset = nodeListOffset;
                NodeListCountOffset = nodeListCountOffset;
                NodeIdOffset = nodeIdOffset;
                NodeTypeOffset = nodeTypeOffset;
                NodeFlagsOffset = nodeFlagsOffset;
            }
        }

        /// <summary>
        /// Where the client keeps an addon's place on screen. Resolved by
        /// reflection like everything else here, so a patch that moves these
        /// moves them for the text as well and there is one thing to mend.
        /// </summary>
        private readonly struct AddonBoundsOffsets
        {
            public static AddonBoundsOffsets Empty => new AddonBoundsOffsets(-1, -1, -1, -1, -1);

            public AddonBoundsOffsets(
                long nodeScreenX, long nodeScreenY, long nodeWidth, long nodeHeight, long nodeScaleX)
            {
                NodeScreenXOffset = nodeScreenX;
                NodeScreenYOffset = nodeScreenY;
                NodeWidthOffset = nodeWidth;
                NodeHeightOffset = nodeHeight;
                NodeScaleXOffset = nodeScaleX;
            }

            // Where the client worked out the node lands, which is not what the
            // window says its position is once the interface is scaled.
            public long NodeScreenXOffset { get; }
            public long NodeScreenYOffset { get; }
            public long NodeWidthOffset { get; }
            public long NodeHeightOffset { get; }
            public long NodeScaleXOffset { get; }

            public bool IsValid =>
                NodeScreenXOffset >= 0 && NodeScreenYOffset >= 0 && NodeScaleXOffset >= 0 &&
                NodeWidthOffset >= 0 && NodeHeightOffset >= 0;
        }

        private readonly struct UiDirectDialogOffsets
        {
            public static UiDirectDialogOffsets Empty =>
                new UiDirectDialogOffsets(-1, -1, -1, -1, -1, -1, -1, -1, -1, 0, -1, -1, -1,
                    AtkNodeWalkOffsets.Empty, AddonBoundsOffsets.Empty,
                    Array.Empty<AddonRealtimeTextSpec>());

            public long RaptureLogModuleOffset { get; }
            public long RaptureAtkModuleOffset { get; }
            public long LastTalkNameOffset { get; }
            public long LastTalkTextOffset { get; }
            public long AtkUnitManagerOffset { get; }
            public long AllLoadedUnitsListOffset { get; }
            public long AtkUnitListEntriesOffset { get; }
            public long AtkUnitListCountOffset { get; }
            public long AtkUnitBaseNameOffset { get; }
            public int AtkUnitBaseNameLength { get; }

            public long AtkTextNodeNodeTextOffset { get; }

            public long AtkUnitBaseVisibilityFlagsOffset { get; }

            public long AtkUnitBaseRootNodeOffset { get; }

            // Not part of IsValid: the reader works without it, just less well.
            public AtkNodeWalkOffsets NodeWalk { get; }

            // Nor this one: reading the line does not depend on knowing where
            // the window that holds it stands.
            public AddonBoundsOffsets Bounds { get; }

            public AddonRealtimeTextSpec[] AddonSpecs { get; }

            public bool IsValid =>
                RaptureLogModuleOffset >= 0 &&
                RaptureAtkModuleOffset >= 0 &&
                LastTalkNameOffset >= 0 &&
                LastTalkTextOffset >= 0 &&
                AtkUnitManagerOffset >= 0 &&
                AllLoadedUnitsListOffset >= 0 &&
                AtkUnitListEntriesOffset >= 0 &&
                AtkUnitListCountOffset >= 0 &&
                AtkUnitBaseNameOffset >= 0 &&
                AtkUnitBaseNameLength > 0 &&
                AtkTextNodeNodeTextOffset >= 0 &&
                AddonSpecs != null &&
                AddonSpecs.Length > 0;

            public UiDirectDialogOffsets(
                long raptureLogModuleOffset,
                long raptureAtkModuleOffset,
                long lastTalkNameOffset,
                long lastTalkTextOffset,
                long atkUnitManagerOffset,
                long allLoadedUnitsListOffset,
                long atkUnitListEntriesOffset,
                long atkUnitListCountOffset,
                long atkUnitBaseNameOffset,
                int atkUnitBaseNameLength,
                long atkTextNodeNodeTextOffset,
                long atkUnitBaseVisibilityFlagsOffset,
                long atkUnitBaseRootNodeOffset,
                AtkNodeWalkOffsets nodeWalk,
                AddonBoundsOffsets bounds,
                AddonRealtimeTextSpec[] addonSpecs)
            {
                NodeWalk = nodeWalk;
                Bounds = bounds;
                RaptureLogModuleOffset = raptureLogModuleOffset;
                RaptureAtkModuleOffset = raptureAtkModuleOffset;
                LastTalkNameOffset = lastTalkNameOffset;
                LastTalkTextOffset = lastTalkTextOffset;
                AtkUnitManagerOffset = atkUnitManagerOffset;
                AllLoadedUnitsListOffset = allLoadedUnitsListOffset;
                AtkUnitListEntriesOffset = atkUnitListEntriesOffset;
                AtkUnitListCountOffset = atkUnitListCountOffset;
                AtkUnitBaseNameOffset = atkUnitBaseNameOffset;
                AtkUnitBaseNameLength = atkUnitBaseNameLength;
                AtkTextNodeNodeTextOffset = atkTextNodeNodeTextOffset;
                AtkUnitBaseVisibilityFlagsOffset = atkUnitBaseVisibilityFlagsOffset;
                AtkUnitBaseRootNodeOffset = atkUnitBaseRootNodeOffset;

                AddonSpecs = addonSpecs ?? Array.Empty<AddonRealtimeTextSpec>();
            }
        }

        private sealed class AddonRealtimeTextSpec
        {
            private AddonRealtimeTextSpec(
                string addonName,
                string chatCode,
                long[] textNodeOffsets,
                bool allowNodeSpeaker,
                long talkBubbleEntriesOffset,
                int talkBubbleEntrySize,
                long talkBubbleTextNodeOffset,
                int talkBubbleEntryCount,
                long inlineTextOffset,
                int speakerNodeId,
                int textNodeId,
                long talkBubbleResNodeOffset = -1)
            {
                TalkBubbleResNodeOffset = talkBubbleResNodeOffset;
                SpeakerNodeId = speakerNodeId;
                TextNodeId = textNodeId;
                AddonName = addonName;
                ChatCode = chatCode;
                TextNodeOffsets = textNodeOffsets ?? Array.Empty<long>();
                AllowNodeSpeaker = allowNodeSpeaker;
                TalkBubbleEntriesOffset = talkBubbleEntriesOffset;
                TalkBubbleEntrySize = talkBubbleEntrySize;
                TalkBubbleTextNodeOffset = talkBubbleTextNodeOffset;
                TalkBubbleEntryCount = talkBubbleEntryCount;
                InlineTextOffset = inlineTextOffset;
            }

            public string AddonName { get; }
            public string ChatCode { get; }
            public long[] TextNodeOffsets { get; }
            public bool AllowNodeSpeaker { get; }
            public long TalkBubbleEntriesOffset { get; }
            public int TalkBubbleEntrySize { get; }
            public long TalkBubbleTextNodeOffset { get; }
            public int TalkBubbleEntryCount { get; }

            /// <summary>The bubble's own node, which carries whether it is on screen.</summary>
            public long TalkBubbleResNodeOffset { get; }

            /// <summary>Offset of a Utf8String stored in the addon itself, or -1.</summary>
            public long InlineTextOffset { get; }

            /// <summary>
            /// Node ids holding the speaker and the line, or -1 when the addon is
            /// only addressable by offset. Ids belong to the UI layout, so unlike
            /// the offsets they survive a patch shifting the struct around - and
            /// they say which node is which instead of leaving it to be guessed
            /// from the order the fields happen to be declared in.
            /// </summary>
            public int SpeakerNodeId { get; }

            public int TextNodeId { get; }

            public static AddonRealtimeTextSpec Direct(
                string addonName,
                string chatCode,
                long[] textNodeOffsets,
                bool allowNodeSpeaker,
                int speakerNodeId = -1,
                int textNodeId = -1)
            {
                return new AddonRealtimeTextSpec(addonName, chatCode, textNodeOffsets, allowNodeSpeaker, -1, 0, -1, 0,
                    -1, speakerNodeId, textNodeId);
            }

            /// <summary>
            /// For addons that keep their line in an inline Utf8String rather than
            /// behind an AtkTextNode pointer.
            /// </summary>
            public static AddonRealtimeTextSpec InlineText(
                string addonName,
                string chatCode,
                long inlineTextOffset)
            {
                return new AddonRealtimeTextSpec(addonName, chatCode, Array.Empty<long>(), false, -1, 0, -1, 0,
                    inlineTextOffset, -1, -1);
            }

            public static AddonRealtimeTextSpec TalkBubbles(
                string addonName,
                string chatCode,
                long talkBubbleEntriesOffset,
                int talkBubbleEntrySize,
                long talkBubbleTextNodeOffset,
                int talkBubbleEntryCount,
                long talkBubbleResNodeOffset)
            {
                return new AddonRealtimeTextSpec(
                    addonName,
                    chatCode,
                    Array.Empty<long>(),
                    false,
                    talkBubbleEntriesOffset,
                    talkBubbleEntrySize,
                    talkBubbleTextNodeOffset,
                    talkBubbleEntryCount,
                    -1,
                    -1,
                    -1,
                    talkBubbleResNodeOffset);
            }
        }
    }

    internal readonly struct TalkAddonRealtimeDialogSnapshot
    {
        private const string DirectDialogCode = "003D";

        public bool SourceAvailable { get; }
        public string ChatCode { get; }
        public string SpeakerName { get; }
        public string TalkText { get; }

        private TalkAddonRealtimeDialogSnapshot(bool sourceAvailable, string chatCode, string speakerName,
            string talkText)
        {
            SourceAvailable = sourceAvailable;
            ChatCode = chatCode ?? string.Empty;
            SpeakerName = speakerName ?? string.Empty;
            TalkText = talkText ?? string.Empty;
        }

        public static TalkAddonRealtimeDialogSnapshot Unavailable()
        {
            return new TalkAddonRealtimeDialogSnapshot(false, string.Empty, string.Empty, string.Empty);
        }

        public static TalkAddonRealtimeDialogSnapshot Available(string talkText)
        {
            return Available(DirectDialogCode, string.Empty, talkText);
        }

        public static TalkAddonRealtimeDialogSnapshot Available(string chatCode, string speakerName, string talkText)
        {
            return new TalkAddonRealtimeDialogSnapshot(true, chatCode, speakerName, talkText);
        }
    }
}
