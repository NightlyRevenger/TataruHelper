// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFXIITataruHelper.EventArguments;
using FFXIITataruHelper.WinUtils;
using Sharlayan;
using Sharlayan.Core;
using Sharlayan.Models;
using Sharlayan.Models.ReadResults;

namespace FFXIITataruHelper.FFHandlers
{
    public class FFMemoryReader : IDisposable
    {
        #region **Events.

        public event AsyncEventHandler<WindowStateChangeEventArgs> FFWindowStateChanged
        {
            add { this._FFWindowStateChanged.Register(value); }
            remove { this._FFWindowStateChanged.Unregister(value); }
        }
        private AsyncEvent<WindowStateChangeEventArgs> _FFWindowStateChanged;

        public event AsyncEventHandler<ChatMessageArrivedEventArgs> FFChatMessageArrived
        {
            add { this._FFChatMessageArrived.Register(value); }
            remove { this._FFChatMessageArrived.Unregister(value); }
        }
        private AsyncEvent<ChatMessageArrivedEventArgs> _FFChatMessageArrived;

        #endregion

        #region **Properties.

        public System.Windows.WindowState FFWindowState { get; private set; }

        public bool UseDirectReading
        {
            get { return _UseDirectReading; }
            set { _UseDirectReading = value; }
        }

        #endregion

        #region **LocalVariables.

        bool _KeepWorking;
        bool _KeepReading;

        bool _UseDirectReadingInternal;

        bool _UseDirectReading;

        int DirectTextsMissedCount;

        private Process _FfXivProcess = null;
        private string _FfProcessName;

        private List<IntPtr> _ExclusionWindowHandlers;

        private ConcurrentQueue<FFChatMsg> _FFxivChat;

        #endregion

        public FFMemoryReader()
        {
            _ExclusionWindowHandlers = new List<IntPtr>();
            _FFxivChat = new ConcurrentQueue<FFChatMsg>();

            _FFWindowStateChanged = new AsyncEvent<WindowStateChangeEventArgs>(EventErrorHandler, "FFWindowStateChanged");
            _FFChatMessageArrived = new AsyncEvent<ChatMessageArrivedEventArgs>(EventErrorHandler, "FFChatMessageArrived");

            _UseDirectReadingInternal = true;
            DirectTextsMissedCount = 0;
        }

