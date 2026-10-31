using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NUnit.Framework;

using Translation.Credentials;
using Translation.Models;
using Translation.Providers.DeepL;
using Translation.Providers.Google;
using Translation.Providers.LibreTranslate;
using Translation.Providers.Papago;
using Translation.Providers.Yandex;
using Translation.Settings;

namespace Translation.Tests.Providers
{
    /// <summary>
    /// Sends a realistic stream of game lines to one keyless engine and records
    /// when, and how, it stops answering.
    ///
    /// The providers swallow most failures and hand back an empty string, so the
    /// line itself says little; the log lines they write ([PAPAGO_HTTP_429] and
    /// the like) are captured next to each result for that reason.
    ///
    /// Explicit because it goes out to the network for minutes at a time. Run
    /// one engine at a time (different engines in parallel is fine):
    ///
    ///   dotnet test Translation.Tests --filter FullyQualifiedName~ProviderEndurance
    ///     -e PROBE_ENGINE=Papago -e PROBE_PATTERN=steady -e PROBE_COUNT=20
    ///
    /// PROBE_ENGINE   GoogleTranslate | YandexFree | DeepL | Papago | LibreTranslate
    /// PROBE_PATTERN  steady (a line every 3 s), burst (6 at once every 10 s),
    ///                soak (a line every 10 s for PROBE_COUNT lines)
    /// PROBE_COUNT    lines, or series for burst
    /// PROBE_INTERVAL_MS  overrides the pause between lines/series
    /// PROBE_OUT      directory for the CSV and the running log
    /// </summary>
    [TestFixture, Explicit]
    public class ProviderEnduranceTests
    {
        private static readonly string[] Corpus =
        {
            "The wood is watching.",
            "Welcome back, <Forename>. We've been expecting you.",
            "I never thought I would see the day when the Scions of the Seventh Dawn would stand before me.",
            "Fear not, for the light of the Crystal shall guide us.",
            "Aye, that's the one. Take it to the quartermaster.",
            "You there! Halt!",
            "Hmph. So the rumors were true.",
            "Alisaie, we must hurry. The Warriors of Darkness will not wait for us.",
            "It is not a matter of strength, but of resolve.",
            "Go on, then. I'll hold them off.",
            "Thancred, did you find anything in the archives?",
            "Nothing yet, but I have a lead in Ul'dah.",
            "The Source and the First are bound by more than we know.",
            "By the Twelve... what have they done?",
            "Come, adventurer, let us speak somewhere more private.",
            "The Echo stirs. I can feel it.",
            "Don't just stand there gawking! We've work to do.",
            "Is this the way to the Crystarium? I seem to have lost my bearings.",
            "My lady, the kingdom has need of your wisdom once more.",
            "We shall not fall, not while there is breath in our lungs.",
            "Mother, I'm home!",
            "Please, take this potion. You'll need it where you're going.",
            "A hero's work is never done, is it?",
            "The ancient seal has been broken. Darkness spreads across the land.",
            "Yes, yes, I understand. But what of the aetheryte?",
            "Thank you.",
            "No!",
            "Well met, friend. May fortune smile upon your journey.",
            "Ahh, so you've finally decided to show your face, Garlean dog.",
            "The Dawn will come. It always does.",
            "Deliver this letter to Master Gerolt in Mor Dhona and wait for his reply.",
            "Quick, to the airship! There's no time to lose!",
            "I have heard tales of your deeds from Eorzea to the farthest reaches of Othard.",
            "Have you no shame? After all they sacrificed for you?",
            "Hold the line! Do not let them breach the gates!",
            "What a curious little thing you are. Where did you come from?",
            "Our research suggests the creature is vulnerable to lightning-aspected magicks.",
            "If you would be so kind as to follow me, the Elder Seedseer will see you now.",
            "Ha ha ha! You think you can stop me? You are but a fool!",
            "Rest well, my friend. Tomorrow we ride at dawn.",
        };

        private sealed class CapturingLogger : ILogger
        {
            private readonly List<string> _lines = new List<string>();

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                var text = formatter(state, exception);
                if (exception != null)
                {
                    text += " " + exception.GetType().Name + ": " + exception.Message;
                }

                lock (_lines)
                {
                    _lines.Add(text);
                }
            }

            public string Drain()
            {
                lock (_lines)
                {
                    var joined = string.Join(" | ", _lines.Select(x => x.Replace('\n', ' ').Replace('\r', ' ')));
                    _lines.Clear();
                    return joined.Length > 240 ? joined.Substring(0, 240) : joined;
                }
            }
        }

        private sealed class Row
        {
            public int N;
            public double AtSeconds;
            public double LatencyMs;
            public string Outcome;
            public string Answer;
            public string Log;
        }

        private static ITranslationProvider Create(string engine, ILogger logger)
        {
            var settings = new TranslationSettings();
            switch (engine)
            {
                case "GoogleTranslate": return new GoogleTranslator(logger, settings);
                case "YandexFree": return new YandexFreeTranslator(logger);
                case "DeepL": return new DeepLTranslator(logger, settings);
                case "Papago": return new PapagoTranslator(logger, settings);
                case "LibreTranslate":
                    return new LibreTranslateTranslator(logger, NullCredentialStore.Instance, settings);
                default:
                    throw new ArgumentException("Unknown PROBE_ENGINE " + engine);
            }
        }

