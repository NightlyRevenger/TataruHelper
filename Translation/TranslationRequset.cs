// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;

namespace Translation
{
    struct TranslationRequest : IEquatable<TranslationRequest>
    {
        public string InSentence { get; private set; }
        public TranslationEngineName TranslationEngineName { get; private set; }
        public string FromLang { get; private set; }
        public string ToLang { get; private set; }

        public TranslationRequest(string inSentence, TranslationEngineName translationEngineName, string fromLang, string toLang)
        {
            InSentence = inSentence;
            TranslationEngineName = translationEngineName;
            FromLang = fromLang;
            ToLang = toLang;
        }

        public override bool Equals(object obj)
        {
            if (obj is TranslationRequest)
                return this.Equals((TranslationRequest)obj);

            return false;
        }

        public bool Equals(TranslationRequest reqv)
        {
            if (Object.ReferenceEquals(reqv, null))
                return false;

            if (Object.ReferenceEquals(this, reqv))
                return true;

            if (this.GetType() != reqv.GetType())
                return false;

            bool result = InSentence == reqv.InSentence && TranslationEngineName == reqv.TranslationEngineName;
            result = result && FromLang == reqv.FromLang && ToLang == reqv.ToLang;

            return result;
        }

        public static bool operator ==(TranslationRequest left, TranslationRequest right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null))
                return false;

            if (ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(TranslationRequest left, TranslationRequest right) => !(left == right);

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + InSentence.GetHashCode();
                hash = hash * 23 + ((int)TranslationEngineName).GetHashCode();
                hash = hash * 23 + FromLang.GetHashCode();
                hash = hash * 23 + ToLang.GetHashCode();

                return hash;
            }
        }
    }
}
