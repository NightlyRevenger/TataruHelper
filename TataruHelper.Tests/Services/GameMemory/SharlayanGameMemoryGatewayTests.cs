using System;
using System.Collections.Generic;
using System.Linq;

using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.Services.Logging;

using NUnit.Framework;

using Sharlayan.Core;
using Sharlayan.Models.ReadResults;

namespace TataruHelper.Tests
{
    public class SharlayanGameMemoryGatewayTests
    {
        [Test]
        public void Gateway_DelegatesDirectDialogAndEqualityToReader()
        {
            var directDialogReader = new FakeDirectDialogReader();
            var gateway = CreateGateway(directDialogReader, () => TalkAddonRealtimeDialogSnapshot.Unavailable());

            var dialog = gateway.GetDirectDialog();
            var equal = gateway.CheckChatEquality(new ChatLogItem(), new ChatLogItem());

            Assert.That(directDialogReader.ExtractCalls, Is.EqualTo(1));
            Assert.That(directDialogReader.EqualityCalls, Is.EqualTo(1));
            Assert.That(dialog, Is.SameAs(directDialogReader.DirectDialogResult));
            Assert.That(equal, Is.True);
        }

        [Test]
        public void Gateway_PrioritizesRealtime003D_AndKeepsOnlyFallback0044()
        {
            var directDialogReader = new FakeDirectDialogReader
            {
                DirectDialogResult = BuildResult(
                    new ChatLogItem { Code = "003D", Line = "OldNpc:FromChatLog" },
                    new ChatLogItem { Code = "0044", Line = "CutsceneNpc:FromChatLog" })
            };

            var gateway = CreateGateway(
                directDialogReader,
                () => TalkAddonRealtimeDialogSnapshot.Available("003D", string.Empty, "LiveText"));

            var result = gateway.GetDirectDialog();
            var items = result.ChatLogItems.ToArray();

            Assert.That(items.Length, Is.EqualTo(2));
            Assert.That(items.Count(item => item.Code == "003D"), Is.EqualTo(1));
            Assert.That(items.Any(item => item.Code == "003D" && item.Line == "LiveText"), Is.True,
                "the line read off the screen keeps the channel's own code");
            Assert.That(items.Any(item => item.Code == "0044" && item.Line == "CutsceneNpc:FromChatLog"), Is.True);
            Assert.That(items.Any(item => item.Code == "003D" && item.Line == "OldNpc:FromChatLog"), Is.False);
        }

        // What lets the reader tell a channel it can read off the screen from one
        // it cannot, and so whether the chat log's copy is a repeat or the only
        // copy there will be.
        [Test]
        public void Gateway_ReportsWhichCodesItHasManagedToReadLive()
        {
            var gateway = CreateGateway(
                new FakeDirectDialogReader(),
                () => TalkAddonRealtimeDialogSnapshot.Available("003D", string.Empty, "LiveText"));

            Assert.That(gateway.HasReadCodeLive("003D"), Is.False, "nothing read yet");

            gateway.GetDirectDialog();

            Assert.Multiple(() =>
            {
                Assert.That(gateway.HasReadCodeLive("003D"), Is.True);
                Assert.That(gateway.HasReadCodeLive("0044"), Is.False, "no subtitle has been read");
            });
        }

        [Test]
        public void Gateway_FallsBackToHeuristicDirectDialog_WhenRealtimeUnavailable()
        {
            var directDialogReader = new FakeDirectDialogReader
            {
                DirectDialogResult = BuildResult(
                    new ChatLogItem { Code = "003D", Line = "FallbackNpc:FallbackText" },
                    new ChatLogItem { Code = "0044", Line = "FallbackCutscene:FallbackText" })
            };

            var gateway = CreateGateway(
                directDialogReader,
                () => TalkAddonRealtimeDialogSnapshot.Unavailable());

            var result = gateway.GetDirectDialog();
            var items = result.ChatLogItems.ToArray();

            Assert.That(items.Length, Is.EqualTo(2));
            Assert.That(items.Any(item => item.Code == "003D" && item.Line == "FallbackNpc:FallbackText"), Is.True);
            Assert.That(items.Any(item => item.Code == "0044" && item.Line == "FallbackCutscene:FallbackText"),
                Is.True);
        }

        [Test]
        public void Gateway_DoesNotEmitRealtime003DDuplicatesAcrossTicks()
        {
            var directDialogReader = new FakeDirectDialogReader
            {
                DirectDialogResult = BuildResult(new ChatLogItem { Code = "003D", Line = "ChatlogNpc:ChatlogText" })
            };

            var queue = new Queue<TalkAddonRealtimeDialogSnapshot>();
            queue.Enqueue(TalkAddonRealtimeDialogSnapshot.Available("LiveText"));
            queue.Enqueue(TalkAddonRealtimeDialogSnapshot.Available("LiveText"));

            var gateway = CreateGateway(directDialogReader, () => queue.Dequeue());

            var firstTick = gateway.GetDirectDialog().ChatLogItems.ToArray();
            var secondTick = gateway.GetDirectDialog().ChatLogItems.ToArray();

            Assert.That(firstTick.Length, Is.EqualTo(1));
            Assert.That(firstTick[0].Line, Is.EqualTo("LiveText"));
            Assert.That(secondTick.Length, Is.EqualTo(0));
        }

        [Test]
        public void Gateway_EmitsRealtimeSpeakerPrefix_WhenSpeakerAvailable()
        {
            var directDialogReader = new FakeDirectDialogReader();
            var gateway = CreateGateway(
                directDialogReader,
                () => TalkAddonRealtimeDialogSnapshot.Available("003D", "LiveNpc", "LiveText"));

            var item = gateway.GetDirectDialog().ChatLogItems.Single();

            Assert.That(item.Code, Is.EqualTo("003D"));
            Assert.That(item.Line, Is.EqualTo("LiveNpc:LiveText"));
        }

        [Test]
        public void Gateway_EmitsTheCutsceneCode_WhenSnapshotIsASubtitle()
        {
            var directDialogReader = new FakeDirectDialogReader();
            var gateway = CreateGateway(
                directDialogReader,
                () => TalkAddonRealtimeDialogSnapshot.Available("0044", "CutsceneNpc", "LiveText"));

            var item = gateway.GetDirectDialog().ChatLogItems.Single();

            Assert.That(item.Code, Is.EqualTo("0044"));
            Assert.That(item.Line, Is.EqualTo("CutsceneNpc:LiveText"));
        }

        // A cutscene can put the same words in the dialogue box and in the
        // subtitle at once. With the chat code deciding what counted as a new
        // line, those arrived one after the other in the window, in the two
        // different colours the codes are drawn in - the same sentence twice.
        [Test]
        public void Gateway_SuppressesTheSameLineShownByTwoAddonsAtOnce()
        {
            var directDialogReader = new FakeDirectDialogReader();
            var queue = new Queue<TalkAddonRealtimeDialogSnapshot>();
            queue.Enqueue(TalkAddonRealtimeDialogSnapshot.Available("003D", string.Empty, "SameText"));
            queue.Enqueue(TalkAddonRealtimeDialogSnapshot.Available("0044", string.Empty, "SameText"));

            var gateway = CreateGateway(directDialogReader, () => queue.Dequeue());

            var firstTick = gateway.GetDirectDialog().ChatLogItems.ToArray();
            var secondTick = gateway.GetDirectDialog().ChatLogItems.ToArray();

            Assert.That(firstTick.Length, Is.EqualTo(1));
            Assert.That(firstTick[0].Code, Is.EqualTo("003D"));
            Assert.That(secondTick, Is.Empty);
        }

        // Cutscene narration reaches the chat log under 0039, not the codes
        // dialogue usually carries. Requiring one of those meant every line of
        // it was shown twice - once read off the screen, once from the log.
        [Test]
        public void Gateway_DropsTheChatLogCopy_WhateverCodeItArrivesUnder()
        {
            var narration = "...The crackling warmth of Alphinaud's campfire.";
            var directDialogReader = new FakeDirectDialogReader();
            var gateway = CreateGateway(
                directDialogReader,
                () => TalkAddonRealtimeDialogSnapshot.Available("003D", string.Empty, narration));

            Assert.That(gateway.GetDirectDialog().ChatLogItems.Single().Line, Is.EqualTo(narration));

            var fromChatLog = BuildResult(new ChatLogItem { Code = "0039", Line = narration });
            gateway.DropLinesAlreadySeenLive(fromChatLog);

            Assert.That(fromChatLog.ChatLogItems, Is.Empty);
        }

