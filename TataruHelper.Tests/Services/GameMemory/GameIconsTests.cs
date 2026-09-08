using System.Text;

using FFXIVTataruHelper.Services.GameMemory;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.GameMemory
{
    /// <summary>
    /// The game puts little pictures in the middle of a sentence. They used to
    /// be thrown away with the rest of the markup, which is why a line arrived
    /// reading "Mentor symbols are as follows: : Expert in PvE combat".
    /// </summary>
    [TestFixture]
    public class GameIconsTests
    {
        /// <summary>
        /// Read off the client on 8 September, from the line about mentor
        /// symbols: start, the icon macro, a length, the id, end.
        /// </summary>
        [TestCase(new byte[] { 0x02, 0x12, 0x02, 0x50, 0x03 }, 79)]
        [TestCase(new byte[] { 0x02, 0x12, 0x02, 0x51, 0x03 }, 80)]
        [TestCase(new byte[] { 0x02, 0x12, 0x02, 0x52, 0x03 }, 81)]
        public void AnIconPayload_GivesItsIcon(byte[] payload, int expected)
        {
            Assert.That(TalkAddonRealtimeReader.IconIn(payload, 0, payload.Length - 1), Is.EqualTo(expected));
        }

        /// <summary>An id of 0xD0 or over is written as a byte of its own.</summary>
        [Test]
        public void ALargeIcon_IsReadFromTheByteThatFollows()
        {
            var payload = new byte[] { 0x02, 0x12, 0x03, 0xF0, 0xC8, 0x03 };

            Assert.That(TalkAddonRealtimeReader.IconIn(payload, 0, payload.Length - 1), Is.EqualTo(200));
        }

        // A colour change, a line break, an item link: payloads, but not icons.
        [TestCase(new byte[] { 0x02, 0x13, 0x02, 0x50, 0x03 })]
        [TestCase(new byte[] { 0x02, 0x10, 0x01, 0x03 })]
        public void SomethingElseInAPayload_IsNotAnIcon(byte[] payload)
        {
            Assert.That(TalkAddonRealtimeReader.IconIn(payload, 0, payload.Length - 1), Is.Zero);
        }

        [Test]
        public void AnIconSurvivesTheLine_AsOneCharacter()
        {
            var line = new byte[] { (byte)'A', 0x02, 0x12, 0x02, 0x50, 0x03, (byte)'B' };
            var decoded = TalkAddonRealtimeReader.DecodeGameString(line, 0, line.Length);

            Assert.That(decoded.Length, Is.EqualTo(3), "one character stands for the icon");
            Assert.That(GameIcons.IdOf(decoded[1]), Is.EqualTo(79));
            Assert.That(GameIcons.Strip(decoded), Is.EqualTo("AB"));
        }

        /// <summary>
        /// A payload that is not an icon still goes, as it always did: a colour
        /// change carries no words.
        /// </summary>
        [Test]
        public void APayloadThatIsNotAnIcon_StillGoes()
        {
            var line = new byte[] { (byte)'A', 0x02, 0x13, 0x02, 0x50, 0x03, (byte)'B' };

            Assert.That(TalkAddonRealtimeReader.DecodeGameString(line, 0, line.Length), Is.EqualTo("AB"));
        }

        [Test]
        public void AMarkKnowsWhichIconItIs()
        {
            var mark = GameIcons.Mark(79);

            Assert.That(mark.Length, Is.EqualTo(1));
            Assert.That(GameIcons.IsMark(mark[0]), Is.True);
            Assert.That(GameIcons.IdOf(mark[0]), Is.EqualTo(79));
        }

        [Test]
        public void OrdinaryText_CarriesNoMarks()
        {
            const string line = "Пусть духи стихий будут благосклонны к тебе.";

            Assert.That(GameIcons.Strip(line), Is.SameAs(line), "nothing to take out, nothing rebuilt");
            foreach (var character in line)
            {
                Assert.That(GameIcons.IsMark(character), Is.False);
            }
        }

        /// <summary>
        /// The screen carries the icons and the chat log does not, so the key
        /// the two are matched on cannot carry them either - or every line with
        /// a mentor crown in it would go out twice.
        /// </summary>
        [Test]
        public void TheKeyTwoRoadsAreMatchedOn_IgnoresIcons()
        {
            var withIcon = "Mentor symbols are as follows: " + GameIcons.Mark(79) + ": Expert in PvE combat";
            const string fromTheChatLog = "Mentor symbols are as follows: : Expert in PvE combat";

            Assert.That(
                SharlayanGameMemoryGateway.BuildDuplicateKey(withIcon),
                Is.EqualTo(SharlayanGameMemoryGateway.BuildDuplicateKey(fromTheChatLog)));
        }
    }
}
