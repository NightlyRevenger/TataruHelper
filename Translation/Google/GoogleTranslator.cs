// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using HttpUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Translation.Google
{
    class GoogleTranslator
    {
        ILog _Logger;

        HttpReader _GoogleWebReader;

        static Regex GoogleRxLeagacy = new Regex("(?<=(<div dir=\"ltr\" class=\"t0\">)).*?(?=(<\\/div>))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static Regex GoogleRx = new Regex("(?<=(<div(.*)class=\"result-container\"(.*)>)).*?(?=(<\\/div>))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        string googleHost = @"translate.google.com";

        public GoogleTranslator(ILog logger)
        {
            _GoogleWebReader = null;
            _Logger = logger;
        }

        void CreateGoogleReader()
        {
            _GoogleWebReader = new HttpUtilities.HttpReader(new HttpUtils.HttpILogWrapper(_Logger));

            _GoogleWebReader.UserAgent = "Opera/9.80 (Android; Opera Mini/11.0.1912/37.7549; U; pl) Presto/2.12.423 Version/12.16";
            _GoogleWebReader.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            _GoogleWebReader.ContentType = null;

            _GoogleWebReader.OptionalHeaders.Add("Accept-Language", "en-US;q=0.5,en;q=0.3");
            _GoogleWebReader.OptionalHeaders.Add("DNT", "1");
            _GoogleWebReader.OptionalHeaders.Add("Upgrade-Insecure-Requests", "1");

            _GoogleWebReader.OptionalHeaders.Add("Pragma", "no-cache");
            _GoogleWebReader.OptionalHeaders.Add("Cache-Control", "no-cache");

            var requestResult = _GoogleWebReader.RequestWebData("https://translate.google.com/m", HttpUtilities.HttpMethods.GET, true);
        }

        public string Translate(string sentence, string inLang, string outLang)
        {
            string result = String.Empty;

            if (_GoogleWebReader == null)
                CreateGoogleReader();

            result = TranslateInternal(sentence, inLang, outLang);

            if (result == string.Empty)
            {
                CreateGoogleReader();
                result = TranslateInternal(sentence, inLang, outLang);
            }

            return result;
        }

        private string TranslateInternal(string sentence, string inLang, string outLang)
        {
            string result = String.Empty;

            try
            {
                string _outLang = outLang;
                string _inLang = inLang;

                string _baseUrl = "https://translate.google.com/m?hl=ru&sl={0}&tl={1}&ie=UTF-8&prev=_m&q={2}";
                string url = string.Format(_baseUrl, _inLang, _outLang, HttpUtility.UrlEncode(sentence));

                var requestResult = _GoogleWebReader.RequestWebData(url, HttpUtilities.HttpMethods.GET, true);

                if (requestResult.IsSuccessful)
                {
                    var rxMatch = GoogleRxLeagacy.Match(requestResult.Body);

                    if (rxMatch.Success)
                    {
                        result = System.Net.WebUtility.HtmlDecode(rxMatch.Value);
                    }
                    else
                    {
                        rxMatch = GoogleRx.Match(requestResult.Body);

                        if (rxMatch.Success)
                        {
                            result = System.Net.WebUtility.HtmlDecode(rxMatch.Value);
                        }
                        else
                        {
                            _Logger?.WriteLog(requestResult.Body ?? "Google body response is null");
                        }
                    }
                }
                else
                {
                    _Logger?.WriteLog(requestResult?.InnerException?.ToString() ?? "Google Exception is null");
                }
            }
            catch (Exception e)
            {
                _Logger?.WriteLog(Convert.ToString(e));
            }

            return result;
        }
    }
}
