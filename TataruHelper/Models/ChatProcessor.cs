using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FFXIVTataruHelper.EventArguments;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.Settings;

using Translation;
using Translation.Models;

namespace FFXIVTataruHelper
{
    public class ChatProcessor
    {
        #region **Events.

        public event AsyncEventHandler<ChatMessageArrivedEventArgs> TextArrived
        {
            add { this._TextArrivedArrived.Register(value); }
            remove { this._TextArrivedArrived.Unregister(value); }
        }

        private AsyncEvent<ChatMessageArrivedEventArgs> _TextArrivedArrived;

        #endregion

        #region **Properties.

        public ReadOnlyCollection<TranslationEngine> TranslationEngines
        {
            get { return _WebTranslator.TranslationEngines; }
        }

        public ReadOnlyCollection<ChatMsgType> AllChatCodes
        {
            get
            {
                return new ReadOnlyCollection<ChatMsgType>(_AllChatCodes);
            }
        }

        #endregion

        #region **LocalVariables.

        WebTranslator _WebTranslator;

        DateTime _LastTranslationTime;

        List<ChatMsgType> _AllChatCodes;

        List<string> MsgBlackList;

        List<string> ChatCodesWithNickNames;

        ChatMessageFilter _ChatMessageFilter;

        readonly ISettingsStore _SettingsStore;
        readonly IAppLogger _Logger;

        private readonly object _translationBufferSync = new object();
        private readonly Dictionary<string, TranslationBufferState> _translationBufferStates;

        private readonly int _translationContextBufferWindowMs;
        private readonly int _translationContextMaxBatchSize;
        private readonly string _translationBatchDelimiter;

        #endregion

        public ChatProcessor(WebTranslator webTranslator, ISettingsStore settingsStore, IAppLogger logger)
        {
            this._TextArrivedArrived =
                new AsyncEvent<ChatMessageArrivedEventArgs>(this.EventErrorHandler, "TranslationArrived");

            _SettingsStore = settingsStore;
            _Logger = logger;

            _translationBufferStates = new Dictionary<string, TranslationBufferState>(StringComparer.Ordinal);
            _translationContextBufferWindowMs = Math.Max(0, settingsStore.AppSettings.TranslationContextBufferWindowMs);
            _translationContextMaxBatchSize = Math.Max(1, settingsStore.AppSettings.TranslationContextMaxBatchSize);
            _translationBatchDelimiter =
                string.IsNullOrEmpty(settingsStore.AppSettings.TranslationContextBatchDelimiter)
                    ? "\n<<<TATARU_TRANSLATION_SEGMENT>>>\n"
                    : settingsStore.AppSettings.TranslationContextBatchDelimiter;

            _AllChatCodes = Helper.LoadJsonData<List<ChatMsgType>>(_SettingsStore.ChatCodesFilePath);

            _WebTranslator = webTranslator;

            MsgBlackList = new List<string>();

            Init();

            _LastTranslationTime = DateTime.UtcNow;
        }

        private void Init()
        {
            MsgBlackList = Helper.LoadJsonData<List<string>>(_SettingsStore.BlackListPath);
            if (MsgBlackList == null)
            {
                _Logger.WriteLog("ChatProcessor: message blacklist not found at " + _SettingsStore.BlackListPath);
                MsgBlackList = new List<string>();
            }

            MsgBlackList = MsgBlackList.Distinct().ToList();

            for (int i = 0; i < MsgBlackList.Count; i++)
            {
                MsgBlackList[i] = Helper.ClearBlackListString(MsgBlackList[i]);
            }

            ChatCodesWithNickNames = Helper.LoadJsonData<List<string>>(_SettingsStore.IgnoreNickNameChatCodesPath);
            if (ChatCodesWithNickNames == null)
            {
                _Logger.WriteLog("ChatProcessor: nickname chat code list not found at " +
                                 _SettingsStore.IgnoreNickNameChatCodesPath);
                ChatCodesWithNickNames = new List<string>();
            }

            ChatCodesWithNickNames = ChatCodesWithNickNames.Distinct().ToList();

            _ChatMessageFilter = new ChatMessageFilter(MsgBlackList, ChatCodesWithNickNames);
        }

        public async Task OnFFChatMessageArrived(ChatMessageArrivedEventArgs ea)
        {
            ChatMsgType msgType = new ChatMsgType();
            var shouldTranslate = _ChatMessageFilter.ShouldTranslate(ea.ChatMessage.Text);

            Logger.WriteRawDialogLog(
                $"ChatReceive code=[{ea.ChatMessage.Code}] text=[{ea.ChatMessage.Text}] shouldTranslate=[{shouldTranslate}]");

            if (shouldTranslate)
                await ProcessChatMsg(ea, msgType);

            if (ShouldLogToChatLog(ea.ChatMessage.Code))
            {
                _Logger.WriteChatLog(String.Format("{0} {1}: {2}", ea.ChatMessage.TimeStamp, ea.ChatMessage.Code,
                    ea.ChatMessage.Text));
            }
        }

        /// <summary>
        /// The codes the story is told in: an NPC speaking, a cutscene subtitle,
        /// narration, and what a boss shouts.
        /// </summary>
        private static readonly HashSet<string> StoryChatCodes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "003D", "0044", "0039", "2AB9" };

        /// <summary>
        /// Whether this line belongs in the log a bug report carries.
        /// </summary>
        /// <remarks>
        /// The story codes go in every session, rather than only when somebody
        /// thought to pass -logall - which nobody reporting a fault ever has.
        /// When a line comes out wrong there is no way back to it: the
        /// conversation is over, the game's own log has scrolled, and asking the
        /// player to reproduce it means asking them to replay a quest.
        ///
        /// The rest of the chat log stays behind the switch, and that is a
        /// deliberate limit rather than an oversight. It carries what other
        /// people said in party chat and in tells, and this file is now offered
        /// up for sending to a stranger who is helping with a fault. Somebody
        /// diagnosing a translation has no business reading either, and the
        /// person pressing the button should not have to know that they would.
        /// </remarks>
        private static bool ShouldLogToChatLog(string chatCode)
        {
            return CmdArgsStatus.LogAllChat
                   || CmdArgsStatus.LogPlotChat
                   || (!string.IsNullOrEmpty(chatCode) && StoryChatCodes.Contains(chatCode));
        }

        public async Task<TranslationResult> Translate(string inSentence, TranslationEngine translationEngine,
            TranslatorLanguage fromLang, TranslatorLanguage toLang, string chatCode,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            string nickName;
            string sentenceToTranslate;
            var nicknameWasSplit =
                _ChatMessageFilter.TrySplitNickname(chatCode, inSentence, out nickName, out sentenceToTranslate);
            var isPlayerChat = ChatMessageFilter.IsPlayerChatCode(chatCode);

            Logger.WriteRawDialogLog(
                $"ChatTranslateStart code=[{chatCode}] input=[{inSentence}] split=[{nicknameWasSplit}] " +
                $"playerChat=[{isPlayerChat}] nickname=[{nickName}] body=[{sentenceToTranslate}] " +
                $"translatePlayerNicknames=[{TranslatePlayerNicknames}] " +
                $"translateSpeakerNames=[{TranslateSpeakerNames}] engine=[{translationEngine?.EngineName}] " +
                $"from=[{fromLang?.LanguageCode}] to=[{toLang?.LanguageCode}]");

            var batchKey = BuildTranslationBatchKey(chatCode, nickName, translationEngine, fromLang, toLang);
            var result = await QueueForBatchedTranslation(
                sentenceToTranslate,
                batchKey,
                translationEngine,
                fromLang,
                toLang,
                cancellationToken);

            if (!result.IsSuccess || result.Text.Length == 0)
            {
                Logger.WriteRawDialogLog(
                    $"ChatTranslateBodyResult success=[{result.IsSuccess}] engine=[{result.Engine}] " +
                    $"failure=[{result.FailureKind}] reason=[{result.FailureReason}] text=[{result.Text}]");
                return result;
            }

            if (nickName.Length > 0 &&
                (isPlayerChat ? TranslatePlayerNicknames : TranslateSpeakerNames))
            {
                nickName = await ResolveSpeakerName(nickName, translationEngine, fromLang, toLang, cancellationToken);
            }

            var line = nickName.Length > 0 ? nickName + " " + result.Text : result.Text;

            // The marker goes on the machine translation rather than the
            // hand-made one: in story dialogue the hand-made answer is the
            // common case, and marking the common case is just clutter.
            if (MarkMachineTranslation && !result.IsLiterary)
            {
                line = MachineTranslationMarker + line;
            }

            var rendered = result.WithText(line).WithSpeaker(nickName);
            Logger.WriteRawDialogLog(
                $"ChatTranslateResult success=[{rendered.IsSuccess}] engine=[{rendered.Engine}] " +
                $"literary=[{rendered.IsLiterary}] speaker=[{rendered.SpeakerName}] text=[{rendered.Text}]");
            return rendered;
        }

