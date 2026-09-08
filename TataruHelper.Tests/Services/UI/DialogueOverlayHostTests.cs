using FFXIVTataruHelper.Services.UI;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.UI
{
    /// <summary>
    /// There is one game and one dialogue box, and every chat window is offered
    /// every line. Left to themselves they would each raise a copy of the box
    /// and stack them over the same place.
    /// </summary>
    [TestFixture]
    public class DialogueOverlayHostTests
    {
        private sealed class Window
        {
            public Window(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        [Test]
        public void TheFirstWindowToAsk_TakesTheCopy()
        {
            var host = new DialogueOverlayHost();
            var first = new Window("main");

            Assert.That(host.BelongsTo(first), Is.True);
            Assert.That(host.BelongsTo(first), Is.True, "and keeps it when it asks again");
        }

        [Test]
        public void ASecondWindow_IsToldNo()
        {
            var host = new DialogueOverlayHost();
            var first = new Window("main");
            var second = new Window("second");

            host.BelongsTo(first);

            Assert.That(host.BelongsTo(second), Is.False);
        }

        [Test]
        public void WhenTheHolderCloses_TheNextWindowTakesOver()
        {
            var host = new DialogueOverlayHost();
            var first = new Window("main");
            var second = new Window("second");

            host.BelongsTo(first);
            host.Release(first);

            Assert.That(host.BelongsTo(second), Is.True);
        }

        [Test]
        public void AWindowThatNeverHeldTheClaim_CannotGiveItAway()
        {
            var host = new DialogueOverlayHost();
            var first = new Window("main");
            var second = new Window("second");

            host.BelongsTo(first);
            host.Release(second);

            Assert.That(host.BelongsTo(second), Is.False, "the first window still holds it");
            Assert.That(host.BelongsTo(first), Is.True);
        }

        [Test]
        public void NobodyIsAsked_UntilThePlayerAsksForTheCopy()
        {
            var host = new DialogueOverlayHost();

            Assert.That(host.IsWanted, Is.False, "off until somebody chooses it");
        }

        [Test]
        public void TurningItOn_IsAnnouncedOnce()
        {
            var host = new DialogueOverlayHost();
            var announced = 0;
            host.WantedChanged += (_, __) => announced++;

            host.IsWanted = true;
            host.IsWanted = true;

            Assert.That(announced, Is.EqualTo(1), "setting it to what it already is changes nothing");
        }

        [Test]
        public void TurningItOff_IsAnnouncedToo()
        {
            var host = new DialogueOverlayHost();
            host.IsWanted = true;

            var announced = 0;
            host.WantedChanged += (_, __) => announced++;
            host.IsWanted = false;

            Assert.That(announced, Is.EqualTo(1),
                "the copy has to come down there and then, not at whatever is said next");
        }
    }
}
