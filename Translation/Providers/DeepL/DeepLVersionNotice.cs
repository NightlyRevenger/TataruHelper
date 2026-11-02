using System;

namespace Translation.Providers.DeepL
{
    /// <summary>
    /// Recognizes the notice DeepL's free web endpoint has been answering in
    /// place of translations since 2 November 2026: HTTP 200 carrying "You're
    /// using an older version of DeepL…" repeated in fifteen languages, the
    /// same blob whatever language was asked for.
    ///
    /// The request shape behind it is retired, so knocking again brings the
    /// same text back; it is a refusal whatever the status code says. The
    /// blob is matched one language at a time rather than by its English line
    /// alone, so a reply narrowed to the asked-for language is refused too.
    /// No line of game dialogue holds any of these sentences.
    ///
    /// tools/deepl-relay/relay.go carries the same markers for the relay.
    /// </summary>
    internal static class DeepLVersionNotice
    {
        private static readonly string[] Markers =
        {
            "older version of deepl",       // EN
            "ältere version von deepl",     // DE
            "старую версию deepl",          // RU
            "ancienne version de deepl",    // FR
            "旧版本的 deepl",               // zh-CN
            "deeplの古いバージョン",        // ja
            "versión antigua de deepl",     // es
            "стару версію deepl",           // uk
            "versione precedente di deepl", // it
            "eski bir sürümünü",            // tr
            "starszej wersji deepl",        // pl
            "versão mais antiga do deepl",  // pt
            "oudere versie van deepl",      // nl
            "deepl versi lama",             // id
            "starší verzi deepl",           // cs
        };

        public static bool IsPresent(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            foreach (var marker in Markers)
                if (text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

            return false;
        }
    }
}
