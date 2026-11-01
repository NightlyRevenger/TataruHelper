using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Translation.Credentials;
using Translation.Models;
using Translation.Providers.AI;

namespace Translation.Providers.OpenRouter
{
    /// <summary>
    /// One key, many models. OpenRouter speaks the OpenAI shape and forwards to
    /// whichever model the name in the model box asks for, so a player who
    /// wants to try something other than what we ship an engine for does not
    /// have to wait for us to add it.
    /// </summary>
    internal sealed class OpenRouterTranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.OpenRouter;

        private readonly OpenAIChatClient _client;

        public OpenRouterTranslator(ILogger logger, ITranslationCredentialStore credentials)
        {
            _client = new OpenAIChatClient(
                TranslationEngineName.OpenRouter,
                "https://openrouter.ai/api/v1/chat/completions",
                "openai/gpt-4o-mini",
                logger,
                credentials,
                extraHeaders: new Dictionary<string, string>
                {
                    // OpenRouter attributes requests to the calling application;
                    // without these the dashboard shows the traffic as unnamed.
                    ["HTTP-Referer"] = "https://github.com/NightlyRevenger/TataruHelper",
                    ["X-Title"] = "Tataru Helper",
                });
        }

        public Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken)
        {
            return _client.TranslateAsync(sentence, inLang, outLang, cancellationToken);
        }

        public Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            TranslationTextKind kind, CancellationToken cancellationToken)
        {
            return _client.TranslateAsync(sentence, inLang, outLang, kind, cancellationToken);
        }
    }
}