        /// <summary>
        /// Renders the speaker's name in the reading language.
        ///
        /// The translators have named most of the cast, and their spelling is
        /// the one a reader will recognise. Failing that the engine is asked,
        /// which at least keeps the name in the same alphabet as the line - and
        /// its answer is cached, so a character costs one request however much
        /// they talk.
        /// </summary>
        private async Task<string> ResolveSpeakerName(
            string nickName,
            TranslationEngine translationEngine,
            TranslatorLanguage fromLang,
            TranslatorLanguage toLang,
            CancellationToken cancellationToken)
        {
            // The speaker arrives punctuated the way it will be shown - "Cid:" -
            // and only the name itself is looked up.
            var trimmedNickname = nickName.Trim();
            var trailing = trimmedNickname.StartsWith("(")
                ? string.Empty
                : trimmedNickname.EndsWith("\uFF1A", StringComparison.Ordinal) ? "\uFF1A" : ":";
            var name = trimmedNickname.TrimEnd(':', '\uFF1A').Trim();
            if (name.Length == 0)
            {
                return nickName;
            }

            if (_WebTranslator.TryGetReferenceSpeakerName(name, fromLang, toLang, out var known))
            {
                Logger.WriteRawDialogLog($"ChatSpeakerReference name=[{name}] translated=[{known}]");
                return known + trailing;
            }

            Logger.WriteRawDialogLog(
                $"ChatSpeakerTranslateStart name=[{name}] engine=[{translationEngine?.EngineName}] " +
                $"from=[{fromLang?.LanguageCode}] to=[{toLang?.LanguageCode}]");
            var translated = await _WebTranslator
                .TranslateAsync(name, translationEngine, fromLang, toLang, cancellationToken)
                .ConfigureAwait(false);

            var resolved = translated.IsSuccess && translated.Text.Length > 0
                ? translated.Text.Trim() + trailing
                : nickName;
            Logger.WriteRawDialogLog(
                $"ChatSpeakerTranslateResult success=[{translated.IsSuccess}] engine=[{translated.Engine}] " +
                $"failure=[{translated.FailureKind}] reason=[{translated.FailureReason}] " +
                $"text=[{translated.Text}] rendered=[{resolved}]");
            return resolved;
        }

        /// <summary>
        /// Whether the speaker's name is shown in the reading language too. A
        /// line that reads "Матушка Миунна: ..." beats one that switches
        /// alphabet halfway through.
        /// </summary>
        public bool TranslateSpeakerNames { get; set; }

        /// <summary>Whether player-chat sender prefixes are translated into the reading language.</summary>
        public bool TranslatePlayerNicknames { get; set; }

        /// <summary>Prefix shown on lines an engine translated, when asked for.</summary>
        internal const string MachineTranslationMarker = "• ";

        /// <summary>
        /// Whether an engine's translation is marked as such. Nothing about the
        /// text says where it came from, and being unable to tell was the whole
        /// complaint.
        /// </summary>
        public bool MarkMachineTranslation { get; set; }

