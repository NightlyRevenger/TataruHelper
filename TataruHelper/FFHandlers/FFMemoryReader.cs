using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using FFXIVTataruHelper.EventArguments;
using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.Settings;
using FFXIVTataruHelper.TataruComponentModel;
using FFXIVTataruHelper.WinUtils;

using Sharlayan.Core;
using Sharlayan.Models;
using Sharlayan.Models.ReadResults;

namespace FFXIVTataruHelper.FFHandlers
{
    public class FFMemoryReader : IFFMemoryReaderService
    {
        #region **Events.

        public event AsyncEventHandler<AsyncPropertyChangedEventArgs> AsyncPropertyChanged
        {
            add => _AsyncPropertyChanged.Register(value);
            remove => _AsyncPropertyChanged.Unregister(value);
        }

        private AsyncEvent<AsyncPropertyChangedEventArgs> _AsyncPropertyChanged;

        public event AsyncEventHandler<WindowStateChangeEventArgs> FFWindowStateChanged
        {
            add => _FFWindowStateChanged.Register(value);
            remove => _FFWindowStateChanged.Unregister(value);
        }

        private AsyncEvent<WindowStateChangeEventArgs> _FFWindowStateChanged;

        public event AsyncEventHandler<ChatMessageArrivedEventArgs> FFChatMessageArrived
        {
            add => _FFChatMessageArrived.Register(value);
            remove => _FFChatMessageArrived.Unregister(value);
        }

        private AsyncEvent<ChatMessageArrivedEventArgs> _FFChatMessageArrived;

        #endregion

        #region **Properties.

        public WindowState FFWindowState
        {
            get;
            private set
            {
                field = value;
                OnPropertyChanged();
            }
        }

        public bool IsGameRunning
        {
            get;
            private set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                OnPropertyChanged();
            }
        }

