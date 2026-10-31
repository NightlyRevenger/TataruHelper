using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

using Translation.Models;
using Translation.Settings;

namespace Translation.Tests
{
    /// <summary>
    /// A line that arrives twice before the first answer is back - from the chat
    /// log and the dialogue window, or a cutscene repeating itself - is asked of
    /// the service once. A free endpoint counts requests, not sentences.
    /// </summary>
    [TestFixture]
    public class SameLineInFlightTests
    {
        [Test]
        public async Task TwoIdenticalRequestsAtOnce_ReachTheEngineOnce()
        {
            var gate = new TaskCompletionSource<bool>();
            var provider = new SlowProvider(gate.Task, "Воин Света приближается.");
            var translator = Create(provider);

            var first = Ask(translator, "The Warrior of Light draws near.");
            var second = Ask(translator, "The Warrior of Light draws near.");

            gate.SetResult(true);
            var results = await Task.WhenAll(first, second);

            Assert.That(provider.CallCount, Is.EqualTo(1));
            Assert.That(results.All(r => r.IsSuccess && r.Text == "Воин Света приближается."));
        }

        [Test]
        public async Task AnEngineHandingTheSentenceBack_IsTreatedAsFailure_AndAnotherEngineStandsIn()
        {
            var echo = new SlowProvider(Task.CompletedTask, "The Warrior of Light draws near.");
            var good = new SlowProvider(Task.CompletedTask, "Воин Света приближается.", TranslationEngineName.Ollama);
            var translator = Create(echo, good);

            var result = await Ask(translator, "The Warrior of Light draws near.");

            Assert.That(good.CallCount, Is.EqualTo(1));
            Assert.That(result.Text, Is.EqualTo("Воин Света приближается."));
        }

        [Test]
        public async Task AnEchoNobodyImproves_StillStands()
        {
            var echo = new SlowProvider(Task.CompletedTask, "The Warrior of Light draws near.");
            var translator = Create(echo);

            var result = await Ask(translator, "The Warrior of Light draws near.");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Text, Is.EqualTo("The Warrior of Light draws near."));
        }

        private static WebTranslator Create(params SlowProvider[] providers)
        {
            var translator = new WebTranslator(
                NullLogger.Instance,
                providers.Cast<ITranslationProvider>().ToArray(),
                new TranslationSettings());
            translator.LoadLanguages();
            return translator;
        }

        private static Task<TranslationResult> Ask(WebTranslator translator, string sentence)
            => translator.TranslateAsync(
                sentence,
                new TranslationEngine(TranslationEngineName.GoogleTranslate, new List<TranslatorLanguage>(), 9),
                new TranslatorLanguage("English", "English", "en"),
                new TranslatorLanguage("Russian", "Russian", "ru"));

        private sealed class SlowProvider : ITranslationProvider
        {
            private readonly Task _gate;
            private readonly string _response;
            private int _calls;

            public TranslationEngineName EngineName { get; }
            public int CallCount => _calls;

            public SlowProvider(Task gate, string response,
                TranslationEngineName engine = TranslationEngineName.GoogleTranslate)
            {
                _gate = gate;
                _response = response;
                EngineName = engine;
            }

            public async Task<string> TranslateAsync(string sentence, string inLang, string outLang,
                CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _calls);
                await _gate.ConfigureAwait(false);
                return _response;
            }
        }
    }
}
