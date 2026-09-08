using FFXIVTataruHelper;

using NUnit.Framework;

namespace TataruHelper.Tests.Views
{
    /// <summary>
    /// The copy shows the game's own words until the translation for them comes
    /// back, so it has to take the speaker's name off the front of a line
    /// without taking anything else with it.
    /// </summary>
    [TestFixture]
    public class DialogueOverlayWordsTests
    {
        [Test]
        public void TheNameComesOffTheFront()
        {
            Assert.That(
                DialogueOverlayWindow.WordsOf("Papalymo:Yda, look.", "Papalymo"),
                Is.EqualTo("Yda, look."));
        }

        /// <summary>
        /// The line the whole rule exists for. Cut at the first colon instead
        /// of at the name, the barkeep's own words would have been thrown away
        /// down to "your cares and your troubles".
        /// </summary>
        [Test]
        public void AColonInTheWordsIsNotTheName()
        {
            const string line = "I'tolwann:I'll say that again: your cares and your troubles.";

            Assert.That(
                DialogueOverlayWindow.WordsOf(line, "I'tolwann"),
                Is.EqualTo("I'll say that again: your cares and your troubles."));
        }

        /// <summary>
        /// A cutscene subtitle names nobody and carries no colon of its own -
        /// but its words may, and none of it is a name.
        /// </summary>
        [Test]
        public void WithNobodySpeaking_TheWholeLineIsTheWords()
        {
            const string line = "Hear: feel: think.";

            Assert.That(DialogueOverlayWindow.WordsOf(line, string.Empty), Is.EqualTo(line));
        }

        [Test]
        public void ANameThatIsNotOnTheFrontIsLeftAlone()
        {
            Assert.That(
                DialogueOverlayWindow.WordsOf("Yda, look.", "Papalymo"),
                Is.EqualTo("Yda, look."));
        }

        [TestCase(null, null)]
        [TestCase("", "Papalymo")]
        public void NothingToTakeFrom_IsNoTrouble(string line, string speaker)
        {
            Assert.That(DialogueOverlayWindow.WordsOf(line, speaker), Is.Empty);
        }
    }
}
