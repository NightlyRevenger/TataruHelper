using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Translation.Http;

namespace Translation.Providers.Google
{
    /// <summary>
    /// The key Google's own website-translation widget uses for its translation
    /// service, read from the widget the way a web page would.
    ///
    /// A key written into this program stops working the day Google changes it,
    /// and the program would not know why. The widget loader names its script,
    /// the script carries the key beside the address it is used for, so both are
    /// read when needed and kept for a few hours. What was last known to work is
    /// the fallback when the page cannot be reached.
    /// </summary>
    internal sealed class GoogleWidgetKey
    {
        private const string LoaderUrl = "https://translate.google.com/translate_a/element.js";

        private const string LastKnownKey = "AIzaSyATBXajvzQLTDHEQbcpq0Ihe0vWDHmO520";

        private static readonly TimeSpan Lifetime = TimeSpan.FromHours(6);

        private static readonly Regex ScriptReference =
            new Regex(@"_loadJs\('([^']+)'\)", RegexOptions.Compiled);

        private static readonly Regex KeyBesideTranslateHtml =
            new Regex(@"/v1/translateHtml.{0,300}?X-goog-api-key""\s*:\s*""(AIza[0-9A-Za-z_\-]{35})""",
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private readonly ILogger _logger;
        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1, 1);
        private string _key;
        private DateTime _readUtc = DateTime.MinValue;

        public GoogleWidgetKey(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<string> GetAsync(bool refresh, CancellationToken cancellationToken)
        {
            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var stale = DateTime.UtcNow - _readUtc > Lifetime;
                if (_key != null && !refresh && !stale)
                {
                    return _key;
                }

                var read = await TryReadAsync(cancellationToken).ConfigureAwait(false);
                if (read != null)
                {
                    _key = read;
                }
                else if (_key == null)
                {
                    _key = LastKnownKey;
                }

                // A failed read is not tried again for every line.
                _readUtc = read != null ? DateTime.UtcNow : DateTime.UtcNow - Lifetime + TimeSpan.FromMinutes(5);
                return _key;
            }
            finally
            {
                _sync.Release();
            }
        }

        private async Task<string> TryReadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var loader = await ApiHttpClient.Shared.GetStringAsync(LoaderUrl, cancellationToken)
                    .ConfigureAwait(false);

                var scriptUrl = ExtractScriptUrl(loader);
                if (scriptUrl == null)
                {
                    _logger?.LogInformation("[GOOGLE_WIDGET_KEY] no script named in the loader.");
                    return null;
                }

                var script = await ApiHttpClient.Shared.GetStringAsync(scriptUrl, cancellationToken)
                    .ConfigureAwait(false);

                var key = ExtractKey(script);
                if (key == null)
                {
                    _logger?.LogInformation("[GOOGLE_WIDGET_KEY] no key beside translateHtml in the script.");
                }

                return key;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger?.LogInformation("{Message}", "[GOOGLE_WIDGET_KEY] " + e.Message);
                return null;
            }
        }

        /// <summary>The loader writes the script's address with JavaScript escapes.</summary>
        internal static string ExtractScriptUrl(string loader)
        {
            if (string.IsNullOrEmpty(loader))
            {
                return null;
            }

            var match = ScriptReference.Match(loader);
            if (!match.Success)
            {
                return null;
            }

            var url = match.Groups[1].Value
                .Replace("\\/", "/")
                .Replace("\\x3d", "=")
                .Replace("\\x26", "&")
                .Replace("\\u003d", "=")
                .Replace("\\u0026", "&");

            return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? url : null;
        }

        internal static string ExtractKey(string script)
        {
            if (string.IsNullOrEmpty(script))
            {
                return null;
            }

            var match = KeyBesideTranslateHtml.Match(script);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
