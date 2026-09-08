using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using FFXIVTataruHelper.FFHandlers;
using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.Settings;

using NUnit.Framework;

using Sharlayan.Core;
using Sharlayan.Models;
using Sharlayan.Models.ReadResults;

namespace TataruHelper.Tests
{
    public class FFMemoryReaderTests
    {
        [Test]
        public void ProcessReadResult_DropsTheChatLogCopyOfALineItAlreadyReadLive()
        {
            var gateway = new FakeGameMemoryGateway
            {
                CodesReadLive = { "003D" },
                DirectDialogResult = BuildResult(
                    new ChatLogItem { Code = "003D", Line = "RealtimeNpc:RealtimeDialog" })
            };
            var reader = new FFMemoryReader(gateway, new NullLogger(), new FakeSettingsStore());

            InvokeProcessReadResult(
                reader,
                BuildResult(
                    new ChatLogItem { Code = "003D", Line = "LogNpc:DelayedDialog" },
                    new ChatLogItem { Code = "000A", Line = "Someone:Say something" }));

            var messages = ReadQueuedMessages(reader);

            // The chat log repeats every NPC line once the player clicks through it,
            // so that copy must not be shown a second time.
            Assert.That(messages.Any(message => message.Text == "LogNpc:DelayedDialog"), Is.False,
                "the delayed copy of a line already shown live");
            Assert.That(messages.Any(message => message.Code == "003D" && message.Text == "RealtimeNpc:RealtimeDialog"),
                Is.True);

            // Unrelated chat channels are untouched.
            Assert.That(messages.Any(message => message.Code == "000A" && message.Text == "Someone:Say something"),
                Is.True);
        }

        // The reason the drop is conditional. Speech bubbles and cutscene
        // subtitles are read out of different addons, and the subtitle's offset
        // is the one FFXIVClientStructs cannot supply - so it can be the only
        // one broken on a given client. Dropping the chat log's copy of a code
        // we have never once managed to read left cutscenes showing nothing at
        // all, which is how a player reported it.
        [Test]
        public void ProcessReadResult_KeepsTheChatLogCopyOfACodeItHasNeverReadLive()
        {
            var gateway = new FakeGameMemoryGateway
            {
                CodesReadLive = { "003D" },
                DirectDialogResult = BuildResult(
                    new ChatLogItem { Code = "003D", Line = "RealtimeNpc:RealtimeDialog" })
            };
            var reader = new FFMemoryReader(gateway, new NullLogger(), new FakeSettingsStore());

            InvokeProcessReadResult(
                reader,
                BuildResult(new ChatLogItem { Code = "0044", Line = "DelayedCutscene" }));

            var messages = ReadQueuedMessages(reader);

            Assert.That(messages.Any(message => message.Code == "0044" && message.Text == "DelayedCutscene"), Is.True,
                "late is better than never");
        }

        [Test]
        public void ProcessReadResult_WithRealtimeOff_UsesTheChatLogOnly()
        {
            var gateway = new FakeGameMemoryGateway
            {
                CodesReadLive = { "003D" },
                DirectDialogResult =
                    BuildResult(new ChatLogItem { Code = "003D", Line = "RealtimeNpc:RealtimeDialog" })
            };
            var reader = new FFMemoryReader(gateway, new NullLogger(), new FakeSettingsStore())
            {
                IsRealtimeTranslationEnabled = false
            };

            InvokeProcessReadResult(
                reader,
                BuildResult(new ChatLogItem { Code = "003D", Line = "LogNpc:DelayedDialog" }));

            var messages = ReadQueuedMessages(reader);

            Assert.That(gateway.GetDirectDialogCalls, Is.EqualTo(0));
            Assert.That(messages.Select(message => message.Text), Is.EquivalentTo(new[] { "LogNpc:DelayedDialog" }));
        }

        [Test]
        public void ProcessReadResult_AlwaysReadsRealtimeGateway()
        {
            var gateway = new FakeGameMemoryGateway
            {
                CodesReadLive = { "003D" },
                DirectDialogResult =
                    BuildResult(new ChatLogItem { Code = "003D", Line = "RealtimeNpc:RealtimeDialog" })
            };
            var reader = new FFMemoryReader(gateway, new NullLogger(), new FakeSettingsStore());

            InvokeProcessReadResult(
                reader,
                BuildResult(new ChatLogItem { Code = "003D", Line = "LogNpc:DelayedDialog" }));

            var messages = ReadQueuedMessages(reader);

            Assert.That(gateway.GetDirectDialogCalls, Is.EqualTo(1));
            Assert.That(messages.Select(message => message.Text),
                Is.EquivalentTo(new[] { "RealtimeNpc:RealtimeDialog" }));
        }

        // The overlay has to be told which line the game is drawing so it can
        // keep an arriving copy of an earlier line from being put over it.
        [Test]
        public void CurrentDialogueLine_ComesFromTheGateway()
        {
            var gateway = new FakeGameMemoryGateway { CurrentDialogueLine = "Cid:Quite." };
            var reader = new FFMemoryReader(gateway, new NullLogger(), new FakeSettingsStore());

            Assert.That(reader.CurrentDialogueLine, Is.EqualTo("Cid:Quite."));
        }

        [TestCase("003D", true)]
        [TestCase("0044", true)]
        [TestCase("000A", false)]
        [TestCase("0039", false)]
        [TestCase("F03D", false, Description = "retired: one code per channel now, whichever way the line arrives")]
        public void IsStoryDialogueCode_MatchesTheTwoDialogueCodes(string code, bool expected)
        {
            Assert.That(
                FFMemoryReader.IsStoryDialogueCode(new ChatLogItem { Code = code }),
                Is.EqualTo(expected));
        }

        /// <summary>
        /// The signatures behind duplicate suppression were kept for the whole
        /// session to answer a question that stops mattering after two
        /// seconds, so an evening in a busy zone put every line anybody said
        /// into a dictionary that could never use them again.
        /// </summary>
        [Test]
        public void ForgetStaleSignatures_KeepsWhatTheWindowStillCoversAndDropsTheRest()
        {
            var now = new DateTime(2026, 8, 27, 14, 0, 0, DateTimeKind.Utc);
            var window = TimeSpan.FromSeconds(2);

            var seen = new ConcurrentDictionary<string, DateTime>();
            seen["003D|just said"] = now.AddMilliseconds(-500);
            seen["003D|said a moment ago"] = now - window;
            seen["000A|said an hour ago"] = now.AddHours(-1);

            FFMemoryReader.ForgetStaleSignatures(seen, now, window);

            Assert.That(seen.ContainsKey("003D|just said"), Is.True, "still inside the window");
            Assert.That(seen.ContainsKey("003D|said a moment ago"), Is.True, "exactly at the edge is not past it");
            Assert.That(seen.ContainsKey("000A|said an hour ago"), Is.False);
        }

        [Test]
        public void ForgetStaleSignatures_SurvivesHavingNothingToSweep()
        {
            Assert.DoesNotThrow(() => FFMemoryReader.ForgetStaleSignatures(null, DateTime.UtcNow, TimeSpan.FromSeconds(2)));
            Assert.DoesNotThrow(() => FFMemoryReader.ForgetStaleSignatures(
                new ConcurrentDictionary<string, DateTime>(), DateTime.UtcNow, TimeSpan.FromSeconds(2)));
        }

        private static void InvokeProcessReadResult(FFMemoryReader reader, ChatLogResult result)
        {
            var method = typeof(FFMemoryReader).GetMethod(
                "ProcessReadResult",
                BindingFlags.Instance | BindingFlags.NonPublic);

            method.Invoke(reader, new object[] { result });
        }

        private static FFChatMsg[] ReadQueuedMessages(FFMemoryReader reader)
        {
            var field = typeof(FFMemoryReader).GetField(
                "_ffxivChat",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return ((ConcurrentQueue<FFChatMsg>)field.GetValue(reader)).ToArray();
        }

        private static ChatLogResult BuildResult(params ChatLogItem[] items)
        {
            var result = new ChatLogResult();
            foreach (var item in items)
            {
                result.ChatLogItems.Enqueue(item);
            }

            return result;
        }

        private sealed class FakeGameMemoryGateway : IGameMemoryGateway
        {
            public int GetDirectDialogCalls { get; private set; }
            public ChatLogResult DirectDialogResult { get; set; } = new ChatLogResult();

            public FFXIVTataruHelper.Services.GameMemory.AddonBounds DialogueBounds { get; set; } =
                FFXIVTataruHelper.Services.GameMemory.AddonBounds.Unknown;

            public DialogueSurface DialogueSurface { get; set; }

            public string CurrentDialogueLine { get; set; } = string.Empty;

            public string CurrentDialogueSpeaker { get; set; } = string.Empty;

            /// <summary>Codes this gateway claims to have read off the screen.</summary>
            public HashSet<string> CodesReadLive { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public bool HasReadCodeLive(string chatCode)
            {
                return chatCode != null && CodesReadLive.Contains(chatCode);
            }

            public LiveReadingStats LiveReading =>
                new LiveReadingStats(CodesReadLive.Count, CodesReadLive.ToArray());

            public void SetProcess(
                ProcessModel processModel,
                string gameLanguage,
                string patchVersion,
                bool useLocalCache,
                bool scanAllMemoryRegions)
            {
            }

            public void UnsetProcess()
            {
            }

            public ChatLogResult GetChatLog(int previousArrayIndex, int previousOffset)
            {
                return new ChatLogResult();
            }

            public ChatLogResult GetDirectDialog()
            {
                GetDirectDialogCalls++;
                return DirectDialogResult;
            }

            public string GetPlayerName() => string.Empty;



            public bool? GetPlayerIsFeminine() => null;


            public bool CheckChatEquality(ChatLogItem item1, ChatLogItem item2)
            {
                return false;
            }
        }

        private sealed class FakeSettingsStore : ISettingsStore
        {
            public FFXIVTataruHelper.AppSettings AppSettings { get; } = new FFXIVTataruHelper.AppSettings();

            public string ChatCodesFilePath => string.Empty;
            public string BlackListPath => string.Empty;
            public string IgnoreNickNameChatCodesPath => string.Empty;
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

            public bool LoadGlobalSettings(string fileName)
            {
                return true;
            }

            public void SaveGlobalSettings(string fileName)
            {
            }
        }

        private sealed class NullLogger : IAppLogger
        {
            public void WriteLog(string input, string memberName = "", int sourceLineNumber = 0) { }
            public void WriteLog(object input, string memberName = "", int sourceLineNumber = 0) { }
            public void WriteConsoleLog(string input) { }
            public void WriteChatLog(string input) { }
        }
    }
}