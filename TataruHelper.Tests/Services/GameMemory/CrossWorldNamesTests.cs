using System.Collections.Generic;

using FFXIVTataruHelper.Services.GameMemory;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.GameMemory
{
    /// <summary>
    /// A player from another world arrives with that world stuck to the end of
    /// their name. Read off a live client on 2026-09-11, from an emote:
    ///
    ///   Cova Rae: Cova RaeLouisoix keeps time by swishing her baton.
    ///
    /// The game draws a little flower between "Rae" and "Louisoix". Sharlayan
    /// does not carry it, so the two arrive as one word, and a service made
    /// "Кова Раэлуисуа" of them.
    /// </summary>
    [TestFixture]
    public class CrossWorldNamesTests
    {
        private static readonly HashSet<string> Worlds = new HashSet<string>
        {
            "Louisoix", "Raiden", "Phoenix", "Titan", "Shiva", "Odin", "Ragnarok"
        };

        private static readonly string Flower = GameIcons.Mark(CrossWorldNames.CrossWorldIcon);

        [Test]
        public void TheNameAndItsWorldAreBothKeptFromTheService()
        {
            const string said = "Cova RaeLouisoix keeps time by swishing her baton authoritatively.";

            var asked = CrossWorldNames.Hide(said, Worlds, out var hidden);

            Assert.That(asked, Does.Not.Contain("Cova"));
            Assert.That(asked, Does.Not.Contain("Louisoix"));
            Assert.That(asked, Does.EndWith(" keeps time by swishing her baton authoritatively."));
            Assert.That(hidden, Has.Count.EqualTo(1));
            Assert.That(hidden[0], Is.EqualTo("Cova Rae" + Flower + "Louisoix"));
        }

        /// <summary>
        /// What a service hands back has the mark where it found it, and the
        /// name goes back there - with the flower between its halves, which is
        /// what the game itself shows.
        /// </summary>
        [Test]
        public void TheNameComesBackWithTheFlowerBetweenItsHalves()
        {
            const string said = "Cova RaeLouisoix keeps time by swishing her baton authoritatively.";

            var asked = CrossWorldNames.Hide(said, Worlds, out var hidden);
            var answered = asked.Replace(
                " keeps time by swishing her baton authoritatively.",
                " следит за временем, властно взмахивая своей дирижерской палочкой.");

            Assert.That(
                CrossWorldNames.Show(answered, hidden),
                Is.EqualTo("Cova Rae" + Flower + "Louisoix следит за временем, властно взмахивая своей дирижерской палочкой."));
        }

        /// <summary>
        /// The rule that keeps this from eating ordinary words: the world has
        /// to be written straight onto the name. Titan, Shiva and Odin are
        /// worlds, and every one of them is a perfectly good surname.
        /// </summary>
        [TestCase("Ser Aymeric Titan bows.")]
        [TestCase("Someone Shiva waves at you.")]
        [TestCase("Talk to Odin about it.")]
        public void AWorldWithASpaceInFrontOfItIsSomebodysSurname(string said)
        {
            var asked = CrossWorldNames.Hide(said, Worlds, out var hidden);

            Assert.That(hidden, Is.Empty);
            Assert.That(asked, Is.EqualTo(said));
        }

        /// <summary>
        /// And it has to end the word. "Raidenwatch" is not Raiden.
        /// </summary>
        [Test]
        public void AWorldThatIsOnlyTheStartOfALongerWordIsNotAWorld()
        {
            CrossWorldNames.Hide("Fenrir GarmRaidenwatch waves.", Worlds, out var hidden);

            Assert.That(hidden, Is.Empty);
        }

        [Test]
        public void TwoOfThemInOneLineAreBothKept()
        {
            const string said = "Cova RaeLouisoix waves at Sakuga VanhellsingRaiden.";

            var asked = CrossWorldNames.Hide(said, Worlds, out var hidden);

            Assert.That(hidden, Has.Count.EqualTo(2));
            Assert.That(hidden[0], Is.EqualTo("Cova Rae" + Flower + "Louisoix"));
            Assert.That(hidden[1], Is.EqualTo("Sakuga Vanhellsing" + Flower + "Raiden"));
            Assert.That(CrossWorldNames.Show(asked, hidden), Is.EqualTo(said.Replace(
                "RaeLouisoix", "Rae" + Flower + "Louisoix").Replace(
                "VanhellsingRaiden", "Vanhellsing" + Flower + "Raiden")));
        }

        /// <summary>
        /// A forename with an apostrophe in it, which half of Eorzea has.
        /// </summary>
        [Test]
        public void AnApostropheIsPartOfAName()
        {
            CrossWorldNames.Hide("Y'shtola RhulPhoenix nods.", Worlds, out var hidden);

            Assert.That(hidden[0], Is.EqualTo("Y'shtola Rhul" + Flower + "Phoenix"));
        }

        [Test]
        public void WithNoWorldsKnown_NothingIsTouched()
        {
            const string said = "Cova RaeLouisoix keeps time.";

            Assert.That(CrossWorldNames.Hide(said, new HashSet<string>(), out var hidden), Is.EqualTo(said));
            Assert.That(hidden, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        public void NothingToHide_IsNoTrouble(string said)
        {
            Assert.That(CrossWorldNames.Hide(said, Worlds, out var hidden), Is.Empty);
            Assert.That(hidden, Is.Empty);
            Assert.That(CrossWorldNames.Show(said, hidden), Is.Empty);
        }

        /// <summary>
        /// A service that drops the mark loses the name with it. Nothing can be
        /// done about that here; what matters is that the rest of the line
        /// comes back whole rather than the restoring falling over.
        /// </summary>
        [Test]
        public void AServiceThatLosesTheMarkDoesNotTakeTheLineWithIt()
        {
            CrossWorldNames.Hide("Cova RaeLouisoix waves.", Worlds, out var hidden);

            Assert.That(CrossWorldNames.Show("машет рукой.", hidden), Is.EqualTo("машет рукой."));
        }
    }
}
