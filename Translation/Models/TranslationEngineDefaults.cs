namespace Translation.Models
{
    public static class TranslationEngineDefaults
    {
        /// <summary>
        /// Whether an engine is offered before the user has said anything about
        /// it.
        ///
        /// Engines that need a server the user has to run themselves stay off.
        /// On by default they would sit in the picker pointing at nothing, and
        /// worse, be tried as a stand-in every time the selected engine fails -
        /// knocking on a port that nobody is listening on, on every line.
        ///
        /// A keyed engine does not have that problem: with no key it raises
        /// before it reaches the network, and the stand-in search skips it.
        /// </summary>
        public static bool IsOnByDefault(TranslationEngineName engine)
        {
            switch (engine)
            {
                // These extra web engines worked but made meaning-changing
                // mistakes on the dialogue probe. Make them opt-in reserves.
                case TranslationEngineName.Tencent:
                case TranslationEngineName.Volcengine:
                case TranslationEngineName.Alibaba:
                case TranslationEngineName.Ollama:
                case TranslationEngineName.LmStudio:
                case TranslationEngineName.LibreTranslate:
                    return false;
                default:
                    return true;
            }
        }
    }
}
