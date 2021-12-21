// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System;

namespace Translation
{
    public class TranslatorLanguague : IEquatable<TranslatorLanguague>
    {
        [JsonProperty]
        public string ShownName { get; private set; }
        [JsonProperty]
        public string SystemName { get; private set; }
        [JsonProperty]
        public string LanguageCode { get; private set; }

        public TranslatorLanguague()
        {
            ShownName = String.Empty;
            SystemName = String.Empty;
            LanguageCode = String.Empty;
        }
        public TranslatorLanguague(string shownName, string systemName, string languageCode)
        {
            ShownName = shownName;
            SystemName = systemName;
            LanguageCode = languageCode;
        }

        public TranslatorLanguague(TranslatorLanguague languague)
        {
            ShownName = languague.ShownName;
            SystemName = languague.SystemName;
            LanguageCode = languague.LanguageCode;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as TranslatorLanguague);
        }

        public bool Equals(TranslatorLanguague lang)
        {
            if (Object.ReferenceEquals(lang, null))
                return false;

            if (Object.ReferenceEquals(this, lang))
                return true;

            if (this.GetType() != lang.GetType())
                return false;

            return this.SystemName == lang.SystemName;
        }

        public static bool operator ==(TranslatorLanguague left, TranslatorLanguague right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null))
                return false;

            if (ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(TranslatorLanguague left, TranslatorLanguague right) => !(left == right);

        public override int GetHashCode()
        {
            return SystemName.GetHashCode();
        }

        public override string ToString()
        {
            return $"Name: {ShownName ?? SystemName ?? "null" }; Code: {LanguageCode ?? "null"}";
        }
    }
}
