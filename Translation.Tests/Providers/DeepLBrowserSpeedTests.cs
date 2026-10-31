using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Translation.Providers.DeepL.Browser;

namespace Translation.Tests.Providers
{
    [TestFixture]
    public class DeepLBrowserSpeedTests
    {
        // Needs a Chromium browser open on the DeepL translator with --remote-debugging-port=9333.
        [Test, Explicit("Drives a real browser")]
        public async Task FastMode_AgainstOrdinary()
        {
            var lines = new[]
            {
                "Where is the healer?", "Are you one of those adventurers, then?", "I remember every adventurer who passed my post.",
                "Thank you very much for your help today.", "The crystal is glowing brighter than before.", "Please wait here a moment.",
            };

            using (var bridge = new DeepLBrowserBridge())
            {
                bridge.UsePort(9333);
                await bridge.TranslateAsync("warm up", "en", "ru", CancellationToken.None);

                foreach (var fast in new[] { false, true, false, true })
                {
                    bridge.FastMode = fast;
                    var watch = Stopwatch.StartNew();
                    var results = string.Empty;
                    foreach (var line in lines)
                    {
                        results += await bridge.TranslateAsync(line + " " + watch.ElapsedTicks, "en", "ru",
                            CancellationToken.None) + " | ";
                    }

                    TestContext.Out.WriteLine($"RESULT fast={fast} per line {watch.ElapsedMilliseconds / lines.Length} ms");
                    TestContext.Out.WriteLine("RESULT   " + results);
                }
            }
        }
    }
}
