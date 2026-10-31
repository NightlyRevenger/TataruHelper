using Newtonsoft.Json.Linq;

using NUnit.Framework;

using Translation.Providers.DeepL;

namespace Translation.Tests.Providers
{
    [TestFixture]
    public class DeepLXTests
    {
        [Test]
        public void TheRequest_CarriesTextAndUpperCaseLanguages()
        {
            var body = JObject.Parse(DeepLXClient.BuildRequestBody("Hello", "en", "ru"));

            Assert.That((string)body["text"], Is.EqualTo("Hello"));
            Assert.That((string)body["source_lang"], Is.EqualTo("EN"));
            Assert.That((string)body["target_lang"], Is.EqualTo("RU"));
        }

        [Test]
        public void NoSourceLanguage_IsLeftToDeepL()
        {
            var body = JObject.Parse(DeepLXClient.BuildRequestBody("Hello", null, "de"));

            Assert.That((string)body["source_lang"], Is.EqualTo("auto"));
        }

        [Test]
        public void TheTranslation_IsReadFromData()
        {
            Assert.That(DeepLXClient.ParseResponse("{\"code\":200,\"data\":\"Привет\"}"), Is.EqualTo("Привет"));
        }

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{\"code\":503,\"message\":\"busy\"}")]
        [TestCase("{\"data\":\"  \"}")]
        public void AnAnswerWithoutATranslation_GivesNull(string body)
        {
            Assert.That(DeepLXClient.ParseResponse(body), Is.Null);
        }
    }
}
