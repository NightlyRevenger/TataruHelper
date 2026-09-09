using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

using Translation;
using Translation.Models;
using Translation.Reference;
using Translation.Settings;

namespace Translation.Tests.Reference
{
    /// <summary>
    /// The character's own name is nobody's to translate.
    ///
    /// Reported from a cutscene on 9 September: an NPC said "D'ark One..." and
    /// it came back "Д'Арк Один...". The service had no way of knowing those
    /// were not two English words - so it is not shown them.
    /// </summary>
    [TestFixture]
    public class PlayerNameIsNotTranslatedTests
    {
        /// <summary>A service that hands back whatever it was given, unchanged but for the words it knows.</summary>
        private sealed class AService : ITranslationProvider
        {
            private readonly Dictionary<string, string> _knows;

            public AService(Dictionary<string, string> knows = null)
            {
                _knows = knows ?? new Dictionary<string, string>();
            }

            public TranslationEngineName EngineName => TranslationEngineName.GoogleTranslate;

            public string LastAsked { get; private set; }

            public Task<string> TranslateAsync(string sentence, string inLang, string outLang,
                CancellationToken cancellationToken)
            {
                LastAsked = sentence;

                var answer = sentence;
                foreach (var pair in _knows)
                {
                    answer = answer.Replace(pair.Key, pair.Value);
                }

                return Task.FromResult(answer);
            }
        }

        private sealed class NoReference : IReferenceTranslationSource
        {
            public string LanguageCode => "ru";

            public string SourceLanguageCode => "en";

            public string Revision => string.Empty;

            public int LineCount => 0;

            public int RulesVersion => 7;

            public string PlayerName { get; set; } = string.Empty;

            public bool? PlayerIsFeminine { get; set; }

            public bool TryGetTranslation(string sentence, out string translation)
            {
                translation = string.Empty;
                return false;
            }

            public bool TryGetSpeakerName(string speaker, out string translated)
            {
                translated = string.Empty;
                return false;
            }

            public void Dispose()
            {
            }
        }

        private static readonly TranslatorLanguage From = new TranslatorLanguage("English", "English", "en");

        private static readonly TranslatorLanguage To = new TranslatorLanguage("Russian", "Russian", "ru");

        private static TranslationEngine Engine()
        {
            return new TranslationEngine(
                TranslationEngineName.GoogleTranslate,
                new List<TranslatorLanguage> { From, To },
                1);
        }

        private static WebTranslator Translator(AService service, string playerName)
        {
            return new WebTranslator(
                NullLogger.Instance,
                new[] { service },
                new TranslationSettings(),
                referenceTranslations: new NoReference())
            {
                PlayerName = playerName
            };
        }

        [Test]
        public async Task TheNameIsNotShownToTheService()
        {
            // A service that would happily turn "One" into a number word.
            var service = new AService(new Dictionary<string, string> { { "One", "Один" } });
            var translator = Translator(service, "D'ark One");

            var result = await translator.TranslateAsync("D'ark One...", Engine(), From, To);

            Assert.That(service.LastAsked, Does.Not.Contain("D'ark"));
            Assert.That(result.Text, Is.EqualTo("D'ark One..."));
        }

        /// <summary>
        /// The game writes "Go swiftly, D'ark." as readily as it writes the
        /// whole name, so the half people are called by is kept too.
        /// </summary>
        [Test]
        public async Task TheHalfOfItPeopleUseIsKeptAsWell()
        {
            var service = new AService(new Dictionary<string, string> { { "swiftly", "быстро" } });
            var translator = Translator(service, "D'ark One");

            var result = await translator.TranslateAsync("Go swiftly, D'ark.", Engine(), From, To);

            Assert.That(service.LastAsked, Does.Not.Contain("D'ark"));
            Assert.That(result.Text, Is.EqualTo("Go быстро, D'ark."));
        }

        /// <summary>
        /// Only where it stands on its own. A character called Al must not turn
        /// every "Also" in the game into a piece of somebody's name.
        /// </summary>
        [Test]
        public async Task ANameInsideAnotherWordIsNotAName()
        {
            var service = new AService();
            var translator = Translator(service, "Al Bhed");

            await translator.TranslateAsync("Also, mind the step.", Engine(), From, To);

            Assert.That(service.LastAsked, Is.EqualTo("Also, mind the step."));
        }

        [Test]
        public async Task WithNobodyNamed_NothingIsHidden()
        {
            var service = new AService();
            var translator = Translator(service, string.Empty);

            await translator.TranslateAsync("Hello there.", Engine(), From, To);

            Assert.That(service.LastAsked, Is.EqualTo("Hello there."));
        }

        /// <summary>
        /// A service that hands the mark back where it found it puts the name
        /// back in the middle of a Russian sentence, not at the end of it.
        /// </summary>
        [Test]
        public async Task TheNameComesBackWhereTheServicePutTheMark()
        {
            var service = new AService(new Dictionary<string, string>
            {
                { "Well met,", "Приветствую," },
                { "- how fare you?", "- как поживаешь?" }
            });

            var translator = Translator(service, "D'ark One");

            var result = await translator.TranslateAsync(
                "Well met, D'ark One - how fare you?", Engine(), From, To);

            Assert.That(result.Text, Is.EqualTo("Приветствую, D'ark One - как поживаешь?"));
        }
    }
}
