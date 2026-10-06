using FFXIVTataruHelper.Services.Settings;

using NUnit.Framework;

using System;
using System.IO;

using Translation.Models;

namespace TataruHelper.Tests
{
    /// <summary>
    /// What is pasted into a key or model field brings along whatever was
    /// around it. Reported 2026-10-06: a model name typed into the OpenAI
    /// field failed for every model tried, while the default model worked -
    /// which is what a stray space after the name does, since the service
    /// then looks for a model whose name ends in one.
    /// </summary>
    public class CredentialStoreTrimTests
    {
        private static DpapiCredentialStore NewStore()
        {
            var directory = Path.Combine(
                Path.GetTempPath(), "tataru-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return new DpapiCredentialStore(directory);
        }

        [TestCase("gpt-6-luna ")]
        [TestCase(" gpt-6-luna")]
        [TestCase("gpt-6-luna\r\n")]
        [TestCase("\tgpt-6-luna\t")]
        public void AModelName_LosesWhatWasPastedAroundIt(string typed)
        {
            var sut = NewStore();

            sut.SetModel(TranslationEngineName.OpenAI, typed);

            Assert.That(sut.GetModel(TranslationEngineName.OpenAI), Is.EqualTo("gpt-6-luna"));
        }

        [Test]
        public void AKey_LosesWhatWasPastedAroundIt()
        {
            var sut = NewStore();

            sut.SetApiKey(TranslationEngineName.OpenAI, "  sk-test-123\n");

            Assert.That(sut.GetApiKey(TranslationEngineName.OpenAI), Is.EqualTo("sk-test-123"));
        }

        /// <summary>
        /// A field cleared down to spaces is a field cleared: the engine goes
        /// back to its own default model rather than asking for one named " ".
        /// </summary>
        [Test]
        public void OnlySpaces_IsTheSameAsNothing()
        {
            var sut = NewStore();
            sut.SetModel(TranslationEngineName.OpenAI, "gpt-6-luna");

            sut.SetModel(TranslationEngineName.OpenAI, "   ");

            Assert.That(sut.GetModel(TranslationEngineName.OpenAI), Is.Empty);
        }
    }
}