        [Test]
        public void Gateway_KeepsAChatLogLineNobodySaidOnScreen()
        {
            var directDialogReader = new FakeDirectDialogReader();
            var gateway = CreateGateway(
                directDialogReader,
                () => TalkAddonRealtimeDialogSnapshot.Available("003D", string.Empty, "Something said aloud"));

            gateway.GetDirectDialog();

            var fromChatLog = BuildResult(new ChatLogItem { Code = "0039", Line = "Something nobody said aloud" });
            gateway.DropLinesAlreadySeenLive(fromChatLog);

            Assert.That(fromChatLog.ChatLogItems, Has.Count.EqualTo(1));
        }

        // Two characters can say the same short thing - "Understood." - and
        // both deserve to be shown.
        [Test]
        public void Gateway_ReportsTheSameWordsFromADifferentSpeaker()
        {
            var directDialogReader = new FakeDirectDialogReader();
            var queue = new Queue<TalkAddonRealtimeDialogSnapshot>();
            queue.Enqueue(TalkAddonRealtimeDialogSnapshot.Available("003D", "Cid", "Understood."));
            queue.Enqueue(TalkAddonRealtimeDialogSnapshot.Available("003D", "Yda", "Understood."));

            var gateway = CreateGateway(directDialogReader, () => queue.Dequeue());

            Assert.That(gateway.GetDirectDialog().ChatLogItems.Single().Line, Is.EqualTo("Cid:Understood."));
            Assert.That(gateway.GetDirectDialog().ChatLogItems.Single().Line, Is.EqualTo("Yda:Understood."));
        }

        [Test]
        public void Gateway_FallsBackToHeuristicDirectDialog_WhenRealtimeAvailableButEmpty()
        {
            var directDialogReader = new FakeDirectDialogReader
            {
                DirectDialogResult = BuildResult(
                    new ChatLogItem { Code = "003D", Line = "FallbackNpc:FallbackText" },
                    new ChatLogItem { Code = "0044", Line = "FallbackCutsceneText" })
            };

            var gateway = CreateGateway(
                directDialogReader,
                () => TalkAddonRealtimeDialogSnapshot.Available("0044", "CutsceneNpc", "   "));

            var items = gateway.GetDirectDialog().ChatLogItems.ToArray();

            Assert.That(items.Length, Is.EqualTo(2));
            Assert.That(items.Any(item => item.Code == "003D" && item.Line == "FallbackNpc:FallbackText"), Is.True);
            Assert.That(items.Any(item => item.Code == "0044" && item.Line == "FallbackCutsceneText"), Is.True);
        }

        [Test]
        public void SelectRealtimeSnapshot_PrioritizesAddonTextOverLastTalkText()
        {
            var snapshot = TalkAddonRealtimeReader.SelectRealtimeSnapshot(
                "DelayedNpc",
                "DelayedText",
                new[] { TalkAddonRealtimeDialogSnapshot.Available("003D", string.Empty, "RealtimeAddonText") });

            Assert.That(snapshot.ChatCode, Is.EqualTo("003D"));
            Assert.That(snapshot.SpeakerName, Is.Empty);
            Assert.That(snapshot.TalkText, Is.EqualTo("RealtimeAddonText"));
        }

        [Test]
        public void SelectRealtimeSnapshot_DoesNotUseLastTalkNameWithDifferentMiniTalkAddonText()
        {
            var snapshot = TalkAddonRealtimeReader.SelectRealtimeSnapshot(
                "CutsceneNpc",
                "DelayedText",
                new[] { TalkAddonRealtimeDialogSnapshot.Available("0044", string.Empty, "RealtimeBubbleText") });

            Assert.That(snapshot.ChatCode, Is.EqualTo("0044"));
            Assert.That(snapshot.SpeakerName, Is.Empty);
            Assert.That(snapshot.TalkText, Is.EqualTo("RealtimeBubbleText"));
        }

        [Test]
        public void SelectRealtimeSnapshot_UsesLastTalkName_WhenLastTalkTextMatchesAddonText()
        {
            var snapshot = TalkAddonRealtimeReader.SelectRealtimeSnapshot(
                "CutsceneNpc",
                "RealtimeBubbleText",
                new[] { TalkAddonRealtimeDialogSnapshot.Available("0044", string.Empty, "RealtimeBubbleText") });

            Assert.That(snapshot.ChatCode, Is.EqualTo("0044"));
            Assert.That(snapshot.SpeakerName, Is.EqualTo("CutsceneNpc"));
            Assert.That(snapshot.TalkText, Is.EqualTo("RealtimeBubbleText"));
        }