        private async Task<TranslationResult> QueueForBatchedTranslation(
            string sentenceToTranslate,
            string batchKey,
            TranslationEngine translationEngine,
            TranslatorLanguage fromLang,
            TranslatorLanguage toLang,
            CancellationToken cancellationToken)
        {
            var request = new BufferedTranslationRequest(sentenceToTranslate, cancellationToken);
            List<BufferedTranslationRequest> batchToFlush = null;

            lock (_translationBufferSync)
            {
                TranslationBufferState state;
                if (!_translationBufferStates.TryGetValue(batchKey, out state))
                {
                    state = new TranslationBufferState();
                    _translationBufferStates[batchKey] = state;
                }

                state.PendingRequests.Add(request);

                if (state.PendingRequests.Count >= _translationContextMaxBatchSize)
                {
                    batchToFlush = TakeBatch(state, _translationContextMaxBatchSize);
                    if (state.PendingRequests.Count == 0)
                    {
                        CancelDelayedFlush(state);
                        _translationBufferStates.Remove(batchKey);
                    }
                    else
                    {
                        EnsureDelayedFlushScheduled(batchKey, state, translationEngine, fromLang, toLang);
                    }
                }
                else
                {
                    EnsureDelayedFlushScheduled(batchKey, state, translationEngine, fromLang, toLang);
                }
            }

            if (batchToFlush != null)
            {
                ObserveBackgroundTask(
                    FlushBatchAsync(batchKey, batchToFlush, translationEngine, fromLang, toLang),
                    "translation batch flush");
            }

            return await request.CompletionSource.Task;
        }

        private void EnsureDelayedFlushScheduled(
            string batchKey,
            TranslationBufferState state,
            TranslationEngine translationEngine,
            TranslatorLanguage fromLang,
            TranslatorLanguage toLang)
        {
            if (state.DelayCts != null)
                return;

            state.DelayCts = new CancellationTokenSource();
            var delayToken = state.DelayCts.Token;

            ObserveBackgroundTask(Task.Run(async () =>
            {
                try
                {
                    if (_translationContextBufferWindowMs > 0)
                    {
                        await Task.Delay(_translationContextBufferWindowMs, delayToken);
                    }

                    List<BufferedTranslationRequest> batch;

                    lock (_translationBufferSync)
                    {
                        TranslationBufferState currentState;
                        if (!_translationBufferStates.TryGetValue(batchKey, out currentState))
                        {
                            return;
                        }

                        if (!ReferenceEquals(currentState.DelayCts, state.DelayCts))
                        {
                            return;
                        }

                        currentState.DelayCts = null;

                        if (currentState.PendingRequests.Count == 0)
                        {
                            _translationBufferStates.Remove(batchKey);
                            return;
                        }

                        batch = TakeBatch(currentState, _translationContextMaxBatchSize);

                        if (currentState.PendingRequests.Count > 0)
                        {
                            EnsureDelayedFlushScheduled(batchKey, currentState, translationEngine, fromLang, toLang);
                        }
                        else
                        {
                            _translationBufferStates.Remove(batchKey);
                        }
                    }

                    await FlushBatchAsync(batchKey, batch, translationEngine, fromLang, toLang);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    // A batch that stops here never completes on its own, and
                    // the lines still buffered for this key would wait forever
                    // without being shown and without an error. Fail the ones
                    // still owned by this task.
                    _Logger.WriteLog($"Delayed translation flush for '{batchKey}' faulted.");
                    _Logger.WriteLog(exception);

                    lock (_translationBufferSync)
                    {
                        FailPendingRequests(_translationBufferStates, batchKey, translationEngine, exception);
                    }
                }
            }), "delayed translation flush");
        }

