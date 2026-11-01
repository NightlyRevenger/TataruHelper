using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Translation.Credentials;
using Translation.Models;
using Translation.Providers.AI;

namespace Translation.Providers.OpenAI
{
    internal sealed class OpenAITranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.OpenAI;

        private readonly OpenAIChatClient _client;

        public OpenAITranslator(ILogger logger, ITranslationCredentialStore credentials)
        {
            _client = new OpenAIChatClient(
                TranslationEngineName.OpenAI,
                "https://api.openai.com/v1/chat/completions",
                "gpt-4o-mini",
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