        private static int Env(string name, int fallback)
        {
            return int.TryParse(Environment.GetEnvironmentVariable(name), out var v) ? v : fallback;
        }

        [Test]
        public async Task TheEngine_KeepsAnswering()
        {
            var engine = Environment.GetEnvironmentVariable("PROBE_ENGINE");
            if (string.IsNullOrWhiteSpace(engine))
            {
                Assert.Ignore("Set PROBE_ENGINE to an engine name.");
            }

            var pattern = (Environment.GetEnvironmentVariable("PROBE_PATTERN") ?? "steady").ToLowerInvariant();
            var defaultInterval = pattern == "steady" ? 3000 : 10000;
            var defaultCount = pattern == "burst" ? 10 : 100;
            var interval = Env("PROBE_INTERVAL_MS", defaultInterval);
            var count = Env("PROBE_COUNT", defaultCount);
            var burstSize = pattern == "burst" ? Env("PROBE_BURST", 6) : 1;
            var outDir = Environment.GetEnvironmentVariable("PROBE_OUT") ?? Path.GetTempPath();
            Directory.CreateDirectory(outDir);

            var upper = engine == "DeepL";
            var from = upper ? "EN" : "en";
            var to = upper ? "RU" : "ru";

            var logger = new CapturingLogger();
            var provider = Create(engine, logger);
            var rows = new List<Row>();
            var clock = Stopwatch.StartNew();
            var stem = $"{engine}-{pattern}-{DateTime.Now:HHmmss}";
            var live = Path.Combine(outDir, stem + ".log");
            var next = 0;

            async Task<Row> One(int n)
            {
                var sentence = Corpus[n % Corpus.Length];
                var started = clock.Elapsed.TotalSeconds;
                var sw = Stopwatch.StartNew();
                string answer = null;
                string outcome;
                try
                {
                    answer = await provider.TranslateAsync(sentence, from, to, CancellationToken.None)
                        .ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(answer))
                    {
                        outcome = "empty";
                    }
                    else if (string.Equals(answer.Trim(), sentence.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        outcome = "echo";
                    }
                    else if (!answer.Any(c => c >= 'Ѐ' && c <= 'ӿ'))
                    {
                        outcome = "noncyrillic";
                    }
                    else
                    {
                        outcome = "ok";
                    }
                }
                catch (Exception ex)
                {
                    outcome = "threw " + ex.GetType().Name + ": " + ex.Message;
                }

                var row = new Row
                {
                    N = n,
                    AtSeconds = started,
                    LatencyMs = sw.Elapsed.TotalMilliseconds,
                    Outcome = outcome,
                    Answer = answer ?? string.Empty,
                    Log = logger.Drain(),
                };
                lock (rows)
                {
                    rows.Add(row);
                }

                var line = $"{engine} #{n,3} t={started,7:F1}s {row.LatencyMs,6:F0}ms {outcome,-12} {row.Answer}" +
                           (row.Log.Length > 0 ? "  LOG: " + row.Log : string.Empty);
                TestContext.Progress.WriteLine(line);
                File.AppendAllText(live, line + Environment.NewLine);
                return row;
            }

            for (var i = 0; i < count; i++)
            {
                var batch = new List<Task<Row>>();
                for (var k = 0; k < burstSize; k++)
                {
                    batch.Add(One(next++));
                }

                await Task.WhenAll(batch).ConfigureAwait(false);
                if (i < count - 1)
                {
                    await Task.Delay(interval).ConfigureAwait(false);
                }
            }

            rows.Sort((a, b) => a.N.CompareTo(b.N));
            var csv = new StringBuilder("n;at_s;latency_ms;outcome;answer;log\n");
            foreach (var r in rows)
            {
                csv.AppendLine(string.Join(";", r.N, r.AtSeconds.ToString("F1"), r.LatencyMs.ToString("F0"),
                    r.Outcome, "\"" + r.Answer.Replace("\"", "'") + "\"", "\"" + r.Log.Replace("\"", "'") + "\""));
            }

            File.WriteAllText(Path.Combine(outDir, stem + ".csv"), csv.ToString());

            var ok = rows.Count(r => r.Outcome == "ok");
            var lat = rows.Where(r => r.Outcome == "ok").Select(r => r.LatencyMs).OrderBy(x => x).ToList();
            var firstBad = rows.OrderBy(r => r.AtSeconds).FirstOrDefault(r => r.Outcome != "ok");
            var summary = $"SUMMARY {engine} {pattern}: ok {ok}/{rows.Count}" +
                          (lat.Count > 0
                              ? $", p50 {lat[lat.Count / 2]:F0}ms, p95 {lat[(int)(lat.Count * 0.95)]:F0}ms"
                              : string.Empty) +
                          (firstBad != null
                              ? $", first failure #{firstBad.N} at {firstBad.AtSeconds:F0}s ({firstBad.Outcome})"
                              : ", no failures");
            TestContext.Progress.WriteLine(summary);
            File.AppendAllText(live, summary + Environment.NewLine);
        }
    }
}
