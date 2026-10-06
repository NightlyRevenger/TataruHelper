using System;

using Translation.Models;

namespace Translation.Exceptions
{
    /// <summary>
    /// A translation service answered, and the answer was no - with its own
    /// words for why, which is what the player needs to read.
    ///
    /// Until this was thrown the refusal was written to the log and the line
    /// came back empty, so every refusal looked the same in the chat window:
    /// "Translation failed: OpenAI", whether the model named did not exist,
    /// the key had no access to it, or the request carried a setting the
    /// model does not take. A player who tried every model from gpt-4 to
    /// gpt-6 saw that one line for each, and had no way to tell them apart.
    /// </summary>
    public class ServiceRefusedException : Exception
    {
        public TranslationEngineName EngineName { get; }

        /// <summary>The HTTP status the service answered with.</summary>
        public int Status { get; }

        public ServiceRefusedException(TranslationEngineName engineName, int status, string message)
            : base(message)
        {
            EngineName = engineName;
            Status = status;
        }
    }
}
