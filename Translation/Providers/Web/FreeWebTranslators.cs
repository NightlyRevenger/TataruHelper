using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Translation.Models;

namespace Translation.Providers.Web
{
    // Web protocol references: LunaTranslator translator/{TranslateCom,ModernMt,
    // ali,huoshan,qqimt}.py. These adapters use our own session/error handling.
    internal sealed class TranslateComTranslator : WebSessionTranslator
    {
        public override TranslationEngineName EngineName => TranslationEngineName.TranslateCom;
        private bool _ready;
        public TranslateComTranslator(ILogger logger, HttpClient client = null) : base(logger, client) { }
        protected override void ResetSession() => _ready = false;
        protected override async Task<string> TranslateCoreAsync(string text, string source, string target, CancellationToken token)
        {
            if (!_ready)
            {
                await SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://www.translate.com/machine-translation"), token).ConfigureAwait(false);
                _ready = true;
            }
            if (source == "auto")
            {
                var detect = await SendAsync(Form("https://www.translate.com/translator/ajax_lang_auto_detect",
                    new Dictionary<string, string> { ["text_to_translate"] = text }), token).ConfigureAwait(false);
                source = (string)JObject.Parse(detect)["language"];
                if (string.IsNullOrWhiteSpace(source)) throw new InvalidOperationException("Language detection failed.");
            }
            var body = await SendAsync(Form("https://www.translate.com/translator/translate_mt", new Dictionary<string, string>
            {
                ["text_to_translate"] = text, ["source_lang"] = source, ["translated_lang"] = target, ["use_cache_only"] = "false"
            }), token).ConfigureAwait(false);
            return WebUtility.HtmlDecode((string)JObject.Parse(body)["translated_text"]);
        }
        private static HttpRequestMessage Form(string url, Dictionary<string, string> fields)
            => new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(fields) };
    }

    internal sealed class ModernMtTranslator : WebSessionTranslator
    {
        public override TranslationEngineName EngineName => TranslationEngineName.ModernMT;
        // Public web-client signing constant, not a user's API key.
        private const string WebClient = "webkey_E3sTuMjpP8Jez49GcYpDVH7r";
        public ModernMtTranslator(ILogger logger, HttpClient client = null) : base(logger, client) { }
        protected override async Task<string> TranslateCoreAsync(string text, string source, string target, CancellationToken token)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var verify = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(WebClient + "#" + timestamp + "#" + text))).ToLowerInvariant();
            var request = JsonRequest("https://webapi.modernmt.com/translate", new
            {
                q = text, source = source == "auto" ? "" : source, target, ts = timestamp, verify, hints = "", multiline = "true"
            });
            request.Headers.Add("Origin", "https://www.modernmt.com");
            request.Headers.Referrer = new Uri("https://www.modernmt.com/translate");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("X-HTTP-Method-Override", "GET");
            return (string)JObject.Parse(await SendAsync(request, token).ConfigureAwait(false))["data"]?["translation"];
        }
        internal static HttpRequestMessage JsonRequest(string url, object body)
            => new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json") };
    }

    internal sealed class AlibabaTranslator : WebSessionTranslator
    {
        public override TranslationEngineName EngineName => TranslationEngineName.Alibaba;
        private string _csrf;
        public AlibabaTranslator(ILogger logger, HttpClient client = null) : base(logger, client) { }
        protected override void ResetSession() => _csrf = null;
        protected override async Task<string> TranslateCoreAsync(string text, string source, string target, CancellationToken token)
        {
            if (string.IsNullOrEmpty(_csrf))
            {
                await SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://translate.alibaba.com"), token).ConfigureAwait(false);
                _csrf = (string)JObject.Parse(await SendAsync(new HttpRequestMessage(HttpMethod.Get,
                    "https://translate.alibaba.com/api/translate/csrftoken"), token).ConfigureAwait(false))["token"];
                if (string.IsNullOrWhiteSpace(_csrf)) throw new InvalidOperationException("Session token missing.");
            }
            var url = "https://translate.alibaba.com/api/translate/text?srcLang=" + Uri.EscapeDataString(source)
                + "&tgtLang=" + Uri.EscapeDataString(target) + "&domain=general&query=" + Uri.EscapeDataString(text) + "&_csrf=" + Uri.EscapeDataString(_csrf);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Referrer = new Uri("https://translate.alibaba.com/");
            var body = await SendAsync(request, token).ConfigureAwait(false);
            return WebUtility.HtmlDecode((string)JObject.Parse(body)["data"]?["translateText"]);
        }
    }

    internal sealed class VolcengineTranslator : WebSessionTranslator
    {
        public override TranslationEngineName EngineName => TranslationEngineName.Volcengine;
        public VolcengineTranslator(ILogger logger, HttpClient client = null) : base(logger, client) { }
        protected override async Task<string> TranslateCoreAsync(string text, string source, string target, CancellationToken token)
        {
            var body = new JObject { ["text"] = text, ["target_language"] = target, ["enable_user_glossary"] = false, ["glossary_list"] = new JArray(), ["category"] = "" };
            if (source != "auto") body["source_language"] = source;
            var request = ModernMtTranslator.JsonRequest("https://translate.volcengine.com/crx/translate/v1/", body);
            request.Headers.Add("Origin", "chrome-extension://klgfhbiooeogdfodpopgppeadghjjemk");
            return (string)JObject.Parse(await SendAsync(request, token).ConfigureAwait(false))["translation"];
        }
    }

    internal sealed class TencentTranslator : WebSessionTranslator
    {
        public override TranslationEngineName EngineName => TranslationEngineName.Tencent;
        public TencentTranslator(ILogger logger, HttpClient client = null) : base(logger, client) { }
        protected override async Task<string> TranslateCoreAsync(string text, string source, string target, CancellationToken token)
        {
            // yi.qq.com is the site's current origin; the old transmart.qq.com
            // certificate expired. Verification remains enabled in our client.
            var request = ModernMtTranslator.JsonRequest("https://yi.qq.com/api/imt", new
            {
                header = new { fn = "auto_translation", session = "", client_key = "browser-chrome-124.0.0-Windows_10-" + Guid.NewGuid() + "-" + DateTimeOffset.UtcNow.ToUnixTimeSeconds(), user = "" },
                type = "plain", model_category = "normal", text_domain = "general",
                source = new { lang = source, text_list = new[] { "", text, "" } }, target = new { lang = target }
            });
            request.Headers.Add("Origin", "https://yi.qq.com");
            request.Headers.Referrer = new Uri("https://yi.qq.com/");
            var translations = JObject.Parse(await SendAsync(request, token).ConfigureAwait(false))["auto_translation"] as JArray;
            if (translations == null) return null;
            var result = new StringBuilder();
            foreach (var part in translations) result.Append((string)part);
            return result.ToString();
        }
    }
}