        /// <summary>
        /// The game keeps its LastTalk pair long after the conversation is
        /// over, so it says nothing about what is on screen now. A blank
        /// window is reported blank.
        /// </summary>
        [Test]
        public void SelectRealtimeSnapshot_DoesNotAnswerFromLastTalk_WhenAddonTextIsEmpty()
        {
            var snapshot = TalkAddonRealtimeReader.SelectRealtimeSnapshot(
                "FallbackNpc",
                "FallbackLastTalkText",
                new[] { TalkAddonRealtimeDialogSnapshot.Available("0044", string.Empty, "   ") });

            Assert.That(snapshot.SourceAvailable, Is.True, "the window is loaded");
            Assert.That(SharlayanGameMemoryGateway.NormalizeDialogToken(snapshot.TalkText), Is.Empty);
        }

        /// <summary>
        /// And with no window loaded at all there is nothing to report, rather
        /// than the last thing anybody said.
        /// </summary>
        [Test]
        public void SelectRealtimeSnapshot_DoesNotAnswerFromLastTalk_WhenNoAddonIsLoaded()
        {
            var snapshot = TalkAddonRealtimeReader.SelectRealtimeSnapshot(
                "FallbackNpc",
                "FallbackLastTalkText",
                Array.Empty<TalkAddonRealtimeDialogSnapshot>());

            Assert.That(snapshot.SourceAvailable, Is.False);
        }

        [Test]
        public void BuildAddonSnapshot_SplitsVisibleTalkSpeakerAndBody()
        {
            var snapshot = TalkAddonRealtimeReader.BuildAddonSnapshot(
                "003D",
                new[] { "VisibleNpc", "Visible dialog text" },
                "StaleNpc",
                "Stale dialog text",
                true);

            Assert.That(snapshot.ChatCode, Is.EqualTo("003D"));
            Assert.That(snapshot.SpeakerName, Is.EqualTo("VisibleNpc"));
            Assert.That(snapshot.TalkText, Is.EqualTo("Visible dialog text"));
        }

        [Test]
        public void SelectBestTalkText_ReturnsLongestNonEmptyCandidate()
        {
            var result = SharlayanGameMemoryGateway.SelectBestTalkText(new[] { "  ", "short", "the longest line" });
            Assert.That(result, Is.EqualTo("the longest line"));
        }

