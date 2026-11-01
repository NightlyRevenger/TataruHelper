using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Translation.Credentials;
using Translation.Models;
using Translation.Providers.AI;

namespace Translation.Providers.Local
{
    /// <summary>
    /// A model running on the player's own machine. Nothing leaves it, nothing
    /// is billed, and nothing runs out mid-raid - which is the one failure the
    /// keyed engines keep producing.
    ///
    /// Ollama serves an OpenAI-compatible route beside its own, so the shape we
    /// already speak works without a second client.
    /// </summary>
    internal sealed class OllamaTranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.Ollama;

        private readonly OpenAIChatClient _client;

        public OllamaTranslator(ILogger logger, ITranslationCredentialStore credentials)
        {
            _client = new OpenAIChatClient(
                TranslationEngineName.Ollama,
                "http://localhost:11434/v1/chat/completions",
                "llama3.1",
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
