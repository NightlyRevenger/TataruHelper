using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.Services.UI;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.UI
{
    /// <summary>
    /// When a copy of the game's dialogue box belongs on screen, and where.
    ///
    /// Every "no" here is a way the copy could otherwise be left hanging over
    /// nothing: the player alt-tabbed, the conversation ended, the translation
    /// has not come back yet.
    /// </summary>
    [TestFixture]
    public class DialogueOverlayPlacementTests
    {
        private static readonly AddonBounds Box = AddonBounds.From(1210f, 1143f, 680, 180, 1.5f);

        private static readonly GameWindowProjection FullScreen = new GameWindowProjection(0, 0, 1.0);

        private static bool Place(
            bool enabled = true,
            bool foreground = true,
            DialogueSurface surface = DialogueSurface.Window,
            AddonBounds? bounds = null,
            GameWindowProjection? projection = null,
            string text = "Пусть духи стихий будут благосклонны к тебе.")
        {
            return DialogueOverlayPlacement.TryPlace(
                enabled, foreground, surface, bounds ?? Box, projection ?? FullScreen, text, out _);
        }

        [Test]
        public void ALineOnScreenWithTheGameInFront_IsCovered()
        {
            var placed = DialogueOverlayPlacement.TryPlace(
                true, true, DialogueSurface.Window, Box, FullScreen, "Пусть духи стихий будут благосклонны.", out var rect);

            Assert.That(placed, Is.True);
            Assert.That(rect.Left, Is.EqualTo(1210));
            Assert.That(rect.Top, Is.EqualTo(1143));
            Assert.That(rect.Width, Is.EqualTo(1020));
            Assert.That(rect.Height, Is.EqualTo(270));
        }

        [Test]
        public void SwitchedOff_NothingIsDrawn()
        {
            Assert.That(Place(enabled: false), Is.False);
        }

        /// <summary>
        /// Alt-tabbed away, the game's box is not on screen either, and a copy
        /// left floating over somebody's browser is the worst thing this could
        /// do.
        /// </summary>
        [Test]
        public void WithTheGameBehindSomethingElse_NothingIsDrawn()
        {
            Assert.That(Place(foreground: false), Is.False);
        }

        [Test]
        public void WithNoLineOnScreen_NothingIsDrawn()
        {
            Assert.That(Place(bounds: AddonBounds.Unknown), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void BeforeTheTranslationComesBack_NothingIsDrawn(string text)
        {
            Assert.That(Place(text: text), Is.False);
        }

        /// <summary>
        /// A window being built or torn down reports a few pixels for a frame
        /// or two. A copy that flashed at that size reads as a glitch.
        /// </summary>
        [Test]
        public void ABoxTooSmallToBeOne_IsNotCovered()
        {
            Assert.That(Place(bounds: AddonBounds.From(100, 100, 20, 12, 1f)), Is.False);
        }

        /// <summary>
        /// The size that decides is the one on the desktop. A box that is big
        /// enough in the game's own counting can come out tiny on a display
        /// scaled up, and it is the reader who has to see it.
        /// </summary>
        [Test]
        public void OnAHeavilyScaledDisplay_TheDesktopSizeDecides()
        {
            var smallOnScreen = new GameWindowProjection(0, 0, 8.0);

            Assert.That(Place(projection: smallOnScreen), Is.False);
        }

        [Test]
        public void BeforeTheGameWindowIsFound_NothingIsDrawn()
        {
            Assert.That(Place(projection: GameWindowProjection.None), Is.False);
        }

        /// <summary>
        /// A cutscene subtitle is covered like anything else. It is drawn in no
        /// window, but it is drawn somewhere the client will say, and that is
        /// all the placing needs; what it looks like is the copy's business.
        /// </summary>
        [Test]
        public void ACutsceneSubtitle_IsCovered()
        {
            // The strip as the client reports it: the full width of the screen,
            // a hundred tall, low down.
            var strip = AddonBounds.From(0f, 562f, 1280, 100, 1f);

            Assert.That(Place(surface: DialogueSurface.Subtitle, bounds: strip), Is.True);
        }

        /// <summary>
        /// A bubble over a character's head is not covered, however good the
        /// rectangle looks. Every bubble on screen lives in one addon, and it
        /// reports the same corner and the same size whoever is speaking, so
        /// the rectangle is not the bubble's and covering it would put a box in
        /// the corner of the screen over nothing at all.
        /// </summary>
        [Test]
        public void ASpeechBubble_IsNotCovered()
        {
            Assert.That(Place(surface: DialogueSurface.Bubble), Is.False);
        }

        [Test]
        public void WithNothingBeingDrawn_NothingIsCovered()
        {
            Assert.That(Place(surface: DialogueSurface.None), Is.False);
        }

        /// <summary>
        /// A notice - "The New Adventurer status is applied..." - is drawn in
        /// the dialogue window's place and is covered like one. What differs is
        /// the dress, not the placing.
        /// </summary>
        [Test]
        public void ANotice_IsCovered()
        {
            Assert.That(Place(surface: DialogueSurface.Notice), Is.True);
        }
    }
}
