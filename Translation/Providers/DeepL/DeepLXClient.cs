using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using Translation.Http;

namespace Translation.Providers.DeepL
{
    /// <summary>
    /// A DeepLX server: someone's own relay that speaks DeepL's web protocol and
    /// answers a plain JSON request. The address is the player's to give.
    /// </summary>
    public static class DeepLXClient
    {
        internal static string BuildRequestBody(string text, string source, string target)
        {
            return new JObject
            {
                ["text"] = text,
                ["source_lang"] = string.IsNullOrWhiteSpace(source) ? "auto" : source.ToUpperInvariant(),
                ["target_lang"] = target.ToUpperInvariant(),
            }.ToString(Newtonsoft.Json.Formatting.None);
        }

        /// <summary>The translation, or null when the answer carries none.</summary>
        internal static string ParseResponse(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                var text = (string)JObject.Parse(body)["data"];
                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<string> TranslateAsync(string endpoint, string text, string source, string target,
            CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Trim()))
            {
                request.Content = new StringContent(BuildRequestBody(text, source, target), Encoding.UTF8,
                    "application/json");

                using (var response = await ApiHttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(
                            "The DeepLX server answered HTTP " + (int)response.StatusCode + ".");
                    }

                    return ParseResponse(body);
                }
            }
        }
    }
}
