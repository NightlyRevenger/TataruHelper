using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Translation.Credentials;
using Translation.Models;
using Translation.Providers.AI;

namespace Translation.Providers.DeepSeek
{
    internal sealed class DeepSeekTranslator : ITranslationProvider
    {
        public TranslationEngineName EngineName => TranslationEngineName.DeepSeek;

        private readonly OpenAIChatClient _client;

        public DeepSeekTranslator(ILogger logger, ITranslationCredentialStore credentials)
        {
            _client = new OpenAIChatClient(
                TranslationEngineName.DeepSeek,
                "https://api.deepseek.com/chat/completions",
                "deepseek-chat",
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