        private async Task FlushBatchAsync(
            string batchKey,
            List<BufferedTranslationRequest> requests,
            TranslationEngine translationEngine,
            TranslatorLanguage fromLang,
            TranslatorLanguage toLang)
        {
            if (requests == null || requests.Count == 0)
            {
                return;
            }

            var activeRequests = requests.Where(x => !x.CancellationToken.IsCancellationRequested).ToList();
            if (activeRequests.Count == 0)
            {
                foreach (var request in requests)
                {
                    request.CompletionSource.TrySetCanceled(request.CancellationToken);
                }

                return;
            }

            try
            {
                if (activeRequests.Count == 1)
                {
                    await TranslateSingleRequest(activeRequests[0], translationEngine, fromLang, toLang);
                }
                else
                {
                    await TranslateBatchRequests(activeRequests, translationEngine, fromLang, toLang);
                }

                // HashSet rather than List.Contains: this loop already walks
                // the whole batch, and Contains would walk it again per line.
                var completedRequests = new HashSet<BufferedTranslationRequest>(activeRequests);

                foreach (var request in requests)
                {
                    if (!completedRequests.Contains(request) && !request.CompletionSource.Task.IsCompleted)
                    {
                        request.CompletionSource.TrySetCanceled(request.CancellationToken);
                    }
                }
            }
            catch (Exception exception)
            {
                _Logger.WriteLog($"Failed to flush translation batch '{batchKey}'.");
                _Logger.WriteLog(exception);

                foreach (var request in requests)
                {
                    if (request.CancellationToken.IsCancellationRequested)
                    {
                        request.CompletionSource.TrySetCanceled(request.CancellationToken);
                    }
                    else
                    {
                        request.CompletionSource.TrySetResult(TranslationResult.Failure(
                            translationEngine?.EngineName ?? default,
                            TranslationFailureKind.ProviderException,
                            exception.Message));
                    }
                }
            }
        }

        private async Task TranslateSingleRequest(
            BufferedTranslationRequest request,
            TranslationEngine translationEngine,
            TranslatorLanguage fromLang,
            TranslatorLanguage toLang)
        {
            if (request.CancellationToken.IsCancellationRequested)
            {
                request.CompletionSource.TrySetCanceled(request.CancellationToken);
                return;
            }

            var result = await _WebTranslator.TranslateAsync(
                request.InputText,
                translationEngine,
                fromLang,
                toLang,
                request.CancellationToken);

            request.CompletionSource.TrySetResult(result);
        }

        private async Task TranslateBatchRequests(
            IReadOnlyList<BufferedTranslationRequest> requests,
            TranslationEngine translationEngine,
            TranslatorLanguage fromLang,
            TranslatorLanguage toLang)
        {
            var combinedInput =
                string.Join(_translationBatchDelimiter, requests.Select(x => x.InputText ?? string.Empty));

            var combinedResult = await _WebTranslator.TranslateAsync(
                combinedInput,
                translationEngine,
                fromLang,
                toLang,
                CancellationToken.None);

            if (combinedResult.IsSuccess && TrySplitBatchedTranslation(
                    combinedResult.Text,
                    _translationBatchDelimiter,
                    requests.Count,
                    out var translatedSegments))
            {
                for (int i = 0; i < requests.Count; i++)
                {
                    requests[i].CompletionSource.TrySetResult(
                        TranslationResult.Success(combinedResult.Engine, translatedSegments[i]));
                }

                return;
            }

            for (int i = 0; i < requests.Count; i++)
            {
                var request = requests[i];

                // The combined call is one shot for everybody, so it stays
                // uncancelled. The fallback is per line, so a line cancelled
                // while the batch was in flight does not burn the quota
                // translating something nobody will read.
                if (request.CancellationToken.IsCancellationRequested)
                {
                    continue;
                }

                var result = await _WebTranslator.TranslateAsync(
                    request.InputText ?? string.Empty,
                    translationEngine,
                    fromLang,
                    toLang,
                    request.CancellationToken);

                request.CompletionSource.TrySetResult(result);
            }
        }

        internal static bool TrySplitBatchedTranslation(
            string combinedTranslation,
            string delimiter,
            int expectedCount,
            out List<string> translatedSegments)
        {
            translatedSegments = null;

            if (expectedCount <= 0)
            {
                translatedSegments = new List<string>();
                return true;
            }

            if (string.IsNullOrEmpty(combinedTranslation) || string.IsNullOrEmpty(delimiter))
            {
                return false;
            }

            var segments = combinedTranslation.Split(new[] { delimiter }, StringSplitOptions.None)
                .Select(x => x ?? string.Empty)
                .ToList();

            if (segments.Count != expectedCount)
            {
                return false;
            }

            translatedSegments = segments;
            return true;
        }

