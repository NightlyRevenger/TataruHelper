using NUnit.Framework;

using Translation.Providers.Google;

namespace Translation.Tests.Providers
{
    [TestFixture]
    public class GoogleWidgetTests
    {
        [Test]
        public void TheLoaderScriptAddress_IsUnescaped()
        {
            var loader = @"x;_loadJs('https:\/\/translate.googleapis.com\/_\/js\/k\x3dtr.O\/am\x3dAAC\/m\x3del_main');y";

            Assert.That(GoogleWidgetKey.ExtractScriptUrl(loader),
                Is.EqualTo("https://translate.googleapis.com/_/js/k=tr.O/am=AAC/m=el_main"));
        }

        [Test]
        public void ANonHttpsAddress_IsNotFollowed()
        {
            Assert.That(GoogleWidgetKey.ExtractScriptUrl("_loadJs('http:\\/\\/x\\/a.js')"), Is.Null);
            Assert.That(GoogleWidgetKey.ExtractScriptUrl(""), Is.Null);
        }

        [Test]
        public void TheKeyBesideTranslateHtml_IsTheOneTaken_NotAnotherInTheScript()
        {
            var script = "a=\"AIzaSyBWDj0QJvVIx8XOhRegXX5_SrRWxhT5Hs4\";" +
                         "d={host:b,path:\"/v1/translateHtml\",method:\"POST\",headers:" +
                         "{\"X-goog-api-key\":\"AIzaSyATBXajvzQLTDHEQbcpq0Ihe0vWDHmO520\"," +
                         "\"Content-Type\":\"application/json+protobuf\"}}";

            Assert.That(GoogleWidgetKey.ExtractKey(script),
                Is.EqualTo("AIzaSyATBXajvzQLTDHEQbcpq0Ihe0vWDHmO520"));
            Assert.That(GoogleWidgetKey.ExtractKey("nothing here"), Is.Null);
        }

        [Test]
        public void TheTranslatedText_IsUnescaped()
        {
            Assert.That(GoogleTranslator.ParseWidgetTranslation("[[\"Tom &amp; Jerry\"]]"),
                Is.EqualTo("Tom & Jerry"));
            Assert.That(GoogleTranslator.ParseWidgetTranslation("[7,\"no permission\"]"), Is.Empty);
            Assert.That(GoogleTranslator.ParseWidgetTranslation(""), Is.Empty);
        }
    }
}
