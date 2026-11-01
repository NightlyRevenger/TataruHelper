using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FFXIVTataruHelper;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.Settings;

using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

using Translation;
using Translation.Models;

namespace TataruHelper.Tests
{
    /// <summary>
    /// The ways a buffered line can die: a flush task that faults must end the
    /// line in a failure rather than a silent forever, and a line cancelled
    /// while its batch is in flight must not pay for a translation nobody
    /// will read.
    /// </summary>
    [TestFixture]
    public class ChatProcessorFaultAndCancellationTests
    {
        private static readonly TranslationEngine Engine =
            new TranslationEngine(TranslationEngineName.OpenAI, new List<TranslatorLanguage>(), 1D);

        private static readonly TranslatorLanguage From =
            new TranslatorLanguage("English", "English", "en");
        private static readonly TranslatorLanguage To =
            new TranslatorLanguage("Russian", "Russian", "ru");

        private const string Delimiter = "<<<TATARU_TRANSLATION_SEGMENT>>>";

        [Test]
        public void FailPendingRequests_FailsEveryBufferedLine_AndClearsTheKey()
        {
            var states = new Dictionary<string, ChatProcessor.TranslationBufferState>(StringComparer.Ordinal);
            var state = new ChatProcessor.TranslationBufferState
            {
                DelayCts = new CancellationTokenSource() // no successor task: this task died
            };
            state.PendingRequests.Add(
                new ChatProcessor.BufferedTranslationRequest("still waiting", TranslationTextKind.Chat, CancellationToken.None));
            states["key"] = state;

            var failed = ChatProcessor.FailPendingRequests(states, "key", Engine,
                new InvalidOperationException("the flush died"));

            Assert.That(failed, Is.True);
            Assert.That(states.ContainsKey("key"), Is.False);
            var completion = state.PendingRequests[0].CompletionSource.Task;
            Assert.That(completion.IsCompleted, Is.True);
            Assert.That(completion.Result.IsSuccess, Is.False);
            Assert.That(completion.Result.FailureKind, Is.EqualTo(TranslationFailureKind.ProviderUnavailable));
        }

        [Test]
        public void FailPendingRequests_LeavesARescheduledStateAlone()
        {
            var states = new Dictionary<string, ChatProcessor.TranslationBufferState>(StringComparer.Ordinal);
            var state = new ChatProcessor.TranslationBufferState
            {
                DelayCts = null // the task rescheduled the remainder: a live task owns it
            };
            state.PendingRequests.Add(
                new ChatProcessor.BufferedTranslationRequest("somebody else's", TranslationTextKind.Chat, CancellationToken.None));
            states["key"] = state;

            var failed = ChatProcessor.FailPendingRequests(states, "key", Engine, new Exception());

            Assert.That(failed, Is.False);
            Assert.That(states.ContainsKey("key"), Is.True);
            Assert.That(state.PendingRequests[0].CompletionSource.Task.IsCompleted, Is.False);
        }

        [Test]
        public async Task ALineCancelledMidBatch_SkipsTheFallbackTranslation()
        {
            var ctsA = new CancellationTokenSource();
            var ctsB = new CancellationTokenSource();

            var translator = new ScriptedTranslator(call =>
            {
                if (call.Sentence.Contains(Delimiter))
                {
                    // The batch is in flight. Line B is being closed over
                    // exactly this moment.
                    ctsB.Cancel();

                    // No delimiter at all: the batch cannot be split, so the
                    // per-line fallback has to run.
                    return Task.FromResult(TranslationResult.Success(Engine.EngineName, "unsplitable"));
                }

                return Task.FromResult(TranslationResult.Success(
                    Engine.EngineName, "translated:" + call.Sentence));
            });

            var sut = NewProcessor(translator);

            var taskA = sut.Translate("line a", Engine, From, To, "0044", ctsA.Token);
            var taskB = sut.Translate("line b", Engine, From, To, "0044", ctsB.Token);

            var resultA = await taskA;

            Assert.That(resultA.IsSuccess, Is.True);
            Assert.That(resultA.Text, Is.EqualTo("translated:line a"));
            Assert.CatchAsync<OperationCanceledException>(() => taskB, "line B must end cancelled");

            var fallbackCalls = translator.NonBatchedCalls;

            Assert.That(fallbackCalls, Has.Count.EqualTo(1));
            Assert.That(fallbackCalls[0].Sentence, Is.EqualTo("line a"));
            Assert.That(fallbackCalls[0].Token, Is.EqualTo(ctsA.Token));
            Assert.That(ctsA.Token.IsCancellationRequested, Is.False);
        }

        private static ChatProcessor NewProcessor(WebTranslator translator)
        {
            return new ChatProcessor(translator, new FakeSettingsStore(), new NullAppLogger());
        }

        /// <summary>
        /// Stands in for the engine: every call is recorded, and the answer is
        /// scripted by the test.
        /// </summary>
        private sealed class ScriptedTranslator : WebTranslator
        {
            private readonly Func<Call, Task<TranslationResult>> _script;

            public ScriptedTranslator(Func<Call, Task<TranslationResult>> script)
                : base(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance)
            {
                _script = script;
            }

            public List<Call> NonBatchedCalls { get; } = new List<Call>();

            public override Task<TranslationResult> TranslateAsync(
                string inSentence,
                TranslationEngine translationEngine,
                TranslatorLanguage fromLang,
                TranslatorLanguage toLang,
                TranslationTextKind kind,
                CancellationToken cancellationToken)
            {
                var call = new Call(inSentence, cancellationToken);

                if (!inSentence.Contains(Delimiter))
                {
                    NonBatchedCalls.Add(call);
                }

                return _script(call);
            }

            public sealed class Call
            {
                public Call(string sentence, CancellationToken token)
                {
                    Sentence = sentence;
                    Token = token;
                }

                public string Sentence { get; }
                public CancellationToken Token { get; }
            }
        }

        private sealed class FakeSettingsStore : ISettingsStore
        {
            public AppSettings AppSettings { get; } = new AppSettings
            {
                TranslationContextBufferWindowMs = 1,
                TranslationContextMaxBatchSize = 4
            };

            public string ChatCodesFilePath => string.Empty;
            public string BlackListPath => string.Empty;
            public string IgnoreNickNameChatCodesPath => string.Empty;
            public string SystemSettingsPath => string.Empty;
            public string SettingsPath => string.Empty;
            public string OldSettingsPath => string.Empty;
            public int SettingsSaveDelayMs => 60_000;
            public int LookForProcessDelayMs => 1;
            public int MemoryReaderDelayMs => 1;
            public int AutoHideWatcherDelayMs => 1;
            public int TranslatorWaitTimeMs => 1;
            public int MaxTranslateTryCount => 1;
            public int MaxChatMessages => 500;

            public bool LoadGlobalSettings(string fileName)
            {
                return true;
            }

            public void SaveGlobalSettings(string fileName)
            {
            }
        }

        private sealed class NullAppLogger : IAppLogger
        {
            public void WriteLog(string input, string memberName = "", int sourceLineNumber = 0)
            {
            }

            public void WriteLog(object input, string memberName = "", int sourceLineNumber = 0)
            {
            }

            public void WriteConsoleLog(string input)
            {
            }

            public void WriteChatLog(string input)
            {
            }
        }
    }
}