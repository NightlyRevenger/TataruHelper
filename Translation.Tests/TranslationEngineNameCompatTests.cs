using System;

using Newtonsoft.Json;

using NUnit.Framework;

using Translation.Models;

namespace Translation.Tests
{
    [TestFixture]
    public class TranslationEngineNameCompatTests
    {
        private sealed class SettingsWithEngine
        {
            public TranslationEngineName CurrentTranslationEngine { get; set; }
        }

        [Test]
        public void Deserialize_RestoredBingValue_KeepsTheSameEngine()
        {
            var settings = JsonConvert.DeserializeObject<SettingsWithEngine>("{\"CurrentTranslationEngine\":21}");
            Assert.That(settings.CurrentTranslationEngine, Is.EqualTo(TranslationEngineName.Bing));
        }

        [Test]
        public void Deserialize_RemovedEngineValue_DoesNotThrow()
        {
            // 1 (Multillect), 2 (DeepL), 4 (Amazon), 6 (Baidu) were removed
            // from the enum but may still exist in persisted user settings.
            foreach (var removedValue in new[] { 1, 2, 4, 6 })
            {
                var json = "{\"CurrentTranslationEngine\": " + removedValue + "}";

                SettingsWithEngine settings = null;
                Assert.That(
                    () => settings = JsonConvert.DeserializeObject<SettingsWithEngine>(json),
                    Throws.Nothing);

                Assert.That((int)settings.CurrentTranslationEngine, Is.EqualTo(removedValue));
                Assert.That(
                    Enum.IsDefined(typeof(TranslationEngineName), settings.CurrentTranslationEngine),
                    Is.False);
            }
        }
    }
}