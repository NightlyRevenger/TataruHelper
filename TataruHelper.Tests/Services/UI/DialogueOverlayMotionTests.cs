using System;

using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.Services.UI;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.UI
{
    /// <summary>
    /// The game grows its dialogue window open and shrinks it closed. Measured
    /// off a running client on 8 September: three frames at 0.85, 0.98 and 1.0
    /// of the full width over some eighty milliseconds, then full size for as
    /// long as the player reads, then the same in reverse.
    ///
    /// Both halves were wrong in their own way. The copy waited out the opening
    /// and left the original readable while it waited; it followed the closing
    /// down and jumped smaller before it vanished.
    /// </summary>
    [TestFixture]
    public class DialogueOverlayMotionTests
    {
        private static readonly DateTime Start = new DateTime(2026, 9, 8, 21, 30, 0, DateTimeKind.Utc);

        [Test]
        public void AWindowOpening_IsFollowedFromItsFirstFrame()
        {
            var motion = new DialogueOverlayMotion();

            Assert.That(motion.ShouldDraw(DialogueSurface.Window, 578, Start), Is.True);
            Assert.That(motion.ShouldDraw(DialogueSurface.Window, 664, Start.AddMilliseconds(40)), Is.True);
            Assert.That(motion.ShouldDraw(DialogueSurface.Window, 680, Start.AddMilliseconds(85)), Is.True);
        }

        [Test]
        public void AWindowSittingStill_KeepsTheCopy()
        {
            var motion = new DialogueOverlayMotion();
            motion.ShouldDraw(DialogueSurface.Window, 680, Start);

            Assert.That(motion.ShouldDraw(DialogueSurface.Window, 680, Start.AddSeconds(9)), Is.True);
        }

        /// <summary>
        /// The client reports the box in floating point and the last digits
        /// wander. A box a hundredth of a pixel narrower is the same box.
        /// </summary>
        [Test]
        public void AWidthThatOnlyWandered_IsTheSameWidth()
        {
            var motion = new DialogueOverlayMotion();
            motion.ShouldDraw(DialogueSurface.Window, 680, Start);

            Assert.That(motion.ShouldDraw(DialogueSurface.Window, 679.98, Start.AddMilliseconds(50)), Is.True);
        }

        [Test]
        public void AWindowClosing_TakesTheCopyOffAtOnce()
        {
            var motion = new DialogueOverlayMotion();
            motion.ShouldDraw(DialogueSurface.Window, 680, Start);

            Assert.That(
                motion.ShouldDraw(DialogueSurface.Window, 673, Start.AddMilliseconds(40)),
                Is.False,
                "the first frame narrower than it has been is the window on its way out");
        }

        /// <summary>
        /// The player moved the interface scale down while a conversation was
        /// open. The box really is smaller now, and waiting for a width that is
        /// not coming back would cost the rest of the conversation.
        /// </summary>
        [Test]
        public void ABoxThatStaysSmaller_IsTheSizeItIsNow()
        {
            var motion = new DialogueOverlayMotion();
            motion.ShouldDraw(DialogueSurface.Window, 680, Start);
            motion.ShouldDraw(DialogueSurface.Window, 540, Start.AddMilliseconds(40));

            Assert.That(
                motion.ShouldDraw(DialogueSurface.Window, 540, Start.AddMilliseconds(40) + DialogueOverlayMotion.Settles),
                Is.True);
        }

        /// <summary>
        /// A subtitle is the width of the screen and a dialogue box is not.
        /// Measured against one another the box looks like a window closing,
        /// and a conversation right after a cutscene went uncovered from the
        /// first line to the last.
        /// </summary>
        [Test]
        public void ADialogueBoxAfterASubtitle_IsNotAWindowClosing()
        {
            var motion = new DialogueOverlayMotion();
            motion.ShouldDraw(DialogueSurface.Subtitle, 1280, Start);

            Assert.That(motion.ShouldDraw(DialogueSurface.Window, 680, Start.AddSeconds(2)), Is.True);
        }

        [Test]
        public void AfterTheConversationEnds_TheNextBoxIsMeasuredAfresh()
        {
            var motion = new DialogueOverlayMotion();
            motion.ShouldDraw(DialogueSurface.Window, 680, Start);
            motion.Forget();

            Assert.That(motion.ShouldDraw(DialogueSurface.Window, 578, Start.AddSeconds(20)), Is.True);
        }
    }
}
