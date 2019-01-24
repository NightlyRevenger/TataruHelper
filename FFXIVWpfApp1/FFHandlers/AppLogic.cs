// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFXIITataruHelper.Translation;
using FFXIITataruHelper.WinUtils;
using Sharlayan;
using Sharlayan.Core;
using Sharlayan.Models;
using Sharlayan.Models.ReadResults;

namespace FFXIITataruHelper.FFHandlers
{
    public class AppLogic : IDisposable
    {
        public delegate void NewTranslationEventHandler(object sender, NewTranslationEventArgs e);

        public class NewTranslationEventArgs
        {
            public ReadOnlyCollection<TranslatedMsg> Sentence { get; private set; }

            public NewTranslationEventArgs(List<TranslatedMsg> s)
            {
                Sentence = new ReadOnlyCollection<TranslatedMsg>(s);
            }
        }

        public event NewTranslationEventHandler NewTransaltionEvent = delegate { };

        public delegate void FFXIVWindowEventHandler(object sender, System.Windows.WindowState e);

        public event FFXIVWindowEventHandler FFXIVWindowEvent = delegate { };

        public delegate void FFProcessEventHandler(object sender, FFProcessEventArgs e);

        public class FFProcessEventArgs
        {
            public string Text { get; private set; }
            public bool IsRunning { get; private set; }

            public FFProcessEventArgs(string s, bool isRunning)
            {
                Text = s;
                IsRunning = isRunning;
            }
        }

        public event FFProcessEventHandler FFXIVProcessEventEvent = delegate { };

        public WebTranslator.TranslationEngine TranslationEngine
        {
            get
            {
                return _Translator.CurrentTranslationEngine;
            }
            set
            {
                _Translator.CurrentTranslationEngine = value;
            }
        }

        public string FFXIVLanguague
        {
            get { return _Translator.SourceLanguage; }
            set { _Translator.SourceLanguage = value; }
        }

        public string TargetLanguage
        {
            get { return _Translator.TargetLanguage; }
            set { _Translator.TargetLanguage = value; }
        }

        public ReadOnlyCollection<TranslatorLanguague> CurrentLanguages
        {
            get { return _Translator.CurrentLanguages; }
        }

        private List<IntPtr> _ExclusionWindowHandlers;
        private Thread _AppLogicThread;

        private Process _FfXivProcess;
        private string _FfProcessName;

        private ConcurrentQueue<TranslatedMsg> _ChatToTranslate;

        private ConcurrentQueue<TranslatedMsg> _TranslatedChat;

        private Dictionary<string, MsgType> _ChatCodesTypes;

        private WebTranslator _Translator;

        private DateTime _LastTranslationTime;

        bool _KeepWorking;
        bool _KeepWorkingFFWin;

        int MaxTranslatedSentences;

        public AppLogic()
        {
            _AppLogicThread = new Thread(this.EntryPoint);

            _ChatToTranslate = new ConcurrentQueue<TranslatedMsg>();

            _TranslatedChat = new ConcurrentQueue<TranslatedMsg>();

            _ChatCodesTypes = new Dictionary<string, MsgType>();

            _Translator = new WebTranslator();

            _ExclusionWindowHandlers = new List<IntPtr>();

            TranslationEngine = 0;

            MaxTranslatedSentences = GlobalSettings.MaxTranslatedSentencesCount;

            _Translator.LoadLanguagues();

            _LastTranslationTime = DateTime.UtcNow;
        }

        public void Start()
        {
            _KeepWorking = true;
            _KeepWorkingFFWin = true;
            _AppLogicThread.Start();
        }

        public void Stop()
        {
            _KeepWorking = false;
            _KeepWorkingFFWin = false;
        }

        public void AddExclusionWindowHandler(IntPtr handler)
        {
            _ExclusionWindowHandlers.Add(handler);
        }

