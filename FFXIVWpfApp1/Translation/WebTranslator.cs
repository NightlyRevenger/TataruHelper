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

        DeepLTranslator _DeepLTranslator;

        PapagoTranslator _PapagoTranslator;

        Amazon.Translate.AmazonTranslateClient amazonTranslateClient;

        private bool amazonLoaded = false;

        private Regex GoogleRx;

        List<KeyValuePair<TranslationRequest, string>> transaltionCache;
        KeyValuePair<TranslationRequest, string> defaultCachedResult = default(KeyValuePair<TranslationRequest, string>);

        public WebTranslator()
        {
            GoogleWebRead = new WebApi.WebReader(@"translate.google.com");
            MultillectWebRead = new WebApi.WebReader(@"translate.multillect.com");
            YandexWebRead = new WebApi.WebReader(@"translate.yandex.net");

            transaltionCache = new List<KeyValuePair<TranslationRequest, string>>(200);

            Task.Run(() =>
            {
                try
                {
                    amazonTranslateClient = new Amazon.Translate.AmazonTranslateClient(@"", @"", Amazon.RegionEndpoint.EUCentral1);
                    amazonLoaded = true;
                }
                catch (Exception ex)
                { Logger.WriteLog(Convert.ToString(ex)); }
            });

            _DeepLTranslator = new DeepLTranslator();

            _PapagoTranslator = new PapagoTranslator();

            string pattern = "(?<=(<div dir=\"ltr\" class=\"t0\">)).*?(?=(<\\/div>))";

            GoogleRx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public void LoadLanguages(string glTrPath, string MultTrPath, string deepPath, string YaTrPath, string AmazonTrPath, string PapagoTrPath)
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

                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(AmazonTrPath);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.Amazon, tmpList, 6));

                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(PapagoTrPath);
                tmptranslationEngines.Add(new TranslationEngine(TranslationEngineName.Papago, tmpList, 7));

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
            if (fromLang.SystemName == toLang.SystemName)
                return inSentence;

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
                case TranslationEngineName.Amazon:
                    {
                        result = AmazonTranslate(inSentence, fromLangCode, toLangCode);
                        break;
                    }
                case TranslationEngineName.Papago:
                    {
                        result = PapagoTranslate(inSentence, fromLangCode, toLangCode);
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
                string _outLang = outLang;
                string _inLang = inLang;

                string _baseUrl = @"https://translate.yandex.net/api/v1.5/tr.json/translate?lang={0}-{1}&key={2}";
                string url = string.Format(_baseUrl, _inLang, _outLang, @"");

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

        private string AmazonTranslate(string sentence, string inLang, string outLang)
        {
            string result = string.Empty;

            try
            {
                if (!amazonLoaded)
                    Task.Run(async () =>
                    {
                        var startTime = DateTime.Now;
                        while (!amazonLoaded && (DateTime.Now - startTime).TotalMilliseconds < GlobalSettings.TranslatorWaitTime)
                        {
                            await Task.Delay(100);
                        }
                    }).Wait();

                if (amazonLoaded)
                {

                    Amazon.Translate.Model.TranslateTextRequest translateTextRequest = new Amazon.Translate.Model.TranslateTextRequest();
                    translateTextRequest.SourceLanguageCode = inLang;
                    translateTextRequest.TargetLanguageCode = outLang;
                    translateTextRequest.Text = sentence;

                    Task.Run(async () =>
                    {
                        try
                        {
                            var res = await amazonTranslateClient.TranslateTextAsync(translateTextRequest);
                            result = res.TranslatedText;
                        }
                        catch (Exception ex)
                        { Logger.WriteLog(Convert.ToString(ex)); }

                    }).Wait(GlobalSettings.TranslatorWaitTime);
                }
            }
            catch (Exception e)
            { Logger.WriteLog(Convert.ToString(e)); }

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

        private string PreprocessSentence(string sentence)
        {
            return sentence.Replace("&", " and ");
        }
    }
}
