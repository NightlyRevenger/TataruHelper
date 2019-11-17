// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using FFXIVTataruHelper.Translation.Baidu;
using IvanAkcheurov.NTextCat.Lib;

namespace FFXIVTataruHelper.Translation
{
    public class WebTranslator
    {
        public ReadOnlyCollection<TranslationEngine> TranslationEngines
        {
            get { return _TranslationEngines; }
        }

        private ReadOnlyCollection<TranslationEngine> _TranslationEngines;

        WebApi.WebReader GoogleWebRead;

        WebApi.WebReader MultillectWebRead;

        WebApi.WebReader YandexWebRead;

        BaiduTranslater _BaiduЕranslator;

        DeepLTranslator _DeepLTranslator;

        PapagoTranslator _PapagoTranslator;

        private bool amazonLoaded = false;

        private Regex GoogleRx;

        List<KeyValuePair<TranslationRequest, string>> transaltionCache;
        KeyValuePair<TranslationRequest, string> defaultCachedResult = default(KeyValuePair<TranslationRequest, string>);

        RankedLanguageIdentifierFactory _NTextCatFactory = null;
        RankedLanguageIdentifier _NTextCatIdentifier = null;
        bool _LanguageIdentificationFailed = false;

        public WebTranslator()
        {
            GoogleWebRead = new WebApi.WebReader(@"translate.google.com");
            MultillectWebRead = new WebApi.WebReader(@"translate.multillect.com");
            YandexWebRead = new WebApi.WebReader(@"translate.yandex.net");

            transaltionCache = new List<KeyValuePair<TranslationRequest, string>>(200);

            _DeepLTranslator = new DeepLTranslator();

            _PapagoTranslator = new PapagoTranslator();

            _BaiduЕranslator = new BaiduTranslater();

            string pattern = "(?<=(<div dir=\"ltr\" class=\"t0\">)).*?(?=(<\\/div>))";

            GoogleRx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public void LoadLanguages(string glTrPath, string MultTrPath, string deepPath, string YaTrPath, string PapagoTrPath, string baiduTrPath)
        {
            try
            {
                List<TranslationEngine> tmptranslationEngines = new List<TranslationEngine>();
                var tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(glTrPath);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.GoogleTranslate, tmpList, 9));

                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(MultTrPath);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.Multillect, tmpList, 1));

                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(deepPath);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.DeepL, tmpList, 10));

                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(YaTrPath);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.Yandex, tmpList, 4));

                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(PapagoTrPath);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.Papago, tmpList, 7));

                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(baiduTrPath);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.Baidu, tmpList, 3));

                tmptranslationEngines = tmptranslationEngines.OrderBy(x => x.Quality * (-1)).ToList();


                _TranslationEngines = new ReadOnlyCollection<TranslationEngine>(tmptranslationEngines);
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        public string Translate(string inSentence, TranslationEngine translationEngine, TranslatorLanguague fromLang, TranslatorLanguague toLang)
        {

            if (fromLang.SystemName == "Auto")
            {
                if (translationEngine.EngineName != TranslationEngineName.GoogleTranslate)
                {
                    var dLang = TryDetectLanguague(inSentence);
                    if (dLang.Length > 1)
                    {
                        var nLang = translationEngine.SupportedLanguages.FirstOrDefault(x => x.SystemName == dLang);
                        if (nLang != null)
                            fromLang = nLang;
                    }
                }
            }

            if (fromLang.SystemName == toLang.SystemName)
                return inSentence;

            switch (toLang.SystemName)
            {
                case "Korean":
                    if (HasKorean(inSentence) >= GlobalSettings.MaxSameLanguagePercent)
                        return inSentence;
                    break;
                case "Japanese":
                    if (HasJapanese(inSentence) >= GlobalSettings.MaxSameLanguagePercent)
                        return inSentence;
                    break;
            }

            TranslationRequest translationRequest = new TranslationRequest(inSentence, translationEngine.EngineName, fromLang.LanguageCode, toLang.LanguageCode);
            var cachedResult = transaltionCache.FirstOrDefault(x => x.Key == translationRequest);

            if (!cachedResult.Equals(defaultCachedResult))
            {
                return cachedResult.Value;
            }

            string result = String.Empty;

            inSentence = PreprocessSentence(inSentence);

            var fromLangCode = fromLang.LanguageCode;
            var toLangCode = toLang.LanguageCode;

            switch (translationEngine.EngineName)
            {
                case TranslationEngineName.GoogleTranslate:
                    {
                        result = GoogleTranslate(inSentence, fromLangCode, toLangCode);
                        break;
                    }

                case TranslationEngineName.Multillect:
                    {
                        result = MultillectTranslate(inSentence, fromLangCode, toLangCode);
                        break;
                    }
                case TranslationEngineName.DeepL:
                    {
                        result = DeeplTranslate(inSentence, fromLangCode, toLangCode);
                        break;
                    }
                case TranslationEngineName.Yandex:
                    {
                        result = YandexTranslate(inSentence, fromLangCode, toLangCode);
                        break;
                    }
                case TranslationEngineName.Papago:
                    {
                        result = PapagoTranslate(inSentence, fromLangCode, toLangCode);
                        break;
                    }
                case TranslationEngineName.Baidu:
                    {
                        result = BaiduTranslate(inSentence, fromLangCode, toLangCode);
                        break;
                    }
                default:
                    {
                        result = String.Empty;
                        break;
                    }
            }

            if (result.Length > 1)
            {
                cachedResult = transaltionCache.FirstOrDefault(x => x.Key == translationRequest);

                if (cachedResult.Equals(defaultCachedResult))
                    transaltionCache.Add(new KeyValuePair<TranslationRequest, string>(translationRequest, result));

                if (transaltionCache.Count > 180)
                    transaltionCache.RemoveRange(0, 50);

            }

            return result;
        }

        public async Task<string> TranslateAsync(string inSentence, TranslationEngine translationEngine, TranslatorLanguague fromLang, TranslatorLanguague toLang)
        {
            string result = String.Empty;

            await Task.Run(() =>
            {
                result = Translate(inSentence, translationEngine, fromLang, toLang);
            });

            return result;
        }

        private string GoogleTranslate(string sentence, string inLang, string outLang)
        {
            string result = String.Empty;

            try
            {
                string _outLang = outLang;
                string _inLang = inLang;

                string _baseUrl = "https://translate.google.com/m?hl=ru&sl={0}&tl={1}&ie=UTF-8&prev=_m&q={2}";
                string url = string.Format(_baseUrl, _inLang, _outLang, sentence);

                var tmpResult = GoogleWebRead.GetWebData(url, WebApi.WebReader.WebMethods.GET);

                var rxMatch = GoogleRx.Match(tmpResult);

                if (rxMatch.Success)
                {
                    result = System.Net.WebUtility.HtmlDecode(rxMatch.Value);
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }

            return result;
        }

        private string MultillectTranslate(string sentence, string inLang, string outLang)
        {
            string result = String.Empty;
            try
            {
                string _outLang = outLang;
                string _inLang = inLang;

                string _baseUrl = @"https://translate.multillect.com/form.json?from={0}&to={1}&text={2}";
                string url = string.Format(_baseUrl, _inLang, _outLang, sentence);

                var tmpResult = MultillectWebRead.GetWebData(url, WebApi.WebReader.WebMethods.GET);

                var resp = JsonConvert.DeserializeObject<MultillectResponse.MultillectRoot>(tmpResult);

                result = resp.result.translations;
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }

            return result;
        }

        private string DeeplTranslate(string sentence, string inLang, string outLang)
        {
            return _DeepLTranslator.Translate(sentence, inLang, outLang);
        }

        private string YandexTranslate(string sentence, string inLang, string outLang)
        {
            string result = String.Empty;
            try
            {
                string yaApiKey = @"trnsl.1.1.20190204T134422Z.621647c1cffc2039.c2005599368f64d39003df38affa93a62699bcfe";
                string _outLang = outLang;
                string _inLang = inLang;

                string _baseUrl = @"https://translate.yandex.net/api/v1.5/tr.json/translate?lang={0}-{1}&key={2}";
                string url = string.Format(_baseUrl, _inLang, _outLang, yaApiKey);

                var tmpResult = YandexWebRead.GetWebData(url, WebApi.WebReader.WebMethods.POST, "text=" + sentence);

                var resp = JsonConvert.DeserializeObject<YandexResponse>(tmpResult);

                if (resp.code == 200)
                {
                    for (int i = 0; i < resp.text.Count; i++)
                    {
                        result += resp.text[i] + " ";
                    }
                }

            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }

            return result;
        }

        private string PapagoTranslate(string sentence, string inLang, string outLang)
        {
            string result = string.Empty;

            try
            {
                result = _PapagoTranslator.Translate(sentence, inLang, outLang);
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }

            return result;
        }

        private string BaiduTranslate(string sentence, string inLang, string outLang)
        {
            string result = string.Empty;

            try
            {
                result = _BaiduЕranslator.Translate(sentence, inLang, outLang);
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }

            return result;
        }

        private string PreprocessSentence(string sentence)
        {
            return sentence.Replace("&", " and ");
        }

        private double HasKorean(string sentence)
        {
            if (sentence.Length == 0)
                return 0;

            int koreanCount = 0;

            for (int i = 0; i < sentence.Length; i++)
            {
                if (IsKoreanLetter(sentence[i]))
                    koreanCount++;
            }

            return (double)koreanCount / (double)sentence.Length;
        }

        private double HasJapanese(string sentence)
        {
            if (sentence.Length == 0)
                return 0;

            int japaneseCount = 0;

            for (int i = 0; i < sentence.Length; i++)
            {
                if (IsJapaneseLetter(sentence[i]))
                    japaneseCount++;
            }

            return (double)japaneseCount / (double)sentence.Length;
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

        private string TryDetectLanguague(string text)
        {
            string result = string.Empty;

            if (_LanguageIdentificationFailed)
                return result;

            try
            {
                if (_NTextCatFactory == null || _NTextCatIdentifier == null)
                {
                    _NTextCatFactory = new RankedLanguageIdentifierFactory();
                    _NTextCatIdentifier = _NTextCatFactory.Load(GlobalSettings.NTextCatLanguageModelsPath);

                }

                var languages = _NTextCatIdentifier.Identify(text);
                var mostCertainLanguage = languages.FirstOrDefault();

                if (mostCertainLanguage != null)
                    result = Helper.ConvertISOLangugueNameToSystemName(mostCertainLanguage.Item1.Iso639_3);
            }
            catch (Exception e)
            {
                _LanguageIdentificationFailed = true;
                Logger.WriteLog(e);
            }

            return result;
        }
    }
}
