using System;
using System.Windows;

using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.Services.UI;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.UI
{
    /// <summary>
    /// Measured off the running game: between one line and the next the game's
    /// dialogue window is gone for a frame or two, because it is torn down and
    /// rebuilt rather than reused. Without the wait the copy blinked out and
    /// back on every line of every conversation.
    /// </summary>
    [TestFixture]
    public class DialogueOverlayHoldTests
    {
        private static readonly DateTime Start = new DateTime(2026, 8, 12, 18, 13, 29, DateTimeKind.Utc);

        private static readonly Rect Box = new Rect(1210, 1143, 1020, 270);

        [Test]
        public void WhatIsFoundNow_IsWhatIsDrawn()
        {
            var hold = new DialogueOverlayHold();

            Assert.That(hold.Decide(true, Box, DialogueSurface.Window, Start, out var drawn, out _), Is.True);
            Assert.That(drawn, Is.EqualTo(Box));
        }

        [Test]
        public void TheGapBetweenTwoLines_IsRiddenOut()
        {
            var hold = new DialogueOverlayHold();
            hold.Decide(true, Box, DialogueSurface.Window, Start, out _, out _);

            var shown = hold.Decide(false, Rect.Empty, DialogueSurface.None, Start.AddMilliseconds(80), out var drawn, out _);

            Assert.That(shown, Is.True, "the copy should stay put across the changeover");
            Assert.That(drawn, Is.EqualTo(Box), "and stay where it was, rather than jump");
        }

        [Test]
        public void AConversationThatHasEnded_ClearsTheCopy()
        {
            var hold = new DialogueOverlayHold();
            hold.Decide(true, Box, DialogueSurface.Window, Start, out _, out _);

            Assert.That(
                hold.Decide(false, Rect.Empty, DialogueSurface.None, Start + DialogueOverlayHold.Grace, out _, out _),
                Is.False);
        }

        /// <summary>
        /// The wait starts again from the last sighting, not from the first, or
        /// a long conversation would eventually run it out mid-flow.
        /// </summary>
        [Test]
        public void EachSighting_StartsTheWaitAgain()
        {
            var hold = new DialogueOverlayHold();
            hold.Decide(true, Box, DialogueSurface.Window, Start, out _, out _);
            hold.Decide(true, Box, DialogueSurface.Window, Start.AddSeconds(30), out _, out _);

            Assert.That(hold.Decide(false, Rect.Empty, DialogueSurface.None, Start.AddSeconds(30.1), out _, out _), Is.True);
        }

        [Test]
        public void OnceClearedItIsNotHeld()
        {
            var hold = new DialogueOverlayHold();
            hold.Decide(true, Box, DialogueSurface.Window, Start, out _, out _);
            hold.Clear();

            Assert.That(hold.Decide(false, Rect.Empty, DialogueSurface.None, Start.AddMilliseconds(10), out _, out _), Is.False);
        }

        [Test]
        public void WithNothingEverSeen_NothingIsDrawn()
        {
            Assert.That(
                new DialogueOverlayHold().Decide(false, Rect.Empty, DialogueSurface.None, Start, out _, out _),
                Is.False);
        }

        /// <summary>
        /// Replayed from the Crystal bearer cutscene on 8 September: after each
        /// of Hydaelyn's lines the game's wooden dialogue frame flashed up,
        /// stretched the width of the screen. The rectangle was held through
        /// the gap and what it was a copy of was not, so the last moment of a
        /// subtitle was drawn in the dress of a dialogue box.
        /// </summary>
        [Test]
        public void WhatIsHeldIsHeldInItsOwnDress()
        {
            var hold = new DialogueOverlayHold();
            var strip = new Rect(0, 562, 1280, 100);

            hold.Decide(true, strip, DialogueSurface.Subtitle, Start, out _, out _);

            var shown = hold.Decide(false, Rect.Empty, DialogueSurface.None, Start.AddMilliseconds(80),
                out var drawn, out var drawnSurface);

            Assert.That(shown, Is.True);
            Assert.That(drawn, Is.EqualTo(strip));
            Assert.That(drawnSurface, Is.EqualTo(DialogueSurface.Subtitle));
        }
    }
}
