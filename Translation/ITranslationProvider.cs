using System.Threading;
using System.Threading.Tasks;

using Translation.Models;

namespace Translation
{
    public interface ITranslationProvider
    {
        TranslationEngineName EngineName { get; }

        Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            CancellationToken cancellationToken);

        // The kind of text decides which prompt an AI engine uses. Engines that
        // do not care - DeepL, Google, Bing and the rest - inherit this default
        // and keep translating the same way whatever the kind.
        Task<string> TranslateAsync(string sentence, string inLang, string outLang,
            TranslationTextKind kind, CancellationToken cancellationToken)
            => TranslateAsync(sentence, inLang, outLang, cancellationToken);
    }
}