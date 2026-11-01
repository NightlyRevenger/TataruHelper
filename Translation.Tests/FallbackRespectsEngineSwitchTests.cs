using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

using Translation.Credentials;
using Translation.Models;
using Translation.Settings;

namespace Translation.Tests
{
    /// <summary>
    /// When the selected engine fails, another one stands in for it. It must be
    /// one the user actually left switched on.
    ///
    /// This went unnoticed for as long as every engine needed a key: one without
    /// a key raised before it reached the network, so a switched-off engine
    /// looked skipped when it was only failing early. An engine that needs no key
    /// - a model on the player's own machine, a keyless web endpoint - has
    /// nothing to raise, and gets called on every failed line regardless of its
    /// switch.
    /// </summary>
    [TestFixture]
    public class FallbackRespectsEngineSwitchTests
    {
        [Test]
        public async Task AnEngineTheUserSwitchedOff_IsNotUsedAsAStandIn()
        {
            var selected = new FakeProvider(TranslationEngineName.GoogleTranslate, string.Empty);
            var switchedOff = new FakeProvider(TranslationEngineName.Ollama, "translated by the local model");

            var result = await TranslateWith(selected, switchedOff,
                new FakeCredentialStore { Disabled = { TranslationEngineName.Ollama } });

            Assert.That(switchedOff.CallCount, Is.Zero, "a switched-off engine was called anyway");
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public async Task AnEngineTheUserLeftOn_IsStillUsedAsAStandIn()
        {
            var selected = new FakeProvider(TranslationEngineName.GoogleTranslate, string.Empty);
            var switchedOn = new FakeProvider(TranslationEngineName.Ollama, "translated by the local model");

            var result = await TranslateWith(selected, switchedOn, new FakeCredentialStore());

            Assert.That(switchedOn.CallCount, Is.EqualTo(1));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Text, Is.EqualTo("translated by the local model"));
        }

        [Test]
        public async Task AnUnchangedFallbackAnswerDoesNotHideTheNextWorkingEngine()
        {
            const string sentence = "The Warrior of Light draws near.";
            var selected = new FakeProvider(TranslationEngineName.GoogleTranslate, "");
            var echoed = new FakeProvider(TranslationEngineName.YandexFree, sentence);
            var translated = new FakeProvider(TranslationEngineName.Bing, "Приближается Воин Света.");
            var translator = new WebTranslator(NullLogger.Instance,
                new ITranslationProvider[] { selected, echoed, translated }, new TranslationSettings());
            translator.LoadLanguages();
            var result = await translator.TranslateAsync(sentence,
                new TranslationEngine(TranslationEngineName.GoogleTranslate, new List<TranslatorLanguage>(), 9),
                new TranslatorLanguage("English", "English", "en"),
                new TranslatorLanguage("Russian", "Russian", "ru"));
            Assert.That(echoed.CallCount, Is.EqualTo(1));
            Assert.That(translated.CallCount, Is.EqualTo(1));
            Assert.That(result.Text, Is.EqualTo("Приближается Воин Света."));
        }

        private static async Task<TranslationResult> TranslateWith(
            FakeProvider selected, FakeProvider standIn, FakeCredentialStore credentials)
        {
            var translator = new WebTranslator(
                NullLogger.Instance,
                new[] { (ITranslationProvider)selected, standIn },
                new TranslationSettings(),
                credentials: credentials);

            // The stand-in search walks the engine catalog, which only exists
            // once the language lists have been read.
            translator.LoadLanguages();

            var engine = new TranslationEngine(
                TranslationEngineName.GoogleTranslate,
                new List<TranslatorLanguage>(),
                9);

            return await translator.TranslateAsync(
                "The Warrior of Light draws near.",
                engine,
                new TranslatorLanguage("English", "English", "en"),
                new TranslatorLanguage("Russian", "Russian", "ru"));
        }

        private sealed class FakeProvider : ITranslationProvider
        {
            private readonly string _response;

            public TranslationEngineName EngineName { get; }
            public int CallCount { get; private set; }

            public FakeProvider(TranslationEngineName engineName, string response)
            {
                EngineName = engineName;
                _response = response;
            }

            public Task<string> TranslateAsync(string sentence, string inLang, string outLang,
                CancellationToken cancellationToken)
            {
                CallCount++;
                return Task.FromResult(_response ?? string.Empty);
            }
        }

        private sealed class FakeCredentialStore : ITranslationCredentialStore
        {
            public HashSet<TranslationEngineName> Disabled { get; } = new HashSet<TranslationEngineName>();

            public bool IsEngineEnabled(TranslationEngineName engine) => !Disabled.Contains(engine);

            public string GetApiKey(TranslationEngineName engine) => string.Empty;
            public string GetRegion(TranslationEngineName engine) => string.Empty;
            public string GetModel(TranslationEngineName engine) => string.Empty;
            public string GetEndpoint(TranslationEngineName engine) => string.Empty;
            public void SetApiKey(TranslationEngineName engine, string apiKey) { }
            public void SetRegion(TranslationEngineName engine, string region) { }
            public void SetModel(TranslationEngineName engine, string model) { }
            public void SetEndpoint(TranslationEngineName engine, string endpoint) { }
            public void SetEngineEnabled(TranslationEngineName engine, bool isEnabled) { }
            public void Save() { }
        }
    }
}
