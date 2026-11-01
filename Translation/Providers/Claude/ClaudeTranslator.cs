using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Translation.Credentials;
using Translation.Models;
using Translation.Providers.AI;

namespace Translation.Providers.Claude
{
    internal sealed class ClaudeTranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.Claude;

        private readonly ClaudeChatClient _client;

        public ClaudeTranslator(ILogger logger, ITranslationCredentialStore credentials)
        {
            _client = new ClaudeChatClient(
                TranslationEngineName.Claude,
                // The small model by default. A chat line is one sentence, and
                // the capable models cost several times as much for a job that
                // does not need them; anyone who wants one names it in settings.
                "claude-haiku-4-5",
                logger,
                credentials);
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
