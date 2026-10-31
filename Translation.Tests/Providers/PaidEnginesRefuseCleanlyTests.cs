using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

using Translation.Credentials;
using Translation.Models;
using Translation.Providers;
using Translation.Settings;

namespace Translation.Tests.Providers
{
    /// <summary>
    /// What a player sees when an engine that needs a key is picked without one,
    /// or with one the service does not accept: how long it takes, and what is
    /// said. Nothing here has a real key; the junk one only ever reaches the
    /// vendor as a rejected credential.
    ///
    /// Explicit because it goes out to nine vendors' servers. Prints a table:
    ///
    ///   dotnet test Translation.Tests --filter FullyQualifiedName~PaidEnginesRefuseCleanly
    /// </summary>
    [TestFixture, Explicit]
    public class PaidEnginesRefuseCleanlyTests
    {
        private static readonly TranslationEngineName[] Paid =
        {
            TranslationEngineName.AzureTranslator,
            TranslationEngineName.GoogleCloudTranslate,
            TranslationEngineName.DeepLApi,
            TranslationEngineName.Yandex,
            TranslationEngineName.YandexGPT,
            TranslationEngineName.OpenAI,
            TranslationEngineName.DeepSeek,
            TranslationEngineName.Gemini,
            TranslationEngineName.OpenRouter,
            TranslationEngineName.Claude,
        };

        private sealed class JunkKeyStore : ITranslationCredentialStore
        {
            private readonly bool _withKey;

            public JunkKeyStore(bool withKey)
            {
                _withKey = withKey;
            }

            public string GetApiKey(TranslationEngineName engine) => _withKey ? "junk-key-for-testing:fx" : string.Empty;
            public string GetRegion(TranslationEngineName engine) => _withKey ? "westeurope" : string.Empty;
            public string GetModel(TranslationEngineName engine) => string.Empty;
            public string GetEndpoint(TranslationEngineName engine) => string.Empty;
            public bool IsEngineEnabled(TranslationEngineName engine) => true;
            public void SetApiKey(TranslationEngineName engine, string apiKey) { }
            public void SetRegion(TranslationEngineName engine, string region) { }
            public void SetModel(TranslationEngineName engine, string model) { }
            public void SetEndpoint(TranslationEngineName engine, string endpoint) { }
            public void SetEngineEnabled(TranslationEngineName engine, bool isEnabled) { }
            public void Save() { }
        }

        [Test]
        public async Task EveryEngine_SaysNoQuicklyAndInWords()
        {
            var slow = new List<string>();
            var silent = new List<string>();

            foreach (var withKey in new[] { false, true })
            {
                var providers = TranslationProviderFactory.CreateDefaultProviders(
                    NullLogger.Instance, new JunkKeyStore(withKey), new TranslationSettings());

                foreach (var engine in Paid)
                {
                    var label = $"{engine,-22} {(withKey ? "junk key" : "no key  ")}";
                    var sw = Stopwatch.StartNew();
                    string text = null;
                    Exception error = null;
                    try
                    {
                        text = await providers[engine].TranslateAsync("The wood is watching.",
                            "en", engine == TranslationEngineName.DeepLApi ? "RU" : "ru",
                            CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }

                    var what = error != null
                        ? $"threw {error.GetType().Name}: {error.Message}"
                        : string.IsNullOrEmpty(text) ? "returned nothing (no explanation)" : "RETURNED: " + text;
                    TestContext.Progress.WriteLine($"{label} {sw.ElapsedMilliseconds,6}ms  {what}");

                    if (sw.ElapsedMilliseconds > 20000)
                        slow.Add(label.Trim());
                    if (error == null)
                        silent.Add(label.Trim());
                }
            }

            TestContext.Progress.WriteLine("SILENT (no exception): " + string.Join("; ", silent));
            Assert.That(slow, Is.Empty, "took more than 20 s to refuse");
        }
    }
}
