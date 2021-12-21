// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using Translation.Baidu;
using Translation.Deepl;
using Translation.Papago;
using Translation.Google;
using Translation.Yandex;
using Translation.Utils;

namespace Translation
{
    public class WebTranslator
    {
        public ReadOnlyCollection<TranslationEngine> TranslationEngines
        {
            get { return _TranslationEngines; }
        }

        private ReadOnlyCollection<TranslationEngine> _TranslationEngines;

        GoogleTranslator _GoogleTranslator;

        YandexTranslator _YandexTranslator;

        BaiduTranslater _BaiduTranslator;

        DeepLTranslator _DeepLTranslator;

        PapagoTranslator _PapagoTranslator;

        List<KeyValuePair<TranslationRequest, string>> transaltionCache;
        KeyValuePair<TranslationRequest, string> defaultCachedResult = default(KeyValuePair<TranslationRequest, string>);

        LanguageDetector _LanguageDetector;

        ILog _Logger;

        string _TransaltionSettingsPath = "TranslationSysSettings.json";

        public WebTranslator(ILog logger)
        {

            _Logger = logger;

            if (!Helper.LoadStaticFromJson(typeof(GlobalTranslationSettings), _TransaltionSettingsPath))
            {
                Helper.SaveStaticToJson(typeof(GlobalTranslationSettings), _TransaltionSettingsPath);
                Helper.LoadStaticFromJson(typeof(GlobalTranslationSettings), _TransaltionSettingsPath);
            }

            transaltionCache = new List<KeyValuePair<TranslationRequest, string>>(GlobalTranslationSettings.TranslationCacheSize);

            _GoogleTranslator = new GoogleTranslator(_Logger);

            _YandexTranslator = new YandexTranslator(_Logger);

            _DeepLTranslator = new DeepLTranslator(_Logger);

            _PapagoTranslator = new PapagoTranslator(_Logger);

            _BaiduTranslator = new BaiduTranslater(_Logger);

            _LanguageDetector = new LanguageDetector(GlobalTranslationSettings.MaxSameLanguagePercent,
                GlobalTranslationSettings.NTextCatLanguageModelsPath, _Logger);
        }

        public void LoadLanguages()
        {
            LoadLanguages(
                GlobalTranslationSettings.GoogleTranslateLanguages,
                GlobalTranslationSettings.DeeplLanguages,
                GlobalTranslationSettings.YandexLanguages,
                GlobalTranslationSettings.PapagoLanguages,
                GlobalTranslationSettings.BaiduLanguages);
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

        public string Translate(string inSentence, TranslationEngine translationEngine, TranslatorLanguague fromLang, TranslatorLanguague toLang)
        {

            if (fromLang.SystemName == "Auto")
            {
                if (translationEngine.EngineName != TranslationEngineName.GoogleTranslate)
                {
                    var dLang = _LanguageDetector.TryDetectLanguague(inSentence);
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

            if (inSentence.All(x => !char.IsLetter(x)))
                return inSentence;

            switch (toLang.SystemName)
            {
                case "Korean":
                    if (_LanguageDetector.HasKorean(inSentence))
                        return inSentence;
                    break;
                case "Japanese":
                    if (_LanguageDetector.HasJapanese(inSentence))
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

                if (transaltionCache.Count > GlobalTranslationSettings.TranslationCacheSize - 10)
                    transaltionCache.RemoveRange(0, GlobalTranslationSettings.TranslationCacheSize / 2);

            }

            return result;
        }

        private void LoadLanguages(string glTrPath, string deepPath, string YaTrPath, string PapagoTrPath, string baiduTrPath)
        {
            try
            {
                List<TranslationEngine> tmptranslationEngines = new List<TranslationEngine>();
                var tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(glTrPath, _Logger);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.GoogleTranslate, tmpList, 9));

                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(deepPath, _Logger);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.DeepL, tmpList, 10));

                /*
                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(YaTrPath, _Logger);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.Yandex, tmpList, 8));//*/

                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(PapagoTrPath, _Logger);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.Papago, tmpList, 6));

                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(baiduTrPath, _Logger);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.Baidu, tmpList, 3));

                tmptranslationEngines = tmptranslationEngines.OrderByDescending(x => x.Quality).ToList();


                _TranslationEngines = new ReadOnlyCollection<TranslationEngine>(tmptranslationEngines);
            }
            catch (Exception e)
            {
                _Logger.WriteLog(Convert.ToString(e));
            }
        }

        private string GoogleTranslate(string sentence, string inLang, string outLang)
        {
            string result = String.Empty;

            try
            {
                result = _GoogleTranslator.Translate(sentence, inLang, outLang);
            }
            catch (Exception e)
            {
                _Logger.WriteLog(Convert.ToString(e));
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
                result = _YandexTranslator.Translate(sentence, inLang, outLang);
            }
            catch (Exception e)
            {
                _Logger.WriteLog(Convert.ToString(e));
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
                _Logger.WriteLog(e.ToString());
            }

            return result;
        }

        private string BaiduTranslate(string sentence, string inLang, string outLang)
        {
            string result = string.Empty;

            try
            {
                result = _BaiduTranslator.Translate(sentence, inLang, outLang);
            }
            catch (Exception e)
            {
                _Logger.WriteLog(e.ToString());
            }

            return result;
        }

        private string PreprocessSentence(string sentence)
        {
            return sentence.Replace("&", " and ");
        }
    }
}
