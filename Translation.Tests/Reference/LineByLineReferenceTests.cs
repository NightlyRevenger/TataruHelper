using System;
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
    /// Text that arrives as several lines at once - the question a cutscene
    /// asks, and every answer under it - is known to the hand-made index a line
    /// at a time and not as the piece it arrives in.
    ///
    /// Without asking line by line the whole choice went to a service, and
    /// every answer read as a machine had put it, though all three lines were
    /// sitting in the index. Checked against the shipped one on 9 September:
    /// "What will you say?", "I thought you were always watching?" and "Miss
    /// that part, did you?" are all in there.
    /// </summary>
    [TestFixture]
    public class LineByLineReferenceTests
    {
        private sealed class KnownLines : IReferenceTranslationSource
        {
            private readonly Dictionary<string, string> _known;

            public KnownLines(Dictionary<string, string> known)
            {
                _known = known;
            }

            public string LanguageCode => "ru";

            public string SourceLanguageCode => "en";

            public string Revision => string.Empty;

            public int LineCount => _known.Count;

            public int RulesVersion => 7;

            public bool TryGetTranslation(string sentence, out string translation)
            {
                return _known.TryGetValue(sentence ?? string.Empty, out translation);
            }

            public string PlayerName { get; set; } = string.Empty;

            public bool? PlayerIsFeminine { get; set; }

            public bool TryGetSpeakerName(string speaker, out string translated)
            {
                translated = string.Empty;
                return false;
            }

            public void Dispose()
            {
            }
        }

        /// <summary>A service that answers the same thing whatever it is asked, and counts.</summary>
        private sealed class AService : ITranslationProvider
        {
            private readonly string _answer;

            public AService(string answer)
            {
                _answer = answer;
            }

            public TranslationEngineName EngineName => TranslationEngineName.GoogleTranslate;

            public int CallCount { get; private set; }

            public Task<string> TranslateAsync(string sentence, string inLang, string outLang,
                CancellationToken cancellationToken)
            {
                CallCount++;
                return Task.FromResult(_answer);
            }
        }

        private static readonly Dictionary<string, string> TheChoice = new Dictionary<string, string>
        {
            { "What will you say?", "Что ты скажешь?" },
            { "I thought you were always watching?", "Я думал, ты всегда наблюдаешь?" },
            { "Miss that part, did you?", "Пропустил эту часть, да?" }
        };

        private static WebTranslator TranslatorKnowing(Dictionary<string, string> known, AService service)
        {
            return new WebTranslator(
                NullLogger.Instance,
                new[] { service },
                new TranslationSettings(),
                referenceTranslations: new KnownLines(known))
            {
                UseReferenceTranslations = true
            };
        }

        private static TranslationEngine Engine()
        {
            return new TranslationEngine(
                TranslationEngineName.GoogleTranslate,
                new List<TranslatorLanguage>
                {
                    new TranslatorLanguage("English", "English", "en"),
                    new TranslatorLanguage("Russian", "Russian", "ru")
                },
                1);
        }

        private static readonly TranslatorLanguage From = new TranslatorLanguage("English", "English", "en");

        private static readonly TranslatorLanguage To = new TranslatorLanguage("Russian", "Russian", "ru");

        [Test]
        public async Task EveryLineKnownByHand_IsAnsweredByHand()
        {
            var service = new AService("a machine had a go");
            var translator = TranslatorKnowing(TheChoice, service);

            var result = await translator.TranslateAsync(
                "What will you say?\n1. I thought you were always watching?\n2. Miss that part, did you?",
                Engine(), From, To);

            Assert.That(result.IsLiterary, Is.True);
            Assert.That(result.Text, Is.EqualTo(
                "Что ты скажешь?\n1. Я думал, ты всегда наблюдаешь?\n2. Пропустил эту часть, да?"));
            Assert.That(service.CallCount, Is.Zero, "nothing was asked of a service");
        }

        /// <summary>
        /// All or none. A piece half by hand and half by a service reads as two
        /// translations stacked on each other, and nothing outside can tell
        /// which half is which.
        /// </summary>
        [Test]
        public async Task OneLineUnknown_SendsTheWholePieceToAService()
        {
            var missingOne = new Dictionary<string, string>(TheChoice);
            missingOne.Remove("Miss that part, did you?");

            var service = new AService("a machine had a go");
            var translator = TranslatorKnowing(missingOne, service);

            var result = await translator.TranslateAsync(
                "What will you say?\n1. I thought you were always watching?\n2. Miss that part, did you?",
                Engine(), From, To);

            Assert.That(result.IsLiterary, Is.False);
            Assert.That(service.CallCount, Is.EqualTo(1));
        }

        [Test]
        public async Task OneLineOnItsOwn_IsAnsweredTheWayItAlwaysWas()
        {
            var service = new AService("a machine had a go");
            var translator = TranslatorKnowing(TheChoice, service);

            var result = await translator.TranslateAsync("What will you say?", Engine(), From, To);

            Assert.That(result.IsLiterary, Is.True);
            Assert.That(result.Text, Is.EqualTo("Что ты скажешь?"));
        }

        /// <summary>
        /// The marker is this application's, not the game's, so it is set aside
        /// for the lookup and put back after - and a number the game itself
        /// wrote is not a marker.
        /// </summary>
        [Test]
        public async Task ANumberTheGameWrote_IsPartOfTheLine()
        {
            var known = new Dictionary<string, string>
            {
                { "10 gil for that?", "Десять гилей за это?" },
                { "Yes.", "Да." }
            };

            var service = new AService("a machine had a go");
            var translator = TranslatorKnowing(known, service);

            var result = await translator.TranslateAsync("10 gil for that?\nYes.", Engine(), From, To);

            Assert.That(result.IsLiterary, Is.True);
            Assert.That(result.Text, Is.EqualTo("Десять гилей за это?\nДа."));
        }
    }
}
