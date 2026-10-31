using System;

using Translation.Exceptions;
using Translation.Models;

namespace Translation.Http
{
    /// <summary>
    /// The pattern DeepL already follows, for the engines that used to answer a
    /// refusal with an empty string: remember that the service asked us to slow
    /// down, say so out loud so the line goes to another engine at once, and
    /// leave the service alone for a while instead of knocking on every line.
    ///
    /// An empty string looks like "no translation", so the line was asked for
    /// again and the refusal repeated at exactly the rate it objected to.
    /// </summary>
    internal sealed class EngineRateLimitGuard
    {
        private readonly TranslationEngineName _engine;
        private readonly RefusalCooldown _cooldown;

        public EngineRateLimitGuard(TranslationEngineName engine, TimeSpan pause)
        {
            _engine = engine;
            _cooldown = new RefusalCooldown(pause);
        }

        /// <summary>Throws while a pause from an earlier refusal is still running.</summary>
        public void ThrowIfPaused()
        {
            if (_cooldown.IsActiveAt(DateTime.UtcNow, out var untilUtc))
            {
                throw new QuotaExceededException(_engine,
                    _engine + " asked for fewer requests; leaving it alone until " +
                    untilUtc.ToLocalTime().ToString("HH:mm:ss") + ".");
            }
        }

        /// <summary>Starts the pause and returns the exception to throw for this refusal.</summary>
        public QuotaExceededException Refused()
        {
            _cooldown.Record(DateTime.UtcNow);
            return new QuotaExceededException(_engine,
                _engine + " is receiving requests faster than it allows. It clears on its own; " +
                "translation continues on another engine in the meantime.");
        }
    }
}