        private void EntryPoint()
        {
            try
            {
                Init();

                ChatTranslator();

                bool IsFirstTime = true;

                while (_KeepWorking)
                {
                    if (IsFirstTime)
                    {
                        IsFirstTime = false;

                        InitMemoryReader();

                        ChatReader();
                    }

                    if ((Process.GetProcessesByName(_FfProcessName)).Length == 0)
                    {
                        if (FFXIVProcessEventEvent != null)
                        {
                            string text = "";
                            FFXIVProcessEventEvent(this, new FFProcessEventArgs(text, false));
                        }

                        _KeepWorkingFFWin = false;
                        IsFirstTime = true;
                    }

                    Thread.Sleep(GlobalSettings.LookForPorcessDelay);
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        private void InitMemoryReader()
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
                        porcessNotFound = false;
                        // supported: English, Chinese, Japanese, French, German, Korean
                        string gameLanguage = "English";
                        // whether to always hit API on start to get the latest sigs based on patchVersion, or use the local json cache (if the file doesn't exist, API will be hit)
                        bool useLocalCache = true;
                        // patchVersion of game, or latest//
                        string patchVersion = "latest";
                        Process process = processes[0];
                        _FfXivProcess = process;
                        ProcessModel processModel = new ProcessModel
                        {
                            Process = process,
                            IsWin64 = true
                        };

                        MemoryHandler.Instance.SetProcess(processModel, gameLanguage, patchVersion, useLocalCache);

                        _KeepWorkingFFWin = true;

                        WatchFFWindowState();

                        if (FFXIVProcessEventEvent != null)
                        {
                            string text = processes[0].ProcessName;
                            FFXIVProcessEventEvent(this, new FFProcessEventArgs(text, true));
                        }
                    }
                    else
                    {
                        Thread.Sleep(GlobalSettings.LookForPorcessDelay);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        private void Init()
        {
            List<ChatMsgType> _ChatCodesTypesTmp = new List<ChatMsgType>(new ChatMsgType[] { new ChatMsgType("003D", MsgType.Translate),
                new ChatMsgType("0044", MsgType.Translate)});

            var tmpCodes = Helper.LoadJsonData<List<ChatMsgType>>(GlobalSettings.ChatCodesFilePath);

            bool coldeLoadFailed = true;
            if (tmpCodes != null)
            {
                if (tmpCodes.Count > 0)
                {
                    _ChatCodesTypesTmp = tmpCodes;
                    coldeLoadFailed = false;
                }
            }
            if (coldeLoadFailed)
                Helper.SaveJson(_ChatCodesTypesTmp, GlobalSettings.ChatCodesFilePath);

            for (int i = 0; i < _ChatCodesTypesTmp.Count; i++)
            {
                _ChatCodesTypes.TryAdd(_ChatCodesTypesTmp[i].ChatCode, _ChatCodesTypesTmp[i].MsgType);
            }
        }

        private void WatchFFWindowState()
        {
            Task.Run(async () =>
            {
                System.Windows.WindowState FFXIVPrevWindowState = System.Windows.WindowState.Minimized;

                while (_KeepWorking)
                {
                    try
                    {
                        bool IsExclusionWindow = false;

                        System.Windows.WindowState FFXIVWindowState = System.Windows.WindowState.Normal;
                        var fgWindow = Win32Interfaces.GetForegroundWindow();

                        if (_FfXivProcess.MainWindowHandle != fgWindow)
                            FFXIVWindowState = System.Windows.WindowState.Minimized;
                        else
                            FFXIVWindowState = System.Windows.WindowState.Normal;

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
                            if (FFXIVWindowEvent != null)
                            {
                                if (FFXIVWindowState != FFXIVPrevWindowState)
                                {
                                    if (FFXIVWindowState == System.Windows.WindowState.Minimized)
                                    {
                                        int t = 0;
                                        t++;
                                    }
                                    FFXIVPrevWindowState = FFXIVWindowState;
                                    FFXIVWindowEvent(this, FFXIVPrevWindowState);
                                }
                            }
                        }
                        Process[] processes = Process.GetProcessesByName(_FfProcessName);
                        if (processes.Length == 0)
                        {
                            if (FFXIVProcessEventEvent != null)
                            {
                                string text = @"null";
                                FFXIVProcessEventEvent(this, new FFProcessEventArgs(text, false));
                            }
                        }


                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(Convert.ToString(e));
                    }

                    await Task.Delay(GlobalSettings.MemoryReaderDelay);
                }
            });
        }

        private void ChatReader()
        {
            int _previousArrayIndex = 0;
            int _previousOffset = 0;

            Task.Run(async () =>
            {
                while (_KeepWorking && _KeepWorkingFFWin)
                {
                    try
                    {
                        ChatLogResult readResult = Reader.GetChatLog(_previousArrayIndex, _previousOffset);

                        var chatLogEntries = readResult.ChatLogItems;

                        _previousArrayIndex = readResult.PreviousArrayIndex;
                        _previousOffset = readResult.PreviousOffset;

                        if (readResult.ChatLogItems.Count > 0)
                        {
                            for (int i = 0; i < readResult.ChatLogItems.Count; i++)
                            {
                                MsgType msgType = MsgType.Translate;
                                if (_ChatCodesTypes.TryGetValue(readResult.ChatLogItems[i].Code, out msgType))
                                {
                                    ProcessChatMsg(readResult.ChatLogItems[i], msgType);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(Convert.ToString(e));
                    }

                    await Task.Delay(GlobalSettings.MemoryReaderDelay);
                }
            });
        }

        private void ProcessChatMsg(ChatLogItem chatLogItem, MsgType msgType)
        {
            switch (msgType)
            {
                case MsgType.Translate:
                    {
                        var tmpMsg = new TranslatedMsg(chatLogItem.Line, String.Empty);
                        _ChatToTranslate.Enqueue(tmpMsg);
                        break;
                    }
                default:
                    {
                        return;
                    }
            }
        }

        private void ChatTranslator()
        {
            Task.Run(async () =>
            {
                TranslatedMsg translatedMsg = new TranslatedMsg();
                while (_KeepWorking && _KeepWorkingFFWin)
                {
                    try
                    {
                        if (_ChatToTranslate.TryDequeue(out translatedMsg))
                        {
                            var diffTime = (int)Math.Round((DateTime.UtcNow - _LastTranslationTime).TotalMilliseconds);

                            if (diffTime < GlobalSettings.TranslationDelay)
                                await Task.Delay(GlobalSettings.TranslationDelay - diffTime);

                            string originalSentence = PrepareTranslationText(translatedMsg.OriginalText);

                            translatedMsg.TranslatedText = _Translator.Transalte(originalSentence);

                            _LastTranslationTime = DateTime.UtcNow;

                            _TranslatedChat.Enqueue(translatedMsg);

                            if (_TranslatedChat.Count >= MaxTranslatedSentences)
                            {
                                RiseNewTrnsationEvent();
                            }
                        }
                        else
                        {
                            RiseNewTrnsationEvent();
                            SpinWait.SpinUntil(() => _ChatToTranslate.IsEmpty == false && _KeepWorking == true);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLog(Convert.ToString(e));
                    }
                }

            });
        }

        private void RiseNewTrnsationEvent()
        {
            if (NewTransaltionEvent != null)
            {
                List<TranslatedMsg> _tmpTransaltedList = new List<TranslatedMsg>(_TranslatedChat.Count);
                TranslatedMsg _translatedMsg = new TranslatedMsg();

                while (_TranslatedChat.TryDequeue(out _translatedMsg))
                {
                    _tmpTransaltedList.Add(_translatedMsg);
                }

                NewTransaltionEvent(this, new NewTranslationEventArgs(_tmpTransaltedList));
            }
        }

        private string PrepareTranslationText(string text)
        {
            text = text.Replace(":", ": ");

            return text;
        }

        public void Dispose()
        {
            if (_FfXivProcess != null)
                _FfXivProcess.Dispose();
        }
    }
}
