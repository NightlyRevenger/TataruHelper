using FFXIVTataruHelper;

using NUnit.Framework;

namespace TataruHelper.Tests
{
    [TestFixture]
    public class ChatMessageFilterCharacterizationTests
    {
        [Test]
        public void ShouldTranslate_ReturnsFalse_ForBlacklistedMessage()
        {
            var filter = new ChatMessageFilter(
                new[] { "Updating online status to Away from Keyboard." },
                new string[0]);

            var shouldTranslate = filter.ShouldTranslate("Updating online status to Away from Keyboard.");

            Assert.That(shouldTranslate, Is.False);
        }

        [Test]
        public void ShouldTranslate_ReturnsTrue_ForRegularMessage()
        {
            var filter = new ChatMessageFilter(
                new[] { "System message" },
                new string[0]);

            var shouldTranslate = filter.ShouldTranslate("Hello from party chat");

            Assert.That(shouldTranslate, Is.True);
        }

        [Test]
        public void TrySplitNickname_SplitsOnlyForConfiguredChatCode()
        {
            var filter = new ChatMessageFilter(
                new string[0],
                new[] { "000A" });

            var split = filter.TrySplitNickname("000A", "Player Name: hello world", out var nickname, out var body);

            Assert.That(split, Is.True);
            Assert.That(nickname, Is.EqualTo("Player Name:"));
            Assert.That(body, Is.EqualTo(" hello world"));
        }

        [Test]
        public void TrySplitNickname_DoesNotSplit_WhenChatCodeNotConfigured()
        {
            var filter = new ChatMessageFilter(
                new string[0],
                new[] { "000A" });

            var split = filter.TrySplitNickname("000B", "Player Name: hello world", out var nickname, out var body);

            Assert.That(split, Is.False);
            Assert.That(nickname, Is.Empty);
            Assert.That(body, Is.EqualTo("Player Name: hello world"));
        }

        [TestCase("003D")]
        [TestCase("0044")]
        public void TrySplitNickname_SplitsDirectDialogSpeakerCodes(string chatCode)
        {
            var filter = new ChatMessageFilter(
                new string[0],
                new[] { "003D", "0044" });

            var split = filter.TrySplitNickname(chatCode, "Npc Name: hello world", out var nickname, out var body);

            Assert.That(split, Is.True);
            Assert.That(nickname, Is.EqualTo("Npc Name:"));
            Assert.That(body, Is.EqualTo(" hello world"));
        }

        [Test]
        public void TrySplitNickname_SplitsFullWidthColonInNoviceNetwork()
        {
            var filter = new ChatMessageFilter(new string[0], new[] { "001B" });

            var split = filter.TrySplitNickname("001B", "Player Name\uFF1A hello world", out var nickname, out var body);

            Assert.Multiple(() =>
            {
                Assert.That(split, Is.True);
                Assert.That(nickname, Is.EqualTo("Player Name\uFF1A"));
                Assert.That(body, Is.EqualTo(" hello world"));
            });
        }

        [TestCase("0048")]
        [TestCase("000A")]
        [TestCase("000B")]
        [TestCase("000C")]
        [TestCase("000E")]
        [TestCase("000D")]
        [TestCase("0018")]
        [TestCase("0019")]
        [TestCase("001E")]
        [TestCase("000F")]
        [TestCase("0010")]
        [TestCase("0017")]
        [TestCase("0025")]
        [TestCase("006B")]
        [TestCase("001B")]
        [TestCase("001D")]
        [TestCase("001C")]
        public void IsPlayerChatCode_RecognizesEveryPlayerChatCategory(string chatCode)
        {
            Assert.That(ChatMessageFilter.IsPlayerChatCode(chatCode), Is.True);
        }

        [TestCase("003D")]
        [TestCase("0044")]
        [TestCase("2AB9")]
        [TestCase("0039")]
        [TestCase("0003")]
        public void IsPlayerChatCode_ExcludesStoryAndSystemCodes(string chatCode)
        {
            Assert.That(ChatMessageFilter.IsPlayerChatCode(chatCode), Is.False);
        }
    }
}