        private static List<BufferedTranslationRequest> TakeBatch(TranslationBufferState state, int maxBatchSize)
        {
            var count = Math.Min(maxBatchSize, state.PendingRequests.Count);
            var batch = state.PendingRequests.Take(count).ToList();
            state.PendingRequests.RemoveRange(0, count);
            return batch;
        }

        private static void CancelDelayedFlush(TranslationBufferState state)
        {
            if (state.DelayCts == null)
            {
                return;
            }

            state.DelayCts.Cancel();
            state.DelayCts.Dispose();
            state.DelayCts = null;
        }

        /// <summary>
        /// The dead-task net. A state whose delay token is still set has no
        /// successor task - the one that was to flush it faulted - so whatever
        /// is buffered there ends in a failure instead of a silent forever.
        /// A state the task already rescheduled (token cleared, a new delay
        /// set for the remainder) is left alone: it belongs to a live task.
        /// </summary>
        internal static bool FailPendingRequests(
            Dictionary<string, TranslationBufferState> states,
            string batchKey,
            TranslationEngine translationEngine,
            Exception exception)
        {
            TranslationBufferState state;
            if (!states.TryGetValue(batchKey, out state))
            {
                return false;
            }

            if (state.DelayCts == null)
            {
                return false;
            }

            state.DelayCts.Dispose();
            state.DelayCts = null;

            foreach (var request in state.PendingRequests)
            {
                request.CompletionSource.TrySetResult(TranslationResult.Failure(
                    translationEngine?.EngineName ?? default,
                    TranslationFailureKind.ProviderUnavailable,
                    exception?.Message ?? string.Empty));
            }

            states.Remove(batchKey);
            return true;
        }

        private static string BuildTranslationBatchKey(
            string chatCode,
            string nickName,
            TranslationEngine translationEngine,
            TranslatorLanguage fromLang,
            TranslatorLanguage toLang)
        {
            return string.Join("|",
                new[]
                {
                    chatCode ?? string.Empty, nickName ?? string.Empty,
                    translationEngine?.EngineName.ToString() ?? string.Empty,
                    fromLang?.LanguageCode ?? string.Empty, toLang?.LanguageCode ?? string.Empty
                });
        }

        private async Task ProcessChatMsg(ChatMessageArrivedEventArgs ea, ChatMsgType msgType)
        {
            switch (msgType.MsgType)
            {
                default:
                    {
                        var translation = new ChatMessageArrivedEventArgs(ea);

                        await _TextArrivedArrived.InvokeAsync(translation);

                        break;
                    }
            }
        }

        private void ObserveBackgroundTask(Task task, string operationName)
        {
            task.ContinueWith(
                t =>
                {
                    _Logger.WriteLog($"Background {operationName} faulted.");
                    _Logger.WriteLog(t.Exception);
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            _Logger.WriteLog(text);
        }

        internal sealed class TranslationBufferState
        {
            public List<BufferedTranslationRequest> PendingRequests { get; } = new List<BufferedTranslationRequest>();

            public CancellationTokenSource DelayCts { get; set; }
        }

        internal sealed class BufferedTranslationRequest
        {
            public string InputText { get; }

            public CancellationToken CancellationToken { get; }

            public TaskCompletionSource<TranslationResult> CompletionSource { get; }

            public BufferedTranslationRequest(string inputText, CancellationToken cancellationToken)
            {
                InputText = inputText ?? string.Empty;
                CancellationToken = cancellationToken;
                CompletionSource =
                    new TaskCompletionSource<TranslationResult>(TaskCreationOptions.RunContinuationsAsynchronously);

                if (cancellationToken.CanBeCanceled)
                {
                    var registration =
                        cancellationToken.Register(() => CompletionSource.TrySetCanceled(cancellationToken));
                    CompletionSource.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
                }
            }
        }
    }
}
