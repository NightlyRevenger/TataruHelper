using System;

using NUnit.Framework;

using Translation.Providers.Google;

namespace Translation.Tests.Providers
{
    [TestFixture]
    public class GoogleSorryTests
    {
        [Test]
        public void ARedirectToTheSorryPage_IsRecognised()
        {
            var uri = new Uri("https://www.google.com/sorry/index?continue=https://translate.google.com/m");

            Assert.That(GoogleTranslator.IsSorryPage(uri, string.Empty), Is.True);
        }

        [Test]
        public void TheSorryTextServedWithAnOrdinaryAddress_IsRecognised()
        {
            var body = "<html><body>Our systems have detected unusual traffic from your computer network.</body></html>";

            Assert.That(GoogleTranslator.IsSorryPage(new Uri("https://translate.google.com/m"), body), Is.True);
        }

        [Test]
        public void AnOrdinaryTranslation_IsNotTakenForIt()
        {
            var uri = new Uri("https://translate.google.com/m?q=traffic");

            Assert.That(GoogleTranslator.IsSorryPage(uri, "<div class=\"result-container\">heavy unusual traffic</div>"),
                Is.False);
            Assert.That(GoogleTranslator.IsSorryPage(uri, "[[[\"unusual traffic\",\"x\"]]]"), Is.False);
            Assert.That(GoogleTranslator.IsSorryPage(uri,
                "<html><textarea>unusual traffic</textarea><div class=\"result-container\">необычный трафик</div></html>"),
                Is.False);
            Assert.That(GoogleTranslator.IsSorryPage(null, null), Is.False);
            Assert.That(GoogleTranslator.IsSorryPage(uri, string.Empty), Is.False);
        }
    }
}
