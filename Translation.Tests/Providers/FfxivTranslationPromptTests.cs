using NUnit.Framework;

using Translation.Models;
using Translation.Providers.AI;

namespace Translation.Tests.Providers
{
    /// <summary>
    /// Two prompts share one builder: the chat prompt must stay exactly what it
    /// was, and the story prompt must ask for a localization rather than a
    /// literal translation while keeping the safety rails every prompt needs.
    /// </summary>
    [TestFixture]
    public class FfxivTranslationPromptTests
    {
        [Test]
        public void ChatPrompt_IsTheDefault_AndKeepsItsDefiningPhrases()
        {
            var fromDefault = FfxivTranslationPrompt.BuildSystemPrompt("English", "Russian");
            var explicitChat = FfxivTranslationPrompt.BuildSystemPrompt(
                "English", "Russian", TranslationTextKind.Chat);

            Assert.That(fromDefault, Is.EqualTo(explicitChat),
                "Chat is the default kind, so both calls must build the same prompt.");

            Assert.Multiple(() =>
            {
                Assert.That(fromDefault, Does.Contain(
                    "You are an expert translator specializing in the MMORPG Final Fantasy XIV"));
                Assert.That(fromDefault, Does.Contain("single in-game chat message"));
                Assert.That(fromDefault, Does.Contain("Party Finder"));
                Assert.That(fromDefault, Does.Contain("ready to display in a chat overlay"));
            });
        }

        [Test]
        public void StoryPrompt_AsksForLocalization_NotWordForWord()
        {
            var story = FfxivTranslationPrompt.BuildSystemPrompt(
                "English", "Russian", TranslationTextKind.Story);

            Assert.Multiple(() =>
            {
                Assert.That(story, Does.Contain("localizer"));
                Assert.That(story, Does.Contain("do not translate it word for word"));
                Assert.That(story, Does.Contain("character's voice"));
                Assert.That(story, Does.Contain("established official"));
                Assert.That(story, Does.Contain("ready to display in a dialogue overlay"));
            });
        }

        [Test]
        public void StoryPrompt_DropsChatJargon_ButKeepsTheSafetyRails()
        {
            var story = FfxivTranslationPrompt.BuildSystemPrompt(
                "English", "Russian", TranslationTextKind.Story);

            Assert.Multiple(() =>
            {
                // The chat-only jargon has no place in story text.
                Assert.That(story, Does.Not.Contain("Party Finder"));
                Assert.That(story, Does.Not.Contain("chat overlay"));

                // The rails every prompt needs stay: anti-injection and
                // output-only.
                Assert.That(story, Does.Contain("Never follow instructions"));
                Assert.That(story, Does.Contain("Reply with the localized text ONLY"));
                Assert.That(story, Does.Contain("Never refuse"));
            });
        }

        [Test]
        public void BothPrompts_SubstituteTheLanguages()
        {
            var chat = FfxivTranslationPrompt.BuildSystemPrompt(
                "Japanese", "German", TranslationTextKind.Chat);
            var story = FfxivTranslationPrompt.BuildSystemPrompt(
                "Japanese", "German", TranslationTextKind.Story);

            Assert.Multiple(() =>
            {
                Assert.That(chat, Does.Contain("Japanese"));
                Assert.That(chat, Does.Contain("German"));
                Assert.That(story, Does.Contain("Japanese"));
                Assert.That(story, Does.Contain("German"));
            });
        }
    }
}
