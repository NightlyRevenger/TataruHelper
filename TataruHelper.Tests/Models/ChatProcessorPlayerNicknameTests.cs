using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using FFXIVTataruHelper;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.Settings;

using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

using Translation;
using Translation.Models;

namespace TataruHelper.Tests.Models
{
    [TestFixture]
    public class ChatProcessorPlayerNicknameTests
    {
        private static readonly TranslationEngine Engine =
            new TranslationEngine(TranslationEngineName.OpenAI, new List<TranslatorLanguage>(), 1D);

        private static readonly TranslatorLanguage From = new TranslatorLanguage("English", "English", "en");
        private static readonly TranslatorLanguage To = new TranslatorLanguage("Russian", "Russian", "ru");

        [Test]
        public async Task TranslatePlayerNicknames_TranslatesTheSenderAndKeepsItInTheResult()
        {
            using var files = new TestFiles();
            var translator = new EchoTranslator();
            var processor = new ChatProcessor(translator, files.Settings, new NullAppLogger())
            {
                TranslatePlayerNicknames = true
            };

            var result = await processor.Translate("Player Name\uFF1A hello", Engine, From, To, "001B");

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Text, Is.EqualTo("translated:Player Name\uFF1A translated: hello"));
                Assert.That(result.SpeakerName, Is.EqualTo("translated:Player Name\uFF1A"));
                Assert.That(translator.Requests, Is.EqualTo(new[] { " hello", "Player Name" }));
            });
        }

        private sealed class EchoTranslator : WebTranslator
        {
            public EchoTranslator() : base(NullLogger.Instance) { }

            public List<string> Requests { get; } = new List<string>();

            public override Task<TranslationResult> TranslateAsync(
                string inSentence,
                TranslationEngine translationEngine,
                TranslatorLanguage fromLang,
                TranslatorLanguage toLang,
                CancellationToken cancellationToken)
            {
                Requests.Add(inSentence);
                return Task.FromResult(TranslationResult.Success(translationEngine.EngineName, "translated:" + inSentence));
            }
        }

        private sealed class TestFiles : IDisposable
        {
            private readonly string _directory;

            public TestFiles()
            {
                _directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_directory);
                File.WriteAllText(Path.Combine(_directory, "blacklist.json"), "[]");
                File.WriteAllText(Path.Combine(_directory, "codes.json"), "[]");
                File.WriteAllText(Path.Combine(_directory, "nicknames.json"), "[\"001B\"]");
                Settings = new TestSettingsStore(_directory);
            }

            public TestSettingsStore Settings { get; }

            public void Dispose()
            {
                Directory.Delete(_directory, true);
            }
        }

        private sealed class TestSettingsStore : ISettingsStore
        {
            private readonly string _directory;

            public TestSettingsStore(string directory)
            {
                _directory = directory;
            }

            public AppSettings AppSettings { get; } = new AppSettings
            {
                TranslationContextBufferWindowMs = 0,
                TranslationContextMaxBatchSize = 1
            };

            public string ChatCodesFilePath => Path.Combine(_directory, "codes.json");
            public string BlackListPath => Path.Combine(_directory, "blacklist.json");
            public string IgnoreNickNameChatCodesPath => Path.Combine(_directory, "nicknames.json");
            public string SystemSettingsPath => string.Empty;
            public string SettingsPath => string.Empty;
            public string OldSettingsPath => string.Empty;
            public int SettingsSaveDelayMs => 1;
            public int LookForProcessDelayMs => 1;
            public int MemoryReaderDelayMs => 1;
            public int AutoHideWatcherDelayMs => 1;
            public int TranslatorWaitTimeMs => 1;
            public int MaxTranslateTryCount => 1;
            public int MaxChatMessages => 500;
            public bool LoadGlobalSettings(string fileName) => true;
            public void SaveGlobalSettings(string fileName) { }
        }

        private sealed class NullAppLogger : IAppLogger
        {
            public void WriteLog(string input, string memberName = "", int sourceLineNumber = 0) { }
            public void WriteLog(object input, string memberName = "", int sourceLineNumber = 0) { }
            public void WriteConsoleLog(string input) { }
            public void WriteChatLog(string input) { }
        }
    }
}
