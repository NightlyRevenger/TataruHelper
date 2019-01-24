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

namespace FFXIITataruHelper.Translation
{
    public class WebTranslator
    {
        public enum TranslationEngine : int
        {
            GoogleTranslate = 0,
            Multillect = 1,
            DeepL = 2
        }

        public string SourceLanguage
        {
            get { return _SourceLanguage.ShownName; }

            set
            {
                try
                {
                    if (value.Length > 1)
                    {
                        var languagues = _TranslatorsLanguages[(int)_CurrentTranslationEngine];

                        var lang = languagues.First(x => x.ShownName == value);

                        _SourceLanguage = lang;
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLog(Convert.ToString(e));
                }
            }

        }

        public string TargetLanguage
        {
            get { return _TargetLanguage.ShownName; }

            set
            {
                try
                {
                    if (value.Length > 1)
                    {
                        var languagues = _TranslatorsLanguages[(int)_CurrentTranslationEngine];

                        var lang = languagues.First(x => x.ShownName == value);

                        _TargetLanguage = lang;
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLog(Convert.ToString(e));
                }
            }
        }

        public TranslationEngine CurrentTranslationEngine
        {
            get { return _CurrentTranslationEngine; }

            set
            {
                _CurrentTranslationEngine = value;
            }
        }

        public ReadOnlyCollection<TranslatorLanguague> CurrentLanguages
        {
            get
            {
                return _TranslatorsLanguages[(int)_CurrentTranslationEngine];
            }
        }

        TranslatorLanguague _SourceLanguage;

        TranslatorLanguague _TargetLanguage;

        TranslationEngine _CurrentTranslationEngine;

        private ReadOnlyCollection<TranslatorLanguague> GoogleLanguages;

        private ReadOnlyCollection<TranslatorLanguague> MultillectLanguages;

        private ReadOnlyCollection<TranslatorLanguague> DeeplLanguages;

        private List<ReadOnlyCollection<TranslatorLanguague>> _TranslatorsLanguages;

        WebApi.WebReader GoogleWebRead;

        WebApi.WebReader MultillectWebRead;

        DeepLTranslator _DeepLTranslator;

        private Regex GoogleRx;

        public WebTranslator()
        {
            GoogleWebRead = new WebApi.WebReader(@"translate.google.com");
            MultillectWebRead = new WebApi.WebReader(@"translate.multillect.com");

            _DeepLTranslator = new DeepLTranslator();

            _TranslatorsLanguages = new List<ReadOnlyCollection<TranslatorLanguague>>();

            string pattern = "(?<=(<div dir=\"ltr\" class=\"t0\">)).*?(?=(<\\/div>))";

            GoogleRx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public void LoadLanguagues()
        {
            try
            {
                var tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(GlobalSettings.GoogleTranslateLanguages);
                GoogleLanguages = new ReadOnlyCollection<TranslatorLanguague>(tmpList);

                _TranslatorsLanguages.Add(GoogleLanguages);

                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(GlobalSettings.MultillectTranslateLanguages);
                MultillectLanguages = new ReadOnlyCollection<TranslatorLanguague>(tmpList);

                _TranslatorsLanguages.Add(MultillectLanguages);

                tmpList = Helper.LoadJsonData<List<TranslatorLanguague>>(GlobalSettings.DeeplLanguages);
                DeeplLanguages = new ReadOnlyCollection<TranslatorLanguague>(tmpList);

                _TranslatorsLanguages.Add(DeeplLanguages);

                _SourceLanguage = CurrentLanguages[0];
                _TargetLanguage = CurrentLanguages[1];
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        public string Transalte(string inSentence)
        {
            string result = String.Empty;

            switch (_CurrentTranslationEngine)
            {
                case TranslationEngine.GoogleTranslate:
                    {
                        result = GoogleTranslate(inSentence, _SourceLanguage.LanguageCode, _TargetLanguage.LanguageCode);
                        break;
                    }

                case TranslationEngine.Multillect:
                    {
                        result = MultillectTranslate(inSentence, _SourceLanguage.LanguageCode, _TargetLanguage.LanguageCode);
                        break;
                    }
                case TranslationEngine.DeepL:
                    {
                        result = DeeplTranslate(inSentence, _SourceLanguage.LanguageCode, _TargetLanguage.LanguageCode);
                        break;
                    }
            }
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
    }
}
