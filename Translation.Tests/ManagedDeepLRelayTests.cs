using NUnit.Framework;
using Translation.Credentials;
using Translation.Models;
using Translation.Settings;

namespace Translation.Tests
{
    public class ManagedDeepLRelayTests
    {
        [Test]
        public void LauncherConnectsRelayAndPreservesOptionalBrowserAndEngineToggle()
        {
            var store = new Store { Model = "browser" };
            Assert.That(ManagedDeepLRelay.Configure(store, "http://127.0.0.1:2285"), Is.True);
            Assert.That(store.Endpoint, Is.EqualTo("http://127.0.0.1:2285"));
            Assert.That(store.Model, Is.EqualTo("browser"));
            Assert.That(store.Saved, Is.True);
            Assert.That(store.IsEngineEnabled(TranslationEngineName.DeepL), Is.False);
        }

        [Test]
        public void ExplicitCustomServerIsPreserved()
        {
            var store = new Store { Endpoint = "https://my-server.example/translate", Model = "browser" };
            Assert.That(ManagedDeepLRelay.Configure(store, "http://127.0.0.1:2285"), Is.False);
            Assert.That(store.Endpoint, Is.EqualTo("https://my-server.example/translate"));
            Assert.That(store.Model, Is.EqualTo("browser"));
            Assert.That(store.Saved, Is.False);
        }

        [Test]
        public void ForgetClearsOnlyTheLaunchersOwnAddress()
        {
            var own = new Store { Endpoint = "http://127.0.0.1:2285/" };
            Assert.That(ManagedDeepLRelay.Forget(own, "http://127.0.0.1:2285"), Is.True);
            Assert.That(own.Endpoint, Is.Empty);
            Assert.That(own.Saved, Is.True);

            var custom = new Store { Endpoint = "https://my-server.example/translate" };
            Assert.That(ManagedDeepLRelay.Forget(custom, "http://127.0.0.1:2285"), Is.False);
            Assert.That(custom.Endpoint, Is.EqualTo("https://my-server.example/translate"));
            Assert.That(custom.Saved, Is.False);

            var empty = new Store();
            Assert.That(ManagedDeepLRelay.Forget(empty, "http://127.0.0.1:2285"), Is.False);
            Assert.That(empty.Saved, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not a URL")]
        [TestCase("http://remote.example:2285")]
        [TestCase("file:///tmp/relay")]
        [TestCase("http://user:password@127.0.0.1:2285")]
        public void MissingOrInvalidLauncherAddressDoesNotChangeSettings(string address)
        {
            var store = new Store { Model = "browser" };
            Assert.That(ManagedDeepLRelay.Configure(store, address), Is.False);
            Assert.That(store.Model, Is.EqualTo("browser"));
            Assert.That(store.Saved, Is.False);
        }

        private sealed class Store : ITranslationCredentialStore
        {
            public string Endpoint = "";
            public string Model = "";
            public bool Saved;
            public string GetApiKey(TranslationEngineName engine) => "";
            public string GetRegion(TranslationEngineName engine) => "";
            public string GetModel(TranslationEngineName engine) => Model;
            public string GetEndpoint(TranslationEngineName engine) => Endpoint;
            public bool IsEngineEnabled(TranslationEngineName engine) => false;
            public void SetApiKey(TranslationEngineName engine, string value) { }
            public void SetRegion(TranslationEngineName engine, string value) { }
            public void SetModel(TranslationEngineName engine, string value) => Model = value;
            public void SetEndpoint(TranslationEngineName engine, string value) => Endpoint = value;
            public void SetEngineEnabled(TranslationEngineName engine, bool value) => Assert.Fail("Engine toggle must be preserved");
            public void Save() => Saved = true;
        }
    }
}
