using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Translation.Providers.Bing;

namespace Translation.Tests.Providers
{
    public class BingLiveTests
    {
        [Test, Explicit("Sends four real requests to Bing")]
        public async Task TheRealProviderTranslatesWithAReusedSession()
        {
            var provider = new BingTranslator(NullLogger.Instance);
            foreach (var item in new[] {
                ("Good evening, traveler.", "en", "ru"),
                ("Where is the healer?", "auto", "ru"),
                ("ありがとうございます。", "ja", "en"),
                ("Bonjour mon ami.", "fr", "ru") })
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var watch = Stopwatch.StartNew();
                var answer = await provider.TranslateAsync(item.Item1, item.Item2, item.Item3, timeout.Token);
                TestContext.Out.WriteLine(item.Item2 + "→" + item.Item3 + " " + watch.ElapsedMilliseconds + "ms " + answer);
                Assert.That(answer, Is.Not.Null.And.Not.Empty);
                Assert.That(answer, Is.Not.EqualTo(item.Item1));
            }
        }
    }
}
