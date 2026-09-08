using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.Services.UI;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.UI
{
    /// <summary>
    /// The first line of a conversation went up undrawn, because being dressed
    /// for a line and being on screen were one question, and the first line's
    /// answer to it was "already dressed, so change nothing". Dressing and
    /// showing are separate questions, and this is the one that says so.
    /// </summary>
    [TestFixture]
    public class DialogueOverlayPresentationTests
    {
        [Test]
        public void TheFirstLineOfAConversation_IsShown()
        {
            var presentation = new DialogueOverlayPresentation();

            Assert.That(
                presentation.Present(DialogueSurface.Window, out _),
                Is.True,
                "the first dialogue line must be put on screen, not merely dressed");
        }

        [Test]
        public void ASecondLineInTheSameDress_IsNotShownAgain()
        {
            var presentation = new DialogueOverlayPresentation();
            presentation.Present(DialogueSurface.Window, out _);

            Assert.That(presentation.Present(DialogueSurface.Window, out _), Is.False);
        }

        [Test]
        public void ALineAfterAHide_IsShownAgain()
        {
            var presentation = new DialogueOverlayPresentation();
            presentation.Present(DialogueSurface.Window, out _);
            presentation.Hide();

            Assert.That(presentation.Present(DialogueSurface.Window, out _), Is.True);
        }

        [Test]
        public void ASubtitleLine_ChangesTheDress()
        {
            var presentation = new DialogueOverlayPresentation();
            presentation.Present(DialogueSurface.Window, out _);

            var shown = presentation.Present(DialogueSurface.Subtitle, out var restyled);

            Assert.That(shown, Is.False, "the copy is already on screen");
            Assert.That(restyled, Is.True);
        }

        [Test]
        public void ADialogueLineAfterASubtitle_ChangesTheDressBack()
        {
            var presentation = new DialogueOverlayPresentation();
            presentation.Present(DialogueSurface.Subtitle, out _);

            var shown = presentation.Present(DialogueSurface.Window, out var restyled);

            Assert.That(shown, Is.False, "the copy is already on screen");
            Assert.That(restyled, Is.True);
        }

        [Test]
        public void HidingKeepsTheDress()
        {
            var presentation = new DialogueOverlayPresentation();
            presentation.Present(DialogueSurface.Subtitle, out _);
            presentation.Hide();

            var mustShow = presentation.Present(DialogueSurface.Subtitle, out var restyled);

            Assert.That(mustShow, Is.True, "coming back to the screen is showing");
            Assert.That(
                restyled,
                Is.False,
                "coming back to the screen is not a change of what is covered");
        }

        [Test]
        public void TheFirstLine_AsksForNoRestyle()
        {
            var presentation = new DialogueOverlayPresentation();

            // Fresh out of the door the copy is dressed as a dialogue line, so
            // the first line only asks to be shown, not restyled over.
            var mustShow = presentation.Present(DialogueSurface.Window, out var restyled);

            Assert.That(mustShow, Is.True, "the first line goes on screen");
            Assert.That(restyled, Is.False);
        }
    }
}