        [Test]
        public void SelectBestTalkText_ReturnsEmpty_WhenOnlyWhitespaceProvided()
        {
            var result = SharlayanGameMemoryGateway.SelectBestTalkText(new[] { " ", "\t", string.Empty });
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void BuildRealtimeSignature_TrimsInput()
        {
            var signature = SharlayanGameMemoryGateway.BuildRealtimeSignature("  Npc:Line  ");
            Assert.That(signature, Is.EqualTo("Npc:Line"));
        }

        // What tells one utterance from another is who said it and what they
        // said - not which addon put it on screen.
        [Test]
        public void BuildRealtimeSignature_IsSpeakerAndText()
        {
            Assert.That(SharlayanGameMemoryGateway.BuildRealtimeSignature(" Npc ", " Line "),
                Is.EqualTo("Npc|Line"));
        }

        [Test]
        public void BuildRealtimeDialogLine_ReturnsTrimmedTalkText()
        {
            var line = SharlayanGameMemoryGateway.BuildRealtimeDialogLine(
                "  Hello there  ");

            Assert.That(line, Is.EqualTo("Hello there"));
        }

        [Test]
        public void BuildRealtimeDialogLine_ReturnsTalkText_WhenAlreadyNormalized()
        {
            var line = SharlayanGameMemoryGateway.BuildRealtimeDialogLine(
                "Hello there");

            Assert.That(line, Is.EqualTo("Hello there"));
        }

        [Test]
        public void BuildRealtimeDialogLine_ReturnsEmpty_WhenTalkTextIsWhitespace()
        {
            var line = SharlayanGameMemoryGateway.BuildRealtimeDialogLine(
                "   ");

            Assert.That(line, Is.EqualTo(string.Empty));
        }

        [Test]
        public void BuildRealtimeDialogLine_AddsSpeakerPrefix_WhenSpeakerProvided()
        {
            var line = SharlayanGameMemoryGateway.BuildRealtimeDialogLine(
                " LiveNpc ",
                " LiveText ");

            Assert.That(line, Is.EqualTo("LiveNpc:LiveText"));
        }

        // The first line after attaching used to be swallowed, because the Talk
        // addon holds what was said before the app started and announcing that
        // read as a conversation happening now. The reader skips addons the game
        // is not drawing, so a line that gets this far is one on screen - and
        // holding it back only meant walking up to an NPC right after launch and
        // getting nothing.
        [Test]
        public void Gateway_EmitsTheFirstRealtimeSnapshot_AfterAttaching()
        {
            var directDialogReader = new FakeDirectDialogReader();
            var snapshot = TalkAddonRealtimeDialogSnapshot.Available("003D", "Npc", "The first thing anyone says");
            var gateway = CreateGateway(directDialogReader, () => snapshot);

            gateway.ResetRealtimeDialogState();

            var item = gateway.GetDirectDialog().ChatLogItems.Single();

            Assert.That(item.Line, Is.EqualTo("Npc:The first thing anyone says"));
        }

        [Test]
        public void Gateway_EmitsRealtimeSnapshot_WhenTheLineChanges()
        {
            var directDialogReader = new FakeDirectDialogReader();
            var current = TalkAddonRealtimeDialogSnapshot.Available("003D", "OldNpc", "First line");
            var gateway = CreateGateway(directDialogReader, () => current);

            gateway.ResetRealtimeDialogState();
            gateway.GetDirectDialog();

            current = TalkAddonRealtimeDialogSnapshot.Available("003D", "NewNpc", "Fresh line");

            var item = gateway.GetDirectDialog().ChatLogItems.Single();

            Assert.That(item.Line, Is.EqualTo("NewNpc:Fresh line"));
        }

        // Nothing on screen must clear what was last said, or the same words
        // said again - an NPC repeating a bubble as you walk past - match the
        // signature still held and are taken for an echo.
        [Test]
        public void Gateway_EmitsTheSameLineAgain_AfterTheScreenClears()
        {
            var directDialogReader = new FakeDirectDialogReader();
            var current = TalkAddonRealtimeDialogSnapshot.Available("0044", string.Empty, "The wood... It's watching!");
            var now = new DateTime(2026, 5, 16, 10, 0, 0);
            var gateway = CreateGateway(directDialogReader, () => current, () => now);

            gateway.ResetRealtimeDialogState();
            Assert.That(gateway.GetDirectDialog().ChatLogItems.Single().Line,
                Is.EqualTo("The wood... It's watching!"));

            current = TalkAddonRealtimeDialogSnapshot.Unavailable();
            gateway.GetDirectDialog();

            current = TalkAddonRealtimeDialogSnapshot.Available("0044", string.Empty, "The wood... It's watching!");

            // Walked past again, not the same breath. Inside it the two are
            // indistinguishable from one line arriving by both roads, which is
            // what the guard is there to collapse.
            now = now.Add(RecentUtterance.SameBreath);

            Assert.That(gateway.GetDirectDialog().ChatLogItems.Single().Line,
                Is.EqualTo("The wood... It's watching!"));
        }

        // The line the game is drawing is the only judge of which conversation a
        // translated copy of an earlier line still belongs to - but it can only
        // be judged on the sweep that reads it, which is what costs nothing.
        [Test]
        public void Gateway_TellsTheLineItIsDrawing_OnEverySweep()
        {
            var directDialogReader = new FakeDirectDialogReader();
            var current = TalkAddonRealtimeDialogSnapshot.Available("003D", "Cid", "First line");
            var gateway = CreateGateway(directDialogReader, () => current);

            Assert.That(gateway.CurrentDialogueLine, Is.Empty, "nothing read yet, nothing drawn");

            gateway.GetDirectDialog();

            var firstReading = gateway.CurrentDialogueLine;
            gateway.GetDirectDialog();

            Assert.Multiple(() =>
            {
                Assert.That(
                    firstReading,
                    Is.EqualTo("Cid:First line"),
                    "the line it is drawing, in the form it reaches the pipeline");
                Assert.That(
                    gateway.CurrentDialogueLine,
                    Is.EqualTo(firstReading),
                    "the same line, on the next sweep");
            });
        }

        [Test]
        public void Gateway_ClearsTheLineItIsDrawing_WhenTheScreenGoesQuiet()
        {
            var current = TalkAddonRealtimeDialogSnapshot.Available("003D", "Cid", "First line");
            var gateway = CreateGateway(new FakeDirectDialogReader(), () => current);

            gateway.GetDirectDialog();
            Assert.That(gateway.CurrentDialogueLine, Is.EqualTo("Cid:First line"));

            current = TalkAddonRealtimeDialogSnapshot.Unavailable();
            gateway.GetDirectDialog();

            Assert.That(
                gateway.CurrentDialogueLine,
                Is.Empty,
                "holding on to it would make a copy still on its way look current");
        }

        [Test]
        public void Gateway_ClearsTheLineItIsDrawing_WhenTheAddonSaysNothing()
        {
            var current = TalkAddonRealtimeDialogSnapshot.Available("003D", "Cid", "First line");
            var gateway = CreateGateway(new FakeDirectDialogReader(), () => current);

            gateway.GetDirectDialog();

            current = TalkAddonRealtimeDialogSnapshot.Available("003D", "Cid", "   ");
            gateway.GetDirectDialog();

            Assert.That(
                gateway.CurrentDialogueLine,
                Is.Empty,
                "an addon speaking no words is drawing no line");
        }

        [Test]
        public void Gateway_ForgettingThePreviousGame_ForgetsWhatItWasDrawing()
        {
            var gateway = CreateGateway(
                new FakeDirectDialogReader(),
                () => TalkAddonRealtimeDialogSnapshot.Available("003D", "Cid", "First line"));

            gateway.GetDirectDialog();
            gateway.ResetRealtimeDialogState();

            Assert.That(
                gateway.CurrentDialogueLine,
                Is.Empty,
                "the previous process's words are not drawn by this one");
        }

        private static SharlayanGameMemoryGateway CreateGateway(
            FakeDirectDialogReader directDialogReader,
            Func<TalkAddonRealtimeDialogSnapshot> realtimeReader)
        {
            return new SharlayanGameMemoryGateway(
                directDialogReader,
                new NullLogger(),
                realtimeReader,
                () => new DateTime(2026, 5, 16, 10, 0, 0));
        }

        /// <summary>
        /// Replayed from a duty on 24 August: the subtitle strip showed the
        /// line naming nobody, and a second later the dialogue window showed
        /// the same line with Cassard's name on it. Both went out, so the
        /// reader said everything twice for the length of the dungeon.
        /// </summary>
        [Test]
        public void Gateway_ShowsOneLineOnce_WhenTwoWindowsCarryIt()
        {
            const string said = "I haven't the faintest what's going on, but you'd best keep moving!";

            var directDialogReader = new FakeDirectDialogReader();
            var queue = new Queue<TalkAddonRealtimeDialogSnapshot>();
            queue.Enqueue(TalkAddonRealtimeDialogSnapshot.Available("0044", string.Empty, said));
            queue.Enqueue(TalkAddonRealtimeDialogSnapshot.Available("0044", "Cassard", said));

            var now = new DateTime(2026, 8, 24, 15, 31, 38, DateTimeKind.Utc);
            var gateway = CreateGateway(directDialogReader, () => queue.Dequeue(), () => now);

            Assert.That(gateway.GetDirectDialog().ChatLogItems.Single().Line, Is.EqualTo(said));

            now = now.AddSeconds(1);

            Assert.That(gateway.GetDirectDialog().ChatLogItems, Is.Empty,
                "the dialogue window's copy of what the subtitle already said");
        }

        /// <summary>
        /// The same words half a minute on are the line being said again, not
        /// the other window catching up.
        /// </summary>
        [Test]
        public void Gateway_ShowsTheLineAgain_WhenItIsSaidLater()
        {
            const string said = "I haven't the faintest what's going on, but you'd best keep moving!";

            var directDialogReader = new FakeDirectDialogReader();
            var queue = new Queue<TalkAddonRealtimeDialogSnapshot>();
            queue.Enqueue(TalkAddonRealtimeDialogSnapshot.Available("0044", string.Empty, said));
            queue.Enqueue(TalkAddonRealtimeDialogSnapshot.Available("0044", "Cassard", said));

            var now = new DateTime(2026, 8, 24, 15, 31, 38, DateTimeKind.Utc);
            var gateway = CreateGateway(directDialogReader, () => queue.Dequeue(), () => now);

            gateway.GetDirectDialog();
            now = now.AddSeconds(34);

            Assert.That(gateway.GetDirectDialog().ChatLogItems.Single().Line, Is.EqualTo("Cassard:" + said));
        }

        /// <summary>
        /// Replayed from the Drowning Wench on 8 September: the barkeep is
        /// spoken to, greets you, is spoken to again and greets you the same
        /// way. Between the two the dialogue window is gone, and the greeting
        /// is a line of its own both times.
        /// </summary>
        [Test]
        public void Gateway_GreetsAgain_WhenTheWindowClosedInBetween()
        {
            const string greeting = "Welcome to the Drowning Wench, a place to leave your cares and troubles behind.";

            var directDialogReader = new FakeDirectDialogReader();
            var current = TalkAddonRealtimeDialogSnapshot.Available("003D", "I'tolwann", greeting);
            var now = new DateTime(2026, 9, 8, 20, 30, 34, DateTimeKind.Utc);
            var gateway = CreateGateway(directDialogReader, () => current, () => now);

            Assert.That(gateway.GetDirectDialog().ChatLogItems.Single().Line,
                Is.EqualTo("I'tolwann:" + greeting));

            // The conversation ends: no window is loaded to read.
            now = now.AddSeconds(3);
            current = TalkAddonRealtimeDialogSnapshot.Unavailable();
            gateway.GetDirectDialog();

            now = now.AddSeconds(3);
            current = TalkAddonRealtimeDialogSnapshot.Available("003D", "I'tolwann", greeting);

            Assert.That(gateway.GetDirectDialog().ChatLogItems.Single().Line,
                Is.EqualTo("I'tolwann:" + greeting),
                "the same greeting a second time is the barkeep greeting you a second time");
        }

        /// <summary>
        /// The same again where the window stays loaded and merely goes blank,
        /// which is what a cutscene's subtitle slots do. A window that is
        /// there but showing nothing says the conversation is over just as
        /// plainly as no window at all.
        /// </summary>
        [Test]
        public void Gateway_GreetsAgain_WhenTheWindowWentBlankInBetween()
        {
            const string greeting = "Welcome to the Drowning Wench, a place to leave your cares and troubles behind.";

            var directDialogReader = new FakeDirectDialogReader();
            var current = TalkAddonRealtimeDialogSnapshot.Available("003D", "I'tolwann", greeting);
            var now = new DateTime(2026, 9, 8, 20, 30, 34, DateTimeKind.Utc);
            var gateway = CreateGateway(directDialogReader, () => current, () => now);

            Assert.That(gateway.GetDirectDialog().ChatLogItems, Has.Count.EqualTo(1));

            now = now.AddSeconds(3);
            current = TalkAddonRealtimeDialogSnapshot.Available("003D", string.Empty, string.Empty);
            gateway.GetDirectDialog();

            now = now.AddSeconds(3);
            current = TalkAddonRealtimeDialogSnapshot.Available("003D", "I'tolwann", greeting);

            Assert.That(gateway.GetDirectDialog().ChatLogItems.Single().Line,
                Is.EqualTo("I'tolwann:" + greeting));
        }

        /// <summary>
        /// The window blinking for a sweep in the middle of one line is not
        /// the line being said again: the breath the two arrival roads share
        /// covers it.
        /// </summary>
        [Test]
        public void Gateway_SaysNothingTwice_WhenTheWindowBlinksMidLine()
        {
            const string said = "Time is of the essence, so I shall speak plain.";

            var directDialogReader = new FakeDirectDialogReader();
            var current = TalkAddonRealtimeDialogSnapshot.Available("003D", "Mother Miounne", said);
            var now = new DateTime(2026, 9, 8, 20, 29, 14, DateTimeKind.Utc);
            var gateway = CreateGateway(directDialogReader, () => current, () => now);

            Assert.That(gateway.GetDirectDialog().ChatLogItems, Has.Count.EqualTo(1));

            now = now.AddMilliseconds(40);
            current = TalkAddonRealtimeDialogSnapshot.Unavailable();
            gateway.GetDirectDialog();

            now = now.AddMilliseconds(40);
            current = TalkAddonRealtimeDialogSnapshot.Available("003D", "Mother Miounne", said);

            Assert.That(gateway.GetDirectDialog().ChatLogItems, Is.Empty);
        }

        /// <summary>
        /// Replayed from a duty on 24 August, to the millisecond:
        ///
        ///   16:29:05.119  the chat log's copy is judged - nothing read live yet
        ///   16:29:05.160  the screen's copy is read, forty-one milliseconds on
        ///
        /// The guard only asked whether the screen had spoken first, so with
        /// the log winning the race both copies were shown - which is every
        /// line of a duty, since there the log always wins.
        /// </summary>
        [Test]
        public void Gateway_ShowsOneLineOnce_WhenTheChatLogArrivesFirst()
        {
            const string said = "No small coup to roll that boulder over!";

            var directDialogReader = new FakeDirectDialogReader();
            var now = new DateTime(2026, 8, 24, 16, 29, 5, 119, DateTimeKind.Utc);
            var gateway = CreateGateway(
                directDialogReader,
                () => TalkAddonRealtimeDialogSnapshot.Available("0044", string.Empty, said),
                () => now);

            var fromChatLog = BuildResult(new ChatLogItem { Code = "0044", Line = "Thancred's Avatar:" + said });
            gateway.DropLinesAlreadySeenLive(fromChatLog);

            Assert.That(fromChatLog.ChatLogItems, Has.Count.EqualTo(1),
                "the log got here first, so its copy is the one shown");

            now = now.AddMilliseconds(41);

            Assert.That(gateway.GetDirectDialog().ChatLogItems, Is.Empty,
                "and the screen's copy of the same words is not shown again");
        }

        /// <summary>
        /// The same duty line, with the sweep that finds nothing on screen put
        /// back in between - which is what really happens, and what the first
        /// version of this test left out:
        ///
        ///   16:37:31.925  the chat log's copy is recorded
        ///                 a sweep finds the screen still empty
        ///   16:37:31.971  the screen's copy arrives
        ///
        /// Clearing the memory on the empty sweep undid the whole guard, and
        /// the test that omitted the sweep passed while the duty duplicated
        /// every line.
        /// </summary>
        [Test]
        public void Gateway_ShowsOneLineOnce_AcrossASweepWithNothingOnScreen()
        {
            const string said = "No small coup to roll that boulder over!";

            var directDialogReader = new FakeDirectDialogReader();
            var current = TalkAddonRealtimeDialogSnapshot.Unavailable();
            var now = new DateTime(2026, 8, 24, 16, 37, 31, 925, DateTimeKind.Utc);
            var gateway = CreateGateway(directDialogReader, () => current, () => now);

            var fromChatLog = BuildResult(new ChatLogItem { Code = "0044", Line = "Thancred's Avatar:" + said });
            gateway.DropLinesAlreadySeenLive(fromChatLog);
            Assert.That(fromChatLog.ChatLogItems, Has.Count.EqualTo(1));

            now = now.AddMilliseconds(20);
            gateway.GetDirectDialog();

            now = now.AddMilliseconds(26);
            current = TalkAddonRealtimeDialogSnapshot.Available("0044", string.Empty, said);

            Assert.That(gateway.GetDirectDialog().ChatLogItems, Is.Empty,
                "the empty sweep in between must not erase what the log just said");
        }

        private static SharlayanGameMemoryGateway CreateGateway(
            FakeDirectDialogReader directDialogReader,
            Func<TalkAddonRealtimeDialogSnapshot> realtimeReader,
            Func<DateTime> clock)
        {
            return new SharlayanGameMemoryGateway(
                directDialogReader, new NullLogger(), realtimeReader, clock);
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

        private sealed class FakeDirectDialogReader : IDirectDialogReader
        {
            public int ExtractCalls { get; private set; }
            public int EqualityCalls { get; private set; }
            public ChatLogResult DirectDialogResult { get; set; } = new ChatLogResult();

            public ChatLogResult ExtractDirectDialog(ChatLogResult chatLogResult)
            {
                ExtractCalls++;
                return DirectDialogResult;
            }

            public bool CheckChatEquality(ChatLogItem item1, ChatLogItem item2)
            {
                EqualityCalls++;
                return true;
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