using System.Windows;
using System.Windows.Media;

using FFXIVTataruHelper.Services.UI;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.UI
{
    /// <summary>
    /// The game wraps its own line to its own box, in the language it was
    /// written in. The translation is another language and another length, so
    /// a copy set at the game's own size can have the tail of a long line
    /// hanging out of the bottom of the box.
    /// </summary>
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class DialogueOverlayFitTests
    {
        /// <summary>
        /// The parchment inside the dialogue box at an interface scale of
        /// 150%: the light part of the game's own frame, 0.196 to 0.793 down
        /// the box, with the line starting 0.091 across and 0.233 down.
        /// </summary>
        private static readonly Size Room = new Size(1020 * (1 - 0.091 - 0.091), 270 * (1 - 0.233 - 0.207));

        private const double GameSize = 270 * 0.098;

        private static Typeface Face()
        {
            return new Typeface(
                new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        }

        private static double Fit(string words, double wanted = GameSize)
        {
            return DialogueOverlayFit.LargestThatFits(words, Room, Face(), 1.0, wanted);
        }

        [Test]
        public void ALineThatFitsIsLeftAtTheGamesOwnSize()
        {
            Assert.That(Fit("Пусть духи стихий будут благосклонны к тебе."), Is.EqualTo(GameSize));
        }

        /// <summary>
        /// Reported on 10 September, and found in the reporter's own chat log:
        /// Nicia on what becomes of the dead. It came out with its last
        /// sentence hanging below the parchment, unreadable. At the game's own
        /// size it overflows by a single line.
        /// </summary>
        [Test]
        public void TheLineThatWouldNotFit_IsBroughtDownUntilItDoes()
        {
            const string said =
                "Считается, что когда мы умираем, шок отделяет наш дух от телесной оболочки. " +
                "Затем наши тела распадаются и поглощаются эфирной рекой, в то время как душа " +
                "отправляется в путешествие к своему конечному пункту назначения в загробной жизни. " +
                "Некоторые называют это «возвращением в Поток жизни».";

            var size = Fit(said);

            Assert.That(size, Is.LessThan(GameSize), "it did not fit and was not brought down");
            Assert.That(size, Is.GreaterThan(DialogueOverlayFit.SmallestReadable));

            var laid = new FormattedText(
                said, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                Face(), size, Brushes.Black, 1.0)
            {
                MaxTextWidth = Room.Width
            };

            Assert.That(laid.Height, Is.LessThanOrEqualTo(Room.Height), "brought down, and still does not fit");
        }

        /// <summary>
        /// A line long enough that no readable size holds it is cut off rather
        /// than shrunk into something nobody can read.
        /// </summary>
        [Test]
        public void ALineNoSizeWouldHold_StopsAtTheSmallestReadable()
        {
            var endless = string.Join(" ", System.Linq.Enumerable.Repeat("Совершенно бесконечная реплика", 200));

            Assert.That(Fit(endless), Is.EqualTo(DialogueOverlayFit.SmallestReadable));
        }

        [TestCase(null)]
        [TestCase("")]
        public void NothingToFit_IsLeftAlone(string words)
        {
            Assert.That(Fit(words), Is.EqualTo(GameSize));
        }

        [Test]
        public void ABoxWithNoRoomInIt_IsLeftAlone()
        {
            Assert.That(
                DialogueOverlayFit.LargestThatFits("Что-нибудь", new Size(0, 0), Face(), 1.0, GameSize),
                Is.EqualTo(GameSize));
        }
    }
}
