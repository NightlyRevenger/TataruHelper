using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

using Translation.Models;
using Translation.Settings;

namespace Translation.Tests
{
    /// <summary>
    /// The text kind has to survive the whole way down to the provider, and it
    /// has to be part of the cache key: the same line spoken in chat and in a
    /// cutscene wants two different prompts, so it must not share one cached
    /// translation.
    /// </summary>
    [TestFixture]
    public class WebTranslatorStoryKindTests
    {
        [Test]
        public async Task TranslateAsync_PassesTheKind_DownToTheProvider()
        {
            var provider = new KindCapturingProvider(TranslationEngineName.GoogleTranslate);
            var translator = new WebTranslator(
                NullLogger.Instance, new[] { provider }, new TranslationSettings());
            var engine = CreateEngine(TranslationEngineName.GoogleTranslate);
            var from = new TranslatorLanguage("English", "English", "en");
            var to = new TranslatorLanguage("German", "German", "de");

            await translator.TranslateAsync(
                "The dragon stirs.", engine, from, to, TranslationTextKind.Story, CancellationToken.None);

            Assert.That(provider.Kinds, Is.EqualTo(new[] { TranslationTextKind.Story }));
        }

        [Test]
        public async Task TranslateAsync_SameLine_ChatAndStory_AreTwoDistinctRequests()
        {
            var provider = new KindCapturingProvider(TranslationEngineName.GoogleTranslate);
            var translator = new WebTranslator(
                NullLogger.Instance, new[] { provider }, new TranslationSettings());
            var engine = CreateEngine(TranslationEngineName.GoogleTranslate);
            var from = new TranslatorLanguage("English", "English", "en");
            var to = new TranslatorLanguage("German", "German", "de");

            await translator.TranslateAsync(
                "We meet again.", engine, from, to, TranslationTextKind.Chat, CancellationToken.None);
            await translator.TranslateAsync(
                "We meet again.", engine, from, to, TranslationTextKind.Story, CancellationToken.None);

            // Different kind means a different cache key: the story call must
            // not be served the chat translation.
            Assert.That(provider.Kinds,
                Is.EqualTo(new[] { TranslationTextKind.Chat, TranslationTextKind.Story }));
        }

        private static TranslationEngine CreateEngine(TranslationEngineName engineName)
        {
            return new TranslationEngine(
                engineName,
                new List<TranslatorLanguage>
                {
                    new TranslatorLanguage("English", "English", "en"),
                    new TranslatorLanguage("German", "German", "de")
                },
                10);
        }

        /// <summary>
        /// Records the kind of every call in order; answers with a fixed string
        /// so the translation itself is never the point of the test.
        /// </summary>
        private sealed class KindCapturingProvider : ITranslationProvider
        {
            public KindCapturingProvider(TranslationEngineName engineName)
            {
                EngineName = engineName;
            }

            public TranslationEngineName EngineName { get; private set; }

            public List<TranslationTextKind> Kinds { get; } = new List<TranslationTextKind>();

            public Task<string> TranslateAsync(string sentence, string inLang,
                string outLang, CancellationToken cancellationToken)
            {
                return TranslateAsync(sentence, inLang, outLang, TranslationTextKind.Chat, cancellationToken);
            }

            public Task<string> TranslateAsync(string sentence, string inLang,
                string outLang, TranslationTextKind kind, CancellationToken cancellationToken)
            {
                Kinds.Add(kind);
                return Task.FromResult("translated:" + sentence);
            }
        }
    }
}