        public string GameProcessDescription
        {
            get;
            private set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                OnPropertyChanged();
            }
        } = string.Empty;

        /// <summary>
        /// The game's own window, for putting something over it. Zero while
        /// nothing is attached.
        /// </summary>
        public IntPtr GameWindowHandle
        {
            get
            {
                try
                {
                    return _ffXivProcess?.MainWindowHandle ?? IntPtr.Zero;
                }
                catch (InvalidOperationException)
                {
                    // The process went away between the check and the ask.
                    return IntPtr.Zero;
                }
            }
        }

        public Services.GameMemory.AddonBounds DialogueBounds =>
            _gameMemoryGateway?.DialogueBounds ?? Services.GameMemory.AddonBounds.Unknown;

        public DialogueSurface DialogueSurface =>
            _gameMemoryGateway?.DialogueSurface ?? DialogueSurface.None;

        public string CurrentDialogueLine => _gameMemoryGateway?.CurrentDialogueLine ?? string.Empty;

        public string CurrentDialogueSpeaker => _gameMemoryGateway?.CurrentDialogueSpeaker ?? string.Empty;

        public bool IsGameWindowForeground
        {
            get;
            private set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region **LocalVariables.

        private volatile bool _keepWorking;
        private volatile bool _keepReading;
        private readonly object _lifecycleSync = new object();
        private CancellationTokenSource _lifecycleCts;
        private Task _entryPointTask = Task.CompletedTask;
        private Task _watchWindowStateTask = Task.CompletedTask;
        private Task _chatMessageEventRiserTask = Task.CompletedTask;

        /// <summary>
        /// Reads dialogue from the game's UI as it appears rather than waiting for
        /// the chat log. Off falls back to chat-log-only behaviour.
        /// </summary>
        public bool IsRealtimeTranslationEnabled { get; set; } = true;

        /// <summary>
        /// Called once the character's name is known. Lines the game addresses
        /// to them carry it, and a hand-made translation of such a line cannot
        /// be recognised until the name can be written into it.
        /// </summary>
        public Action<string, bool?> PlayerNameResolved { get; set; }

        public Action<string> GameLanguageResolved { get; set; }

        private bool _playerNameResolved;

        private string _detectedGameLanguage = string.Empty;

        /// <summary>
        /// The language of the last game attached to, kept after it closes.
        ///
        /// Separate from <see cref="_detectedGameLanguage"/>, which is forgotten
        /// on purpose so that a game restarted in another language is detected
        /// afresh. This one is for the report: it is usually written once play
        /// has finished, and which language the lines were in explains half of
        /// what people ask about a translation.
        /// </summary>
        private string _lastKnownGameLanguage = string.Empty;

        /// <summary>
        /// Whether a game has been attached at any point since startup, which is
        /// what tells a report taken after the game was closed apart from one
        /// taken before it was ever started. The first has a session worth of
        /// evidence behind it; the second has nothing, and saying so is the whole
        /// answer.
        /// </summary>
        private bool _everAttached;

        /// <summary>
        /// The game side of a bug report, in one read. The character's name is
        /// deliberately not among it: whether it could be read is the diagnostic,
        /// and the name itself is nobody's business but its owner's.
        /// </summary>
        public GameReadingDiagnostics Reading
        {
            get
            {
                var live = _gameMemoryGateway?.LiveReading ?? LiveReadingStats.None;

                return new GameReadingDiagnostics(
                    IsGameRunning,
                    _everAttached,
                    GameProcessDescription,
                    _lastKnownGameLanguage,
                    _playerNameResolved,
                    IsRealtimeTranslationEnabled,
                    live.Lines,
                    live.Codes);
            }
        }

        private void ResolvePlayerNameOnce()
        {
            if (_playerNameResolved || PlayerNameResolved == null)
            {
                return;
            }

            var name = _gameMemoryGateway.GetPlayerName();
            if (string.IsNullOrWhiteSpace(name))
            {
                // Not logged in yet; ask again next sweep.
                return;
            }

            var isFeminine = _gameMemoryGateway.GetPlayerIsFeminine();

            _playerNameResolved = true;
            _logger.WriteLog("Player resolved: " + name + ", feminine: " + isFeminine);
            PlayerNameResolved(name, isFeminine);
        }

        private readonly ConcurrentDictionary<string, DateTime> _recentEmittedMessages;
        private static readonly TimeSpan DuplicateSuppressionWindow = TimeSpan.FromSeconds(2);

        /// <summary>
        /// How many signatures may pile up before the stale ones are swept.
        /// Two seconds of a busy zone is a few dozen lines, so this is roomy
        /// enough never to sweep in ordinary play and small enough that the
        /// dictionary cannot grow without bound.
        /// </summary>
        private const int MostSignaturesWorthKeeping = 512;

        private Process _ffXivProcess = null;
        private string _ffProcessName;

        private readonly List<IntPtr> _exclusionWindowHandlers;

        private readonly ConcurrentQueue<FFChatMsg> _ffxivChat;

        private readonly IGameMemoryGateway _gameMemoryGateway;
        private readonly IAppLogger _logger;
        private readonly ISettingsStore _settingsStore;

        #endregion

        public FFMemoryReader(IGameMemoryGateway gameMemoryGateway, IAppLogger logger, ISettingsStore settingsStore)
        {
            _gameMemoryGateway = gameMemoryGateway;
            _logger = logger;
            _settingsStore = settingsStore;
            _exclusionWindowHandlers = new List<IntPtr>();
            _ffxivChat = new ConcurrentQueue<FFChatMsg>();
            _recentEmittedMessages = new ConcurrentDictionary<string, DateTime>();

            _FFWindowStateChanged =
                new AsyncEvent<WindowStateChangeEventArgs>(EventErrorHandler, "FFWindowStateChanged");
            _FFChatMessageArrived =
                new AsyncEvent<ChatMessageArrivedEventArgs>(EventErrorHandler, "FFChatMessageArrived");
            _AsyncPropertyChanged =
                new AsyncEvent<AsyncPropertyChangedEventArgs>(EventErrorHandler,
                    "FFMemoryReader \n FFChatMessageArrived");

        }

        public void Start()
        {
            lock (_lifecycleSync)
            {
                if (_lifecycleCts != null && !_entryPointTask.IsCompleted)
                {
                    return;
                }

                _keepWorking = true;
                _keepReading = true;
                _lifecycleCts = new CancellationTokenSource();
                var lifecycleToken = _lifecycleCts.Token;

                FFWindowState = WindowState.Minimized;
                IsGameWindowForeground = false;

                _entryPointTask = Task.Run(async () =>
                {
                    try
                    {
                        await EntryPoint(lifecycleToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.WriteLog("FFMemoryReader.Start/EntryPoint canceled.");
                    }
                    catch (Exception e)
                    {
                        _logger.WriteLog("FFMemoryReader.Start/EntryPoint failed.");
                        _logger.WriteLog(e);
                    }
                }, lifecycleToken);
            }
        }

        public void AddExclusionWindowHandler(IntPtr handler)
        {
            _exclusionWindowHandlers.Add(handler);
        }

        private async Task EntryPoint(CancellationToken cancellationToken)
        {
            StartChatMessageEvetRiser(cancellationToken);

            while (_keepWorking && !cancellationToken.IsCancellationRequested)
            {
                await InitMemoryReader(cancellationToken);

                if (!_keepWorking || cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                StartWatchFFWindowState(cancellationToken);
                await ChatReader(cancellationToken);
            }
        }

        private async Task InitMemoryReader(CancellationToken cancellationToken)
        {
            try
            {
                var processNotFound = true;

                const string processName = "ffxiv_dx11";
                _ffProcessName = processName;

                while (_keepWorking && processNotFound && !cancellationToken.IsCancellationRequested)
                {
                    var processes = Process.GetProcessesByName(_ffProcessName);
                    try
                    {
                        if (processes.Length > 0)
                        {
                            try
                            {
                                // Read off the game rather than assumed. This was
                                // "English" whatever the client was set to, which
                                // is the wrong signatures and the wrong text for
                                // everybody playing in one of the other three.
                                // Supported: English, Chinese, Japanese, French,
                                // German, Korean.
                                var languageCode = GameClientLanguage.Detect(_logger);
                                var gameLanguage = GameClientLanguage.ReaderName(languageCode);
                                // whether to always hit API on start to get the latest sigs based on patchVersion, or use the local json cache (if the file doesn't exist, API will be hit)
                                const bool useLocalCache = true;
                                const bool scanAllMemoryRegions = false;
                                // patchVersion of game, or latest//
                                const string patchVersion = "latest";
                                var process = processes[0];

                                if (_ffXivProcess != null)
                                {
                                    _ffXivProcess.Dispose();
                                }

                                _ffXivProcess = process;
                                var processModel = new ProcessModel { Process = process };

                                _gameMemoryGateway.SetProcess(processModel, gameLanguage, patchVersion, useLocalCache,
                                    scanAllMemoryRegions);

                                processNotFound = false;
                                _keepReading = true;
                                _detectedGameLanguage = languageCode;
                                _lastKnownGameLanguage = languageCode;

                                GameLanguageResolved?.Invoke(languageCode);
                            }
                            catch (OperationCanceledException)
                            {
                                _logger.WriteLog("FFMemoryReader.InitMemoryReader process attach canceled.");
                                throw;
                            }
                            catch (Exception e)
                            {
                                await Task.Delay(_settingsStore.LookForProcessDelayMs, cancellationToken);
                                _logger.WriteLog("FFMemoryReader.InitMemoryReader process attach failed.");
                                _logger.WriteLog(e);
                            }
                        }
                        else
                        {
                            await Task.Delay(_settingsStore.LookForProcessDelayMs, cancellationToken);
                        }
                    }
                    finally
                    {
                        // The array hands out live process handles; keep the one
                        // now held by _ffXivProcess and release the rest, so a
                        // long search for a game that is not running does not
                        // leak a handle per poll.
                        for (var i = 0; i < processes.Length; i++)
                        {
                            if (!ReferenceEquals(processes[i], _ffXivProcess))
                            {
                                processes[i].Dispose();
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.WriteLog("FFMemoryReader.InitMemoryReader canceled.");
            }
            catch (Exception e)
            {
                _logger.WriteLog("FFMemoryReader.InitMemoryReader failed.");
                _logger.WriteLog(e);
            }
        }

        private void StartWatchFFWindowState(CancellationToken cancellationToken)
        {
            if (_watchWindowStateTask != null && !_watchWindowStateTask.IsCompleted)
            {
                return;
            }

            _watchWindowStateTask = Task.Run(
                () => WatchFFWindowStateLoop(cancellationToken),
                cancellationToken);
        }

        private async Task WatchFFWindowStateLoop(CancellationToken cancellationToken)
        {
            var ffxivPrevWindowState = WindowState.Minimized;
            FFWindowState = WindowState.Minimized;
            IsGameWindowForeground = false;

            var isRunningPrev = false;
            while (_keepWorking && _keepReading && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var ffxivWindowState = WindowState.Minimized;
                    var fgWindow = Win32Interfaces.GetForegroundWindow();
                    var isExclusionWindow = _exclusionWindowHandlers.Any(handler => handler == fgWindow);
                    var isGameWindowForeground = false;

                    if (_ffXivProcess != null)
                    {
                        var processWindowHandle = _ffXivProcess.MainWindowHandle;
                        if (processWindowHandle != IntPtr.Zero)
                        {
                            ffxivWindowState = Win32Interfaces.IsIconic(processWindowHandle)
                                ? WindowState.Minimized
                                : WindowState.Normal;

                            if (isExclusionWindow)
                            {
                                isGameWindowForeground = IsGameWindowForeground;
                            }
                            else
                            {
                                isGameWindowForeground = processWindowHandle == fgWindow;
                            }
                        }
                    }

                    IsGameWindowForeground = isGameWindowForeground;

                    var oldValue = ffxivPrevWindowState;
                    if (ffxivWindowState != ffxivPrevWindowState)
                    {
                        ffxivPrevWindowState = ffxivWindowState;

                        var ea = new WindowStateChangeEventArgs(this)
                        {
                            OldWindowState = oldValue,
                            NewWindowState = ffxivPrevWindowState,
                            IsRunningOld = isRunningPrev,
                            IsRunningNew = true,
                            Text = ""
                        };

                        _FFWindowStateChanged.InvokeAsync(ea).Forget();
                    }

                    FFWindowState = ffxivPrevWindowState;

                    // Sampled and released on the spot: holding live process
                    // handles across the delay would leak one per poll for as
                    // long as the game keeps running.
                    string gameProcessName = null;
                    int gameProcessId = 0;
                    {
                        var processes = Process.GetProcessesByName(_ffProcessName);
                        try
                        {
                            gameProcessName = processes.Length > 0 ? processes[0].ProcessName : null;
                            gameProcessId = processes.Length > 0 ? processes[0].Id : 0;
                        }
                        finally
                        {
                            foreach (var gameProcess in processes)
                            {
                                gameProcess.Dispose();
                            }
                        }
                    }

                    if (gameProcessName == null)
                    {
                        const WindowState oldState = WindowState.Normal;
                        var ea = new WindowStateChangeEventArgs(this)
                        {
                            OldWindowState = oldState,
                            NewWindowState = ffxivPrevWindowState,
                            IsRunningOld = isRunningPrev,
                            IsRunningNew = false,
                            Text = ""
                        };

                        _FFWindowStateChanged.InvokeAsync(ea).Forget();

                        _keepReading = false;

                        isRunningPrev = false;
                        IsGameRunning = false;
                        GameProcessDescription = string.Empty;

                        FFWindowState = WindowState.Minimized;
                        IsGameWindowForeground = false;

                        // The character and the language belonged to the process
                        // that has gone. Kept, they would be reported as current
                        // while nothing is attached - and the next game started
                        // could be a different character in a different language,
                        // which would then never be read because this said it
                        // already had been.
                        _playerNameResolved = false;
                        _detectedGameLanguage = string.Empty;

                        _gameMemoryGateway.UnsetProcess();
                    }
                    else
                    {
                        if (isRunningPrev == false)
                        {
                            const WindowState oldState = WindowState.Minimized;
                            const WindowState newState = WindowState.Normal;
                            var ea = new WindowStateChangeEventArgs(this)
                            {
                                OldWindowState = oldState,
                                NewWindowState = newState,
                                IsRunningOld = isRunningPrev,
                                IsRunningNew = true,
                                Text = gameProcessName + ".exe" + "  PID: " +
                                       gameProcessId.ToString()
                            };

                            _FFWindowStateChanged.InvokeAsync(ea).Forget();

                            FFWindowState = WindowState.Normal;
                        }

                        isRunningPrev = true;
                        IsGameRunning = true;
                        _everAttached = true;
                        GameProcessDescription = gameProcessName + ".exe" + "  PID: " +
                                                 gameProcessId.ToString();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.WriteLog("FFMemoryReader.WatchFFWindowState canceled.");
                    break;
                }
                catch (Exception e)
                {
                    _logger.WriteLog("FFMemoryReader.WatchFFWindowState failed.");
                    _logger.WriteLog(e);
                }

                await Task.Delay(_settingsStore.MemoryReaderDelayMs, cancellationToken);
            }
        }

        private async Task ChatReader(CancellationToken cancellationToken)
        {
            var previousArrayIndex = 0;
            var previousOffset = 0;

            while (_keepWorking && _keepReading && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var readResult = _gameMemoryGateway.GetChatLog(previousArrayIndex, previousOffset);
                    previousArrayIndex = readResult.PreviousArrayIndex;
                    previousOffset = readResult.PreviousOffset;

                    ProcessReadResult(readResult);
                }
                catch (OperationCanceledException)
                {
                    _logger.WriteLog("FFMemoryReader.ChatReader canceled.");
                    break;
                }
                catch (Exception e)
                {
                    _logger.WriteLog("FFMemoryReader.ChatReader read failed.");
                    _logger.WriteLog(e);
                }

                await Task.Delay(_settingsStore.MemoryReaderDelayMs, cancellationToken);
            }
        }

        private void ProcessReadResult(ChatLogResult readResult)
        {
            var chatLogEntries = readResult?.ChatLogItems?.ToArray() ?? Array.Empty<ChatLogItem>();

            foreach (var chatLogItem in chatLogEntries)
            {
                // While reading live, the chat log will repeat every NPC line the
                // moment the player clicks through it. Letting both in translated
                // and displayed each line twice.
                //
                // Only for a code we have actually managed to read off the screen,
                // though. Dropping it unconditionally meant that on a client whose
                // subtitles we cannot read - the one offset that has to be
                // re-derived by hand after a patch - cutscenes showed nothing at
                // all, when the chat log would have shown them a moment later.
                if (IsRealtimeTranslationEnabled &&
                    IsStoryDialogueCode(chatLogItem) &&
                    _gameMemoryGateway.HasReadCodeLive(chatLogItem.Code))
                {
                    continue;
                }

                ProcessChatMsg(chatLogItem);
            }

            if (!IsRealtimeTranslationEnabled)
            {
                return;
            }

            ResolvePlayerNameOnce();

            var directDialog = _gameMemoryGateway.GetDirectDialog();
            if (directDialog?.ChatLogItems == null || directDialog.ChatLogItems.Count == 0)
            {
                return;
            }

            foreach (var directItem in directDialog.ChatLogItems.ToArray())
            {
                if (IsStoryDialogueCode(directItem))
                {
                    ProcessChatMsg(directItem);
                }
            }
        }

        /// <summary>
        /// The two codes story dialogue arrives under: an NPC speaking, and a
        /// cutscene subtitle.
        ///
        /// One code per channel, whichever way the line reached us. The realtime
        /// reader used to relabel its lines F03D and F044 so that a window could
        /// tick the live copy and the chat-log copy separately - but the two are
        /// the same line, arriving either at once or after the player clicks
        /// through, and which of them happens is what the Real-Time Translation
        /// switch decides. Two ticks for one channel only ever misled the person
        /// reading the list.
        /// </summary>
        internal static bool IsStoryDialogueCode(ChatLogItem chatLogItem)
        {
            if (chatLogItem == null || string.IsNullOrEmpty(chatLogItem.Code))
            {
                return false;
            }

            return string.Equals(chatLogItem.Code, "003D", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(chatLogItem.Code, "0044", StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldSuppressAsDuplicate(ChatLogItem chatLogItem)
        {
            if (chatLogItem == null)
            {
                return true;
            }

            var normalizedLine = (chatLogItem.Line ?? string.Empty).Trim();
            if (normalizedLine.Length == 0)
            {
                return true;
            }

            var signature = string.Concat(chatLogItem.Code ?? string.Empty, "|", normalizedLine);
            var now = DateTime.UtcNow;
            if (_recentEmittedMessages.TryGetValue(signature, out var previousEmittedAt))
            {
                if ((now - previousEmittedAt) <= DuplicateSuppressionWindow)
                {
                    return true;
                }
            }

            _recentEmittedMessages[signature] = now;

            // Nothing here was ever thrown away: an entry per distinct line for
            // the whole session, each holding the line itself, to answer a
            // question that stops mattering after two seconds. An evening in a
            // busy zone put megabytes of chat into a dictionary that could
            // never use them again.
            if (_recentEmittedMessages.Count > MostSignaturesWorthKeeping)
            {
                ForgetStaleSignatures(_recentEmittedMessages, now, DuplicateSuppressionWindow);
            }

            return false;
        }

        /// <summary>
        /// Drops the signatures that have aged past the window they are asked
        /// about. Swept rather than pruned on every line: the sweep costs a
        /// walk of the dictionary, and doing that per line would spend more
        /// than it saves.
        /// </summary>
        internal static void ForgetStaleSignatures(
            ConcurrentDictionary<string, DateTime> seen, DateTime now, TimeSpan window)
        {
            if (seen == null)
            {
                return;
            }

            foreach (var entry in seen)
            {
                if (now - entry.Value > window)
                {
                    seen.TryRemove(entry.Key, out _);
                }
            }
        }

        private void StartChatMessageEvetRiser(CancellationToken cancellationToken)
        {
            if (_chatMessageEventRiserTask != null && !_chatMessageEventRiserTask.IsCompleted)
            {
                return;
            }

            _chatMessageEventRiserTask = Task.Run(
                () => ChatMessageEvetRiserLoop(cancellationToken),
                cancellationToken);
        }

        private async Task ChatMessageEvetRiserLoop(CancellationToken cancellationToken)
        {
            if (_FFChatMessageArrived.HandlersCount == 0)
            {
                while (_keepWorking && _FFChatMessageArrived.HandlersCount == 0 &&
                       !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(50, cancellationToken);
                }
            }

            while (_keepWorking && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_ffxivChat.TryDequeue(out var ffChatMsg))
                    {
                        var ea = new ChatMessageArrivedEventArgs(this) { ChatMessage = ffChatMsg };

                        await _FFChatMessageArrived.InvokeAsync(ea);
                    }
                    else
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.WriteLog("FFMemoryReader.ChatMessageEvetRiser canceled.");
                    break;
                }
                catch (Exception e)
                {
                    _logger.WriteLog("FFMemoryReader.ChatMessageEvetRiser failed.");
                    _logger.WriteLog(e);
                }
            }
        }

        private void ProcessChatMsg(ChatLogItem chatLogItem)
        {
            if (ShouldSuppressAsDuplicate(chatLogItem))
            {
                return;
            }

            var tmpMsg = new FFChatMsg(chatLogItem.Line, chatLogItem.Code, chatLogItem.TimeStamp);
            _ffxivChat.Enqueue(tmpMsg);
        }

        private void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            var eventArgs = new AsyncPropertyChangedEventArgs(this, prop);
            _AsyncPropertyChanged.InvokeAsync(eventArgs).Forget();
        }

        private void EventErrorHandler(string eventName, Exception ex)
        {
            var text = eventName + Environment.NewLine + Convert.ToString(ex);
            _logger.WriteLog(text);
        }

        public void Stop()
        {
            var pending = BeginStop();
            if (pending.BackgroundTasks == null)
            {
                return;
            }

            WaitForBackgroundTasks(pending.BackgroundTasks, TimeSpan.FromSeconds(5));
            FinishStop(pending.LifecycleCts);
        }

        public async Task StopAsync(TimeSpan timeout)
        {
            var pending = BeginStop();
            if (pending.BackgroundTasks == null)
            {
                return;
            }

            try
            {
                var tasksToWait = pending.BackgroundTasks.Where(t => t != null).ToArray();
                if (tasksToWait.Length > 0)
                {
                    var aggregated = Task.WhenAll(tasksToWait);
                    using var cts = new CancellationTokenSource(timeout);
                    try
                    {
                        await aggregated.WaitAsync(cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.WriteLog("FFMemoryReader.StopAsync timeout while waiting background tasks.");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.WriteLog("FFMemoryReader.StopAsync wait failed.");
                _logger.WriteLog(e);
            }

            FinishStop(pending.LifecycleCts);
        }

        private bool _isStopped;

        private readonly struct PendingStop
        {
            public PendingStop(Task[] tasks, CancellationTokenSource cts)
            {
                BackgroundTasks = tasks;
                LifecycleCts = cts;
            }

            public Task[] BackgroundTasks { get; }
            public CancellationTokenSource LifecycleCts { get; }
        }

        private PendingStop BeginStop()
        {
            Task[] backgroundTasks;
            CancellationTokenSource lifecycleCts;

            lock (_lifecycleSync)
            {
                if (_isStopped)
                {
                    return default;
                }

                _isStopped = true;
                _keepWorking = false;
                _keepReading = false;

                lifecycleCts = _lifecycleCts;
                _lifecycleCts = null;

                backgroundTasks = new[] { _entryPointTask, _watchWindowStateTask, _chatMessageEventRiserTask };

                _entryPointTask = Task.CompletedTask;
                _watchWindowStateTask = Task.CompletedTask;
                _chatMessageEventRiserTask = Task.CompletedTask;
            }

            try
            {
                lifecycleCts?.Cancel();
            }
            catch (Exception e)
            {
                _logger.WriteLog("FFMemoryReader.Stop cancel failed.");
                _logger.WriteLog(e);
            }

            return new PendingStop(backgroundTasks, lifecycleCts);
        }

        private void FinishStop(CancellationTokenSource lifecycleCts)
        {
            try
            {
                _gameMemoryGateway.UnsetProcess();
            }
            catch (Exception e)
            {
                _logger.WriteLog("FFMemoryReader.Stop UnsetProcess failed.");
                _logger.WriteLog(e);
            }
            finally
            {
                lifecycleCts?.Dispose();
            }
        }

        private void WaitForBackgroundTasks(Task[] backgroundTasks, TimeSpan timeout)
        {
            try
            {
                var tasksToWait = backgroundTasks?.Where(task => task != null).ToArray() ?? Array.Empty<Task>();
                if (tasksToWait.Length == 0)
                {
                    return;
                }

                var aggregatedTask = Task.WhenAll(tasksToWait);
                if (!aggregatedTask.Wait(timeout))
                {
                    _logger.WriteLog("FFMemoryReader.Stop timeout while waiting background tasks.");
                }
            }
            catch (AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                {
                    if (innerException is OperationCanceledException)
                    {
                        continue;
                    }

                    _logger.WriteLog("FFMemoryReader.Stop background task failed.");
                    _logger.WriteLog(innerException);
                }
            }
            catch (Exception e)
            {
                _logger.WriteLog("FFMemoryReader.Stop wait failed.");
                _logger.WriteLog(e);
            }
        }

        public void Dispose()
        {
            Stop();
            _ffXivProcess?.Dispose();
        }
    }
}