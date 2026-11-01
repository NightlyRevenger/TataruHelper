using System;

using Translation.Credentials;
using Translation.Models;

namespace Translation.Settings
{
    /// <summary>Connects a relay started and checked by the desktop launcher.</summary>
    public static class ManagedDeepLRelay
    {
        public const string EnvironmentVariable = "TATARU_DEEPL_RELAY";

        /// <summary>
        /// Raised after the relay's address was written to, or taken out of, the
        /// settings. The relay is connected in the background a moment after
        /// start-up, so a settings page that already read the address has to
        /// be told it changed.
        /// </summary>
        public static event EventHandler Changed;

        public static bool Configure(ITranslationCredentialStore store, string endpoint)
        {
            // The launcher may configure its own loopback relay, never a remote
            // service. Explicit custom endpoints remain the user's choice.
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttp || !uri.IsLoopback
                || !string.IsNullOrEmpty(uri.UserInfo))
                return false;

            var current = store.GetEndpoint(TranslationEngineName.DeepL);
            if (!string.IsNullOrWhiteSpace(current)
                && !string.Equals(current.TrimEnd('/'), endpoint.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                return false;

            store.SetEndpoint(TranslationEngineName.DeepL, endpoint);
            store.Save();
            Changed?.Invoke(null, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Clears the launcher's own address once its relay is gone, so DeepL goes
        /// straight to the next route instead of knocking on a closed port. An
        /// address the player entered is never touched.
        /// </summary>
        public static bool Forget(ITranslationCredentialStore store, string defaultEndpoint)
        {
            var current = store.GetEndpoint(TranslationEngineName.DeepL);
            if (string.IsNullOrWhiteSpace(current)
                || !string.Equals(current.TrimEnd('/'), defaultEndpoint.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                return false;

            store.SetEndpoint(TranslationEngineName.DeepL, string.Empty);
            store.Save();
            Changed?.Invoke(null, EventArgs.Empty);
            return true;
        }
    }
}