        public void Start()
        {
            _KeepWorking = true;
            _KeepReading = true;

            FFWindowState = System.Windows.WindowState.Minimized;

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    await EntryPoint();
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                }

            }, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            _KeepWorking = false;
            _KeepReading = false;
        }

        public void AddExclusionWindowHandler(IntPtr handler)
        {
            _ExclusionWindowHandlers.Add(handler);
        }

        private async Task EntryPoint()
        {
            ChatMessageEvetRiser();

            //SendDebugMessages();

            while (_KeepWorking)
            {

                await InitMemoryReader();

                WatchFFWindowState();

                await ChatReader();
            }
        }

        private async Task InitMemoryReader()
        {
            try
            {
                bool porcessNotFound = true;

                string processName1 = "ffxiv_dx11";
                _FfProcessName = processName1;

                while (_KeepWorking && porcessNotFound)
                {
                    Process[] processes = Process.GetProcessesByName(_FfProcessName);
                    if (processes.Length > 0)
                    {
                        try
                        {
                            // supported: English, Chinese, Japanese, French, German, Korean
                            string gameLanguage = "English";
                            // whether to always hit API on start to get the latest sigs based on patchVersion, or use the local json cache (if the file doesn't exist, API will be hit)
                            bool useLocalCache = true;
                            // patchVersion of game, or latest//
                            string patchVersion = "latest";
                            Process process = processes[0];

                            if (_FfXivProcess != null)
                                _FfXivProcess.Dispose();

                            _FfXivProcess = process;
                            ProcessModel processModel = new ProcessModel
                            {
                                Process = process,
                                IsWin64 = true
                            };

                            MemoryHandler.Instance.SetProcess(processModel, gameLanguage, patchVersion, useLocalCache);

                            porcessNotFound = false;
                            _KeepReading = true;
                        }
                        catch (Exception e)
                        {
                            await Task.Delay(GlobalSettings.LookForPorcessDelay);
                            Logger.WriteLog(e);
                        }
                    }
                    else
                    {
                        await Task.Delay(GlobalSettings.LookForPorcessDelay);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        private void WatchFFWindowState()
        {
            Task.Factory.StartNew(async () =>
            {
                System.Windows.WindowState FFXIVPrevWindowState = System.Windows.WindowState.Minimized;
                FFWindowState = System.Windows.WindowState.Minimized;

                bool _isRunningPrev = false;
                while (_KeepWorking && _KeepReading)
                {
                    try
                    {
                        bool IsExclusionWindow = false;

                        System.Windows.WindowState FFXIVWindowState = System.Windows.WindowState.Normal;
                        var fgWindow = Win32Interfaces.GetForegroundWindow();

                        if (_FfXivProcess != null)
                        {
                            if (_FfXivProcess.MainWindowHandle != fgWindow)
                                FFXIVWindowState = System.Windows.WindowState.Minimized;
                            else
                                FFXIVWindowState = System.Windows.WindowState.Normal;
                        }

                        for (int i = 0; i < _ExclusionWindowHandlers.Count; i++)
                        {
                            if (fgWindow == _ExclusionWindowHandlers[i])
                            {
                                IsExclusionWindow = true;
                                break;
                            }
                        }

                        if (!IsExclusionWindow && fgWindow != IntPtr.Zero)
                        {
                            var oldValue = FFXIVPrevWindowState;

                            if (FFXIVWindowState != FFXIVPrevWindowState)
                            {
                                FFXIVPrevWindowState = FFXIVWindowState;

                                var ea = new WindowStateChangeEventArgs(this)
                                {
                                    OldWindowState = oldValue,
                                    NewWindowState = FFXIVPrevWindowState,

                                    IsRunningOld = _isRunningPrev,
                                    IsRunningNew = true,

                                    Text = ""
                                };

                                _FFWindowStateChanged.InvokeAsync(ea);
                            }

                            FFWindowState = FFXIVPrevWindowState;
                        }

                        Process[] processes = Process.GetProcessesByName(_FfProcessName);
                        if (processes.Length == 0)
                        {
                            System.Windows.WindowState oldState = System.Windows.WindowState.Normal;
                            var ea = new WindowStateChangeEventArgs(this)
                            {
                                OldWindowState = oldState,
                                NewWindowState = FFXIVPrevWindowState,

                                IsRunningOld = _isRunningPrev,
                                IsRunningNew = false,
                                Text = ""
                            };

                            _FFWindowStateChanged.InvokeAsync(ea);

                            _KeepReading = false;

                            _isRunningPrev = false;

                            FFWindowState = System.Windows.WindowState.Minimized;

                            MemoryHandler.Instance.UnsetProcess();
                        }
                        else
                        {
                            if (_isRunningPrev == false)
                            {
                                System.Windows.WindowState oldState = System.Windows.WindowState.Minimized;
                                System.Windows.WindowState newState = System.Windows.WindowState.Normal;
                                var ea = new WindowStateChangeEventArgs(this)
                                {
                                    OldWindowState = oldState,
                                    NewWindowState = newState,

                                    IsRunningOld = _isRunningPrev,
                                    IsRunningNew = true,
                                    Text = processes[0].ProcessName + ".exe" + "  PID: " + processes[0].Id.ToString()
                                };

                                _FFWindowStateChanged.InvokeAsync(ea);

                                FFWindowState = System.Windows.WindowState.Normal;
                            }

                            _isRunningPrev = true;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(Convert.ToString(e));
                    }

                    await Task.Delay(GlobalSettings.MemoryReaderDelay);
                }

            }, TaskCreationOptions.LongRunning);
        }

        private async Task ChatReader()
        {
            int _previousArrayIndex = 0;
            int _previousOffset = 0;

            ChatLogResult previousPanelResults = new ChatLogResult();

            while (_KeepWorking && _KeepReading)
            {
                try
                {
                    ChatLogResult readResult = Reader.GetChatLog(_previousArrayIndex, _previousOffset);
                    _previousArrayIndex = readResult.PreviousArrayIndex;
                    _previousOffset = readResult.PreviousOffset;

                    if (_UseDirectReadingInternal && _UseDirectReading)
                    {
                        var directDialog = Reader.GetDirectDialog();
                        readResult.ChatLogItems.AddRange(directDialog.ChatLogItems);

                        ClearMessagesList(readResult, previousPanelResults, directDialog);
                    }
                    if (DirectTextsMissedCount > GlobalSettings.MaxСonsecutiveNotFromLogSentences)
                        _UseDirectReadingInternal = false;

                    var chatLogEntries = readResult.ChatLogItems;

                    if (readResult.ChatLogItems.Count > 0)
                    {
                        for (int i = 0; i < readResult.ChatLogItems.Count; i++)
                        {
                            ProcessChatMsg(readResult.ChatLogItems[i]);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLog(Convert.ToString(e));
                }

                await Task.Delay(GlobalSettings.MemoryReaderDelay);
            }
        }

        void ClearMessagesList(ChatLogResult chatLogResult, ChatLogResult previousPanelResults, ChatLogResult panelResult)
        {
            bool messgaesDeleted = false;
            bool previousCount = previousPanelResults.ChatLogItems.Count > 0 && chatLogResult.ChatLogItems.Count > 0;
            for (int i = 0; i < previousPanelResults.ChatLogItems.Count; i++)
            {
                var pvPanel = previousPanelResults.ChatLogItems[i];
                var panel = chatLogResult.ChatLogItems.FirstOrDefault(x => Helper.IsStringLettersEqual(x.Line, pvPanel.Line));
                if (panel != null)
                {
                    if (panelResult.ChatLogItems.FirstOrDefault(x => Helper.IsStringLettersEqual(x.Line, panel.Line)) == null)
                        chatLogResult.ChatLogItems.Remove(panel);

                    var rmCount = previousPanelResults.ChatLogItems.RemoveAll(x => Helper.IsStringLettersEqual(x.Line, panel.Line));
                    messgaesDeleted = true;

                    if (i - rmCount > -2)
                        i = i - rmCount;
                }
            }

            if (previousCount)
            {
                if (messgaesDeleted == false)
                    DirectTextsMissedCount++;
                else
                    DirectTextsMissedCount = 0;
            }

            if (previousPanelResults.ChatLogItems.Count > 200)
            {
                int startPos = 0;
                int count = previousPanelResults.ChatLogItems.Count / 2;
                previousPanelResults.ChatLogItems.RemoveRange(startPos, count);
            }

            previousPanelResults.ChatLogItems.AddRange(panelResult.ChatLogItems);
        }

        private void ChatMessageEvetRiser()
        {
            Task.Factory.StartNew(async () =>
            {
                if (_FFChatMessageArrived.HandlersCount == 0)
                    SpinWait.SpinUntil(() => _FFChatMessageArrived.HandlersCount > 0);

                while (_KeepWorking)
                {
                    try
                    {
                        FFChatMsg ffChatMsg = new FFChatMsg();
                        if (_FFxivChat.TryDequeue(out ffChatMsg))
                        {
                            var ea = new ChatMessageArrivedEventArgs(this)
                            {
                                ChatMessage = ffChatMsg
                            };

                            await _FFChatMessageArrived.InvokeAsync(ea);
                        }
                        else
                        {
                            SpinWait.SpinUntil(() => _FFxivChat.IsEmpty == false && _KeepWorking == true);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(e);
                    }
                }

            }, TaskCreationOptions.LongRunning);
        }

        private void ProcessChatMsg(ChatLogItem chatLogItem)
        {
            var tmpMsg = new FFChatMsg(chatLogItem.Line, chatLogItem.Code, chatLogItem.TimeStamp);
            _FFxivChat.Enqueue(tmpMsg);
        }

        private string PrepareTranslationText(string text)
        {
            text = text.Replace(":", ": ");

            return text;
        }

        private void EventErrorHandler(string evname, Exception ex)
        {
            string text = evname + Environment.NewLine + Convert.ToString(ex);
            Logger.WriteLog(text);
        }

        public void Dispose()
        {
            if (_FfXivProcess != null)
                _FfXivProcess.Dispose();
        }

        public void SendDebugMessages()
        {
            Task.Run(async () =>
            {
                await Task.Delay(2000);

                _FFxivChat.Enqueue(new FFChatMsg(@"_1_ Dakshina:Once you have finished the task, feel free to disssmount your marid.", "003D", DateTime.Now));
                _FFxivChat.Enqueue(new FFChatMsg(@"_2_ Dakshina:Once you have finished the task, feel free to disssmount your marid. It will make its own way back here.", "003D", DateTime.Now));
                _FFxivChat.Enqueue(new FFChatMsg(@"_3_ Inoshishi Bugyo:But...the boar will not be next year's totem animal... <sigh>", "", DateTime.Now));
                _FFxivChat.Enqueue(new FFChatMsg(@"_4_ Dakshina:Once you have finished the task, feel free to disssmount your marid. It will make its own way back here. Lydirlona:Mayhap you have heard that Rowena's House of Splendors is expanding its operations. I am proud to say that these rumors are true. Inoshishi Bugyo:But...the boar will not be next year's totem animal... <sigh>", "003D", DateTime.Now));
                _FFxivChat.Enqueue(new FFChatMsg(@"_5_ Dakshina:Once you have finished the task, feel free to disssmount your marid. It will make its own way back here.", "003D", DateTime.Now));
                _FFxivChat.Enqueue(new FFChatMsg(@"_6_ Lydirlona:Mayhap you have heard that Rowena's House of Splendors is expanding its operations. I am proud to say that these rumors are true.", "003D", DateTime.Now));
                _FFxivChat.Enqueue(new FFChatMsg(@"_7_ Inoshishi Bugyo:But...the boar will not be next year's totem animal... <sigh>", "003D", DateTime.Now));
                _FFxivChat.Enqueue(new FFChatMsg(@"_8_ Dakshina:Once you have finished the task, feel free to disssmount your marid. It will make its own way back here.", "", DateTime.Now));
                _FFxivChat.Enqueue(new FFChatMsg(@"_9_ Lydirlona:Mayhap you have heard that Rowena's House of Splendors is expanding its operations. I am proud to say that these rumors are true.", "003D", DateTime.Now));
                _FFxivChat.Enqueue(new FFChatMsg(@"_10_ Inoshishi Bugyo:But...the boar will not be next year's totem animal... <sigh>", "003D", DateTime.Now));//*/
                _FFxivChat.Enqueue(new FFChatMsg(@"Conrad:Ahhh, don't fret over that. You're not the first person to take up arms against the Empire under a false name. We'd do the same if we had any sense....My condolences for your loss, child.", "003D", DateTime.Now));
                _FFxivChat.Enqueue(new FFChatMsg(@"Conrad:My comrades and I must confer on your proposal. A moment, if you please...", "003D", DateTime.Now));
                _FFxivChat.Enqueue(new FFChatMsg(@"Conrad:Allow me to welcome you once more to Rhalgr's Reach, our humble headquarters.", "003D", DateTime.Now));
                _FFxivChat.Enqueue(new FFChatMsg(@"Conrad:My name is Conrad Kemp, and I have the dubious honor of overseeing operations here.", "003D", DateTime.Now));//*/
            });
        }
    }
}
