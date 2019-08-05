// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using static FFXIITataruHelper.Translation.WebTranslator;

namespace FFXIITataruHelper.Translation
{
    public struct EngineDescription
    {
        public TranslationEngine TranslationEngine { get; set; }
        public double Value { get; set; }

        public EngineDescription(TranslationEngine translationEngine, double value)
        {
            TranslationEngine = translationEngine;
            Value = value;
        }
    }
}
