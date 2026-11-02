using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Translation.Credentials;
using Translation.Models;
using Translation.Providers.DeepL;
using Translation.Settings;

namespace Translation.Tests.Providers
{
    /// <summary>
    /// Since 2 November 2026 DeepL's free web endpoint answers HTTP 200 with
    /// "You're using an older version of DeepL…" in place of a translation,
    /// the same blob in fifteen languages whatever was asked for. A status of
    /// 200 reads as a translation to everything downstream: the junk reached
    /// the player's screen and the dialogue window took over DeepL, which had
    /// refused. Whatever reads DeepL's answer has to recognize the notice as
    /// the refusal it is.
    /// </summary>
    [TestFixture]
    public class DeepLVersionNoticeTests
    {
        /// <summary>
        /// Reproduced live through the relay on 2 November 2026, identical for
        /// every target language. The relay keeps the same markers in
        /// tools/deepl-relay/relay.go.
        /// </summary>
        private const string Notice = """
            You're using an older version of DeepL. To continue using the app, please update to the latest version.

            Sie verwenden eine ältere Version von DeepL. Bitte aktualisieren Sie die App, um sie weiterhin nutzen zu können.

            Вы используете старую версию DeepL. Чтобы продолжить работу с приложением, обновите его до последней версии.

            Vous utilisez une ancienne version de DeepL. Veuillez mettre à jour l'application pour continuer à l'utiliser.

            您正在使用旧版本的 DeepL。 如需继续使用该应用程序，请更新至最新版本。

            DeepLの古いバージョンをご利用中です。 引き続きご利用いただくには、アプリを最新版へアップデートしてください。

            Estás usando una versión antigua de DeepL. Para seguir usando la aplicación, actualízala a la versión más reciente.

            Ти використовуєш стару версію DeepL. Щоб продовжити користуватися програмою, онови її до останньої версії.

            Stai utilizzando una versione precedente di DeepL. Per continuare a utilizzare l'app, aggiornala all'ultima versione.

            DeepL'in eski bir sürümünü kullanıyorsun. Uygulamayı kullanmaya devam etmek için en son sürüme güncelle.

            Korzystasz ze starszej wersji DeepL. Jeżeli chcesz kontynuować korzystanie z aplikacji, zaktualizuj ją do najnowszej wersji.

            Está a utilizar uma versão mais antiga do DeepL. Para continuar a utilizar a aplicação, atualize-a para a versão mais recente.

            U gebruikt een oudere versie van DeepL. Om de app te blijven gebruiken, moet u updaten naar de nieuwste versie.

            Anda menggunakan DeepL versi lama. Untuk terus menggunakan aplikasi ini, harap perbarui ke versi terbaru.

            Používáte starší verzi DeepL. Pokud chcete aplikaci dále používat, aktualizujte ji na nejnovější verzi.
            """;

        [Test]
        public void TheBlob_IsRecognized()
        {
            Assert.That(DeepLVersionNotice.IsPresent(Notice), Is.True);
        }

        /// <summary>
        /// Each language on its own: the blob carries all fifteen today, but a
        /// reply limited to the asked-for language must be refused too.
        /// </summary>
        [Test]
        public void EachLanguageOfTheNotice_IsRecognized()
        {
            foreach (var line in Notice.Split("\n\n"))
            {
                Assert.That(DeepLVersionNotice.IsPresent(line), Is.True, line);
            }
        }

        [TestCase("Hello, friend")]
        [TestCase("Привет, друг")]
        [TestCase("こんにちは世界")]
        [TestCase("Haritanın eski bir sürümünü buldum")]
        [TestCase("")]
        [TestCase(null)]
        public void AnOrdinaryTranslation_IsNotTheNotice(string text)
        {
            Assert.That(DeepLVersionNotice.IsPresent(text), Is.False);
        }

        [Test]
        public async Task RelayAnsweringWithTheNotice_TranslatesNothing()
        {
            var translator = new DeepLTranslator(null, new TranslationSettings(),
                new Store { Endpoint = "http://127.0.0.1:2285" },
                (endpoint, text, source, target, token) => Task.FromResult(Notice),
                (text, source, target, token) => Task.FromResult("unused"),
                warmBrowser: false);

            var result = await translator.TranslateAsync("Hello, friend", "auto", "ru", CancellationToken.None);

            Assert.That(result, Is.Empty, "the notice must not reach the screen as a translation");
        }

        [Test]
        public async Task RelayAnsweringNormally_KeepsItsTranslation()
        {
            var translator = new DeepLTranslator(null, new TranslationSettings(),
                new Store { Endpoint = "http://127.0.0.1:2285" },
                (endpoint, text, source, target, token) => Task.FromResult("Привет, друг"),
                (text, source, target, token) => Task.FromResult("unused"),
                warmBrowser: false);

            var result = await translator.TranslateAsync("Hello, friend", "auto", "ru", CancellationToken.None);

            Assert.That(result, Is.EqualTo("Привет, друг"));
        }

        [Test]
        public async Task BrowserAnsweringWithTheNotice_TranslatesNothing()
        {
            var translator = new DeepLTranslator(null, new TranslationSettings(),
                new Store { Model = DeepLTranslator.BrowserModel },
                (endpoint, text, source, target, token) => Task.FromResult<string>(null),
                (text, source, target, token) => Task.FromResult(Notice),
                warmBrowser: false);

            var result = await translator.TranslateAsync("Hello, friend", "auto", "ru", CancellationToken.None);

            Assert.That(result, Is.Empty);
        }

        private sealed class Store : ITranslationCredentialStore
        {
            public string Endpoint = "";
            public string Model = "";
            public string GetApiKey(TranslationEngineName engine) => "";
            public string GetRegion(TranslationEngineName engine) => "";
            public string GetModel(TranslationEngineName engine) => Model;
            public string GetEndpoint(TranslationEngineName engine) => Endpoint;
            public bool IsEngineEnabled(TranslationEngineName engine) => true;
            public void SetApiKey(TranslationEngineName engine, string value) { }
            public void SetRegion(TranslationEngineName engine, string value) { }
            public void SetModel(TranslationEngineName engine, string value) => Model = value;
            public void SetEndpoint(TranslationEngineName engine, string value) => Endpoint = value;
            public void SetEngineEnabled(TranslationEngineName engine, bool value) { }
            public void Save() { }
        }
    }
}
