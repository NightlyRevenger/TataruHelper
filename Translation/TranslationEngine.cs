// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translation
{
    public enum TranslationEngineName : int
    {
        GoogleTranslate = 0,
        Multillect = 1,
        DeepL = 2,
        Yandex = 3,
        Amazon = 4,
        Papago = 5,
        Baidu = 6,
    }

    public class TranslationEngine : IEquatable<TranslationEngine>
    {
        public string Name
        {
            get { return EngineName.ToString(); }
        }

        public ReadOnlyCollection<TranslatorLanguague> SupportedLanguages
        {
            get { return _SupportedLanguages; }
        }

        public TranslationEngineName EngineName { get; private set; }
        public double Quality { get; private set; }

        ReadOnlyCollection<TranslatorLanguague> _SupportedLanguages;

        public TranslationEngine(TranslationEngineName translationEngineName, List<TranslatorLanguague> translatorLanguagues, double quality)
        {
            this.EngineName = translationEngineName;
            _SupportedLanguages = new ReadOnlyCollection<TranslatorLanguague>(translatorLanguagues);
            Quality = quality;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as TranslationEngine);
        }

        public bool Equals(TranslationEngine engine)
        {
            if (Object.ReferenceEquals(engine, null))
                return false;

            if (Object.ReferenceEquals(this, engine))
                return true;

            if (this.GetType() != engine.GetType())
                return false;

            return this.EngineName == engine.EngineName;
        }

        public static bool operator ==(TranslationEngine left, TranslationEngine right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null))
                return false;

            if (ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(TranslationEngine left, TranslationEngine right) => !(left == right);

        public override int GetHashCode()
        {
            return ((int)EngineName).GetHashCode();
        }
    }
}
