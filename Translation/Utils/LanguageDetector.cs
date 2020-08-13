// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using IvanAkcheurov.NTextCat.Lib;
using System;
using System.Linq;

namespace Translation.Utils
{
    class LanguageDetector
    {
        private double _MaxSameLanguagePercent;

        RankedLanguageIdentifierFactory _NTextCatFactory = null;
        RankedLanguageIdentifier _NTextCatIdentifier = null;
        bool _LanguageIdentificationFailed = false;
        string _NTextCatLanguageModelsPath;

        ILog _Logger;

        public LanguageDetector(double maxSameLanguagePercent, string nTextCatLanguageModelsPath, ILog logger)
        {
            _Logger = logger;
            _MaxSameLanguagePercent = maxSameLanguagePercent;

            _NTextCatLanguageModelsPath = nTextCatLanguageModelsPath;
        }

        public string TryDetectLanguague(string text)
        {
            string result = string.Empty;

            if (_LanguageIdentificationFailed)
                return result;

            try
            {
                if (_NTextCatFactory == null || _NTextCatIdentifier == null)
                {
                    _NTextCatFactory = new RankedLanguageIdentifierFactory();
                    _NTextCatIdentifier = _NTextCatFactory.Load(_NTextCatLanguageModelsPath);
                }

                var languages = _NTextCatIdentifier.Identify(text);
                var mostCertainLanguage = languages.FirstOrDefault();

                if (mostCertainLanguage != null)
                    result = ConvertISOLangugueNameToSystemName(mostCertainLanguage.Item1.Iso639_3);
            }
            catch (Exception e)
            {
                _LanguageIdentificationFailed = true;
                _Logger?.WriteLog(e.ToString());
            }

            return result;
        }

        public bool HasKorean(string sentence)
        {
            if (sentence.Length == 0)
                return false;

            int koreanCount = 0;

            for (int i = 0; i < sentence.Length; i++)
            {
                if (IsKoreanLetter(sentence[i]))
                    koreanCount++;
            }

            return (((double)koreanCount / (double)sentence.Length)>= _MaxSameLanguagePercent);
        }

        public bool HasJapanese(string sentence)
        {
            if (sentence.Length == 0)
                return false;

            int japaneseCount = 0;

            for (int i = 0; i < sentence.Length; i++)
            {
                if (IsJapaneseLetter(sentence[i]))
                    japaneseCount++;
            }

            return (((double)japaneseCount / (double)sentence.Length) >= _MaxSameLanguagePercent);
        }

        private bool IsKoreanLetter(char ch)
        {
            if ((0xAC00 <= ch && ch <= 0xD7A3) || (0x3131 <= ch && ch <= 0x318E))
                return true;

            return false;
        }

        private bool IsJapaneseLetter(char ch)
        {
            // 0x3040 -> 0x309F === Hirigana, 0x30A0 -> 0x30FF === Katakana, 0x4E00 -> 0x9FBF === Kanji

            if ((ch >= 0x3040 && ch <= 0x309F) || (ch >= 0x30A0 && ch <= 0x30FF) || (ch >= 0x4E00 && ch <= 0x9FBF))
                return true;

            return false;
        }

        private static string ConvertISOLangugueNameToSystemName(string lang)
        {
            string result = string.Empty;
            switch (lang)
            {
                case "eng":
                    result = "English";
                    break;
                case "dan":
                    result = "Danish";
                    break;
                case "nor":
                    result = "Norwegian";
                    break;
                case "fra":
                    result = "French";
                    break;
                case "spa":
                    result = "Spanish";
                    break;
                case "swe":
                    result = "Swedish";
                    break;
                case "nld":
                    result = "Dutch";
                    break;
                case "ita":
                    result = "Italian";
                    break;
                case "por":
                    result = "Portuguese";
                    break;
                case "deu":
                    result = "German";
                    break;
                case "rus":
                    result = "Russian";
                    break;
                case "kor":
                    result = "Korean";
                    break;
                case "zho":
                    result = "Chinese";
                    break;
                case "jpn":
                    result = "Japanese";
                    break;
            }

            return result;
        }
    }
}
