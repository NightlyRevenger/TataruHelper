using FFXIVTataruHelper.WinUtils;

using NUnit.Framework;

namespace TataruHelper.Tests.WinUtils
{
    /// <summary>
    /// Moving and resizing the chat window by following the cursor, which is
    /// how it is done under Wine: there the window is kept out of the Linux
    /// desktop's window manager so it can stay above the game, and that costs
    /// it the system's own move and size loops.
    /// </summary>
    [TestFixture]
    public class HandTrackedBoundsTests
    {
        private static readonly HandTrackedBounds.Bounds Start =
            new HandTrackedBounds.Bounds(100, 200, 400, 300);

        private const double Min = 60;

        [Test]
        public void Moving_CarriesTheWindowAndKeepsItsSize()
        {
            Assert.That(HandTrackedBounds.Moved(Start, 30, -50),
                Is.EqualTo(new HandTrackedBounds.Bounds(130, 150, 400, 300)));
        }

        [Test]
        public void TheRightEdge_FollowsTheCursorAndTheLeftStaysPut()
        {
            Assert.That(HandTrackedBounds.Resized(Start, 1, 0, 120, 999, Min, Min),
                Is.EqualTo(new HandTrackedBounds.Bounds(100, 200, 520, 300)));
        }

        /// <summary>
        /// Pulling the left edge leftwards grows the window and moves its left
        /// side; the right side does not move.
        /// </summary>
        [Test]
        public void TheLeftEdge_FollowsTheCursorAndTheRightStaysPut()
        {
            var resized = HandTrackedBounds.Resized(Start, -1, 0, -80, 0, Min, Min);

            Assert.That(resized, Is.EqualTo(new HandTrackedBounds.Bounds(20, 200, 480, 300)));
            Assert.That(resized.Right, Is.EqualTo(Start.Right));
        }

        [Test]
        public void TheTopEdge_FollowsTheCursorAndTheBottomStaysPut()
        {
            var resized = HandTrackedBounds.Resized(Start, 0, -1, 0, -40, Min, Min);

            Assert.That(resized, Is.EqualTo(new HandTrackedBounds.Bounds(100, 160, 400, 340)));
            Assert.That(resized.Bottom, Is.EqualTo(Start.Bottom));
        }

        [Test]
        public void ACorner_MovesBothOfItsEdges()
        {
            Assert.That(HandTrackedBounds.Resized(Start, 1, 1, 50, 70, Min, Min),
                Is.EqualTo(new HandTrackedBounds.Bounds(100, 200, 450, 370)));
        }

        /// <summary>
        /// The cursor can be dragged far past the far edge. The window stops at
        /// its least size, and the held edge stops with it - the far edge is
        /// not pushed away.
        /// </summary>
        [Test]
        public void PulledPastTheFarEdge_TheWindowStopsAtItsLeastSize()
        {
            var fromTheRight = HandTrackedBounds.Resized(Start, 1, 0, -1000, 0, Min, Min);
            Assert.That(fromTheRight, Is.EqualTo(new HandTrackedBounds.Bounds(100, 200, Min, 300)));

            var fromTheLeft = HandTrackedBounds.Resized(Start, -1, 0, 1000, 0, Min, Min);
            Assert.That(fromTheLeft.Width, Is.EqualTo(Min));
            Assert.That(fromTheLeft.Right, Is.EqualTo(Start.Right), "the right side is where it was");
        }

        /// <summary>
        /// A side that is not held is left alone however far the cursor goes
        /// in its direction - pulling the right edge does nothing to the height.
        /// </summary>
        [Test]
        public void AnEdgeThatIsNotHeld_IsLeftAlone()
        {
            var resized = HandTrackedBounds.Resized(Start, 1, 0, 10, 500, Min, Min);

            Assert.That(resized.Top, Is.EqualTo(Start.Top));
            Assert.That(resized.Height, Is.EqualTo(Start.Height));
        }
    }
}
