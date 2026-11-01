using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Translation.Credentials;
using Translation.Models;
using Translation.Providers.AI;

namespace Translation.Providers.Local
{
    /// <summary>
    /// The other common way to run a model locally. Same contract as
    /// <see cref="OllamaTranslator"/> on a different port.
    /// </summary>
    internal sealed class LmStudioTranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.LmStudio;

        private readonly OpenAIChatClient _client;

        public LmStudioTranslator(ILogger logger, ITranslationCredentialStore credentials)
        {
            _client = new OpenAIChatClient(
                TranslationEngineName.LmStudio,
                "http://localhost:1234/v1/chat/completions",
                // LM Studio answers with whichever model is loaded, so the name
                // only has to be present, not correct.
                "local-model",
                logger,
                credentials,
                requiresApiKey: false);
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
