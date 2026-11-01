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
    public class FreeFallbackOrderTests
    {
        [Test]
        public async Task FastDialogueReservesFollowTheEstablishedEngines()
        {
            var calls = new List<TranslationEngineName>();
            var providers = new List<ITranslationProvider>();
            foreach (var engine in new[] { TranslationEngineName.DeepL, TranslationEngineName.YandexFree,
                TranslationEngineName.GoogleTranslate, TranslationEngineName.TranslateCom, TranslationEngineName.Bing,
                TranslationEngineName.ModernMT, TranslationEngineName.Papago, TranslationEngineName.Tencent,
                TranslationEngineName.Volcengine, TranslationEngineName.Alibaba })
                providers.Add(new Provider(engine, calls));
            var translator = new WebTranslator(NullLogger.Instance, providers, new TranslationSettings(),
                credentials: NullCredentialStore.Instance);
            translator.LoadLanguages();
            var result = await translator.TranslateAsync("The Warrior of Light draws near.",
                new TranslationEngine(TranslationEngineName.DeepL, new List<TranslatorLanguage>(), 10),
                new TranslatorLanguage("English", "English", "en"),
                new TranslatorLanguage("Russian", "Russian", "ru"));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(calls, Is.EqualTo(new[] { TranslationEngineName.DeepL, TranslationEngineName.YandexFree,
                TranslationEngineName.GoogleTranslate, TranslationEngineName.TranslateCom, TranslationEngineName.Bing,
                TranslationEngineName.ModernMT }));
        }
        private sealed class Provider : ITranslationProvider
        {
            public TranslationEngineName EngineName { get; }
            private readonly List<TranslationEngineName> _calls;
            public Provider(TranslationEngineName engine, List<TranslationEngineName> calls) { EngineName = engine; _calls = calls; }
            public Task<string> TranslateAsync(string text, string source, string target, CancellationToken token)
            {
                _calls.Add(EngineName);
                return Task.FromResult(EngineName == TranslationEngineName.ModernMT ? "Перевод реплики" : "");
            }
        }
    }
}
