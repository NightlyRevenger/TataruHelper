using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NUnit.Framework;
using Translation.Models;
using Translation.Providers.Web;

namespace Translation.Tests.Providers
{
    /// <summary>Repeatable real-service probe; never part of the offline test run.</summary>
    public class WebCandidatesLiveTests
    {
        [TestCase(TranslationEngineName.TranslateCom)]
        [TestCase(TranslationEngineName.ModernMT)]
        [TestCase(TranslationEngineName.Tencent)]
        [TestCase(TranslationEngineName.Volcengine)]
        [TestCase(TranslationEngineName.Alibaba)]
        [Explicit("Sends real translations to the selected web service")]
        public async Task DialogueAndLanguagePairsTranslate(TranslationEngineName engine)
        {
            WebSessionTranslator provider = engine switch
            {
                TranslationEngineName.TranslateCom => new TranslateComTranslator(NullLogger.Instance),
                TranslationEngineName.ModernMT => new ModernMtTranslator(NullLogger.Instance),
                TranslationEngineName.Tencent => new TencentTranslator(NullLogger.Instance),
                TranslationEngineName.Volcengine => new VolcengineTranslator(NullLogger.Instance),
                _ => new AlibabaTranslator(NullLogger.Instance)
            };
            var rows = new List<object>();
            var samples = new[]
            {
                ("Go on, then. I'll hold them off.", "en", "ru"),
                ("Aye, that's the one. Take it to the quartermaster.", "en", "ru"),
                ("Hold the line! Do not let them breach the gates!", "en", "ru"),
                ("We shall not fall, not while there is breath in our lungs.", "en", "ru"),
                ("Have you no shame? After all they sacrificed for you?", "en", "ru"),
                ("Is this the way to the Crystarium? I seem to have lost my bearings.", "en", "ru"),
                ("先に行け。ここは俺が食い止める。", "ja", "ru"),
                ("約束だ。必ず生きて帰ってこい。", "ja", "ru"),
                ("Where is the healer?", "auto", "ru"),
                ("Здравствуйте, друг.", "ru", "en"),
                ("Where is the healer?", "en", "ja"),
                ("Спасибо за помощь.", "ru", "ja"),
                ("ありがとうございます。", "ja", "en")
            };
            foreach (var (text, source, target) in samples)
            {
                var watch = Stopwatch.StartNew();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                var answer = await provider.TranslateAsync(text, source, target, timeout.Token);
                rows.Add(new { Engine = engine.ToString(), Source = text, SourceLang = source, TargetLang = target,
                    Text = answer, Ms = watch.ElapsedMilliseconds });
                TestContext.Out.WriteLine(source + "→" + target + " " + watch.ElapsedMilliseconds + "ms " + answer);
                Assert.That(answer, Is.Not.Null.And.Not.Empty);
                await Task.Delay(250);
            }
            var directory = Environment.GetEnvironmentVariable("WEB_CANDIDATES_PROBE_OUT");
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, engine + ".json"), JsonConvert.SerializeObject(rows, Formatting.Indented));
            }
        }
    }
}
