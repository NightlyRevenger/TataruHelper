using System.IO;

using FFXIVTataruHelper;

using Newtonsoft.Json;

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

        /// <summary>
        /// Built from the shipped list rather than from a roster written out
        /// here, so that a channel added to the file is covered by this test
        /// without anybody remembering to add it twice.
        /// </summary>
        private static ChatMessageFilter FilterWithShippedNicknameCodes()
        {
            var codes = JsonConvert.DeserializeObject<string[]>(
                File.ReadAllText(Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "Resources", "IgnoreNickNameChatCodes.json")));

            return new ChatMessageFilter(new string[0], codes);
        }

        [Test]
        public void IsPlayerChatCode_RecognizesEveryChannelThatCarriesAName()
        {
            var filter = FilterWithShippedNicknameCodes();

            foreach (var chatCode in new[]
                     {
                         "0048", "000A", "000B", "000C", "000E", "000D", "0018", "0019",
                         "001E", "000F", "0010", "0017", "0025", "006B", "001B", "001D", "001C"
                     })
            {
                Assert.That(filter.IsPlayerChatCode(chatCode), Is.True, chatCode);
            }
        }

        /// <summary>
        /// The two story codes carry a name too - an NPC's - and they belong to
        /// the other switch. Everything else is either player chat or carries
        /// no name at all.
        /// </summary>
        [TestCase("003D")]
        [TestCase("0044")]
        [TestCase("2AB9")]
        [TestCase("0039")]
        [TestCase("0003")]
        public void IsPlayerChatCode_ExcludesStoryAndSystemCodes(string chatCode)
        {
            Assert.That(FilterWithShippedNicknameCodes().IsPlayerChatCode(chatCode), Is.False);
        }

        /// <summary>
        /// The guard the derivation rests on: the shipped list is the story
        /// codes plus player chat and nothing else, so subtracting the two
        /// leaves exactly the player channels.
        /// </summary>
        [Test]
        public void EveryShippedNicknameCode_IsEitherPlayerChatOrStory()
        {
            var codes = JsonConvert.DeserializeObject<string[]>(
                File.ReadAllText(Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "Resources", "IgnoreNickNameChatCodes.json")));
            var filter = new ChatMessageFilter(new string[0], codes);

            foreach (var chatCode in codes)
            {
                var isStory = chatCode == "003D" || chatCode == "0044";
                Assert.That(filter.IsPlayerChatCode(chatCode), Is.EqualTo(!isStory), chatCode);
            }
        }
    }
}
