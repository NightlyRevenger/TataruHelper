using FFXIVTataruHelper.Services.GameMemory;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.GameMemory
{
    /// <summary>
    /// Which of the game's windows a line is being drawn in, worked out from
    /// the key the reader files its candidates under.
    /// </summary>
    [TestFixture]
    public class DialogueSurfaceTests
    {
        [TestCase("Talk@7FF6A1B2C3D0", DialogueSurface.Window)]
        [TestCase("TalkSubtitle@7FF6A1B2C3D0", DialogueSurface.Subtitle)]
        [TestCase("MiniTalk@7FF6A1B2C3D0", DialogueSurface.Bubble)]
        [TestCase("_MiniTalk@7FF6A1B2C3D0", DialogueSurface.Bubble)]
        [TestCase("SelectString@7FF6A1B2C3D0", DialogueSurface.None)]
        [TestCase("", DialogueSurface.None)]
        [TestCase(null, DialogueSurface.None)]
        public void TheKeySaysWhatIsBeingDrawnIn(string candidateKey, DialogueSurface expected)
        {
            Assert.That(TalkAddonRealtimeReader.SurfaceOf(candidateKey), Is.EqualTo(expected));
        }

        /// <summary>
        /// "Talk" is the start of "TalkSubtitle". Matched on the prefix, every
        /// cutscene subtitle came back a dialogue box - which is a wooden frame
        /// hung in the middle of a cutscene.
        /// </summary>
        [Test]
        public void ASubtitleIsNotADialogueBoxBecauseItsNameStartsTheSame()
        {
            Assert.That(
                TalkAddonRealtimeReader.SurfaceOf("TalkSubtitle@1"),
                Is.Not.EqualTo(TalkAddonRealtimeReader.SurfaceOf("Talk@1")));
        }
    }
}
