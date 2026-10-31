using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Translation.Providers.DeepL.Browser;

namespace Translation.Tests.Providers
{
    [TestFixture]
    public class DeepLBrowserLiveTests
    {
        // Needs a Chromium browser already open on the DeepL translator with --remote-debugging-port=9333.
        [Test, Explicit("Drives a real browser")]
        public async Task ABrowserOnDeepL_Translates()
        {
            using (var bridge = new DeepLBrowserBridge())
            {
                bridge.UsePort(9333);

                var first = await bridge.TranslateAsync("Where is the healer?", "en", "ru", CancellationToken.None);
                var second = await bridge.TranslateAsync("Thank you very much", "en", "de", CancellationToken.None);
                var third = await bridge.TranslateAsync("Привет, как дела?", "ru", "en", CancellationToken.None);

                TestContext.Out.WriteLine("RESULT " + first + " | " + second + " | " + third);
                Assert.That(first, Is.Not.Empty);
                Assert.That(second, Does.Contain("Dank"));
                Assert.That(third.ToLowerInvariant(), Does.Contain("hello").Or.Contain("hi"));
            }
        }
    }
}
