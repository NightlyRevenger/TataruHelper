// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Translation.Google
{
    class GoogleTranslator
    {
        ILog _Logger;

        WebApi.WebReader _GoogleWebReader;

        private Regex GoogleRx;

        public GoogleTranslator(ILog logger)
        {
            _Logger = logger;

            _GoogleWebReader = new WebApi.WebReader(@"translate.google.com", _Logger);

            string pattern = "(?<=(<div dir=\"ltr\" class=\"t0\">)).*?(?=(<\\/div>))";

            GoogleRx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public string Translate(string sentence, string inLang, string outLang)
        {
            string result = String.Empty;

            try
            {
                string _outLang = outLang;
                string _inLang = inLang;

                string _baseUrl = "https://translate.google.com/m?hl=ru&sl={0}&tl={1}&ie=UTF-8&prev=_m&q={2}";
                string url = string.Format(_baseUrl, _inLang, _outLang, sentence);

                var tmpResult = _GoogleWebReader.GetWebData(url, WebApi.WebReader.WebMethods.GET);

                var rxMatch = GoogleRx.Match(tmpResult);

                if (rxMatch.Success)
                {
                    result = System.Net.WebUtility.HtmlDecode(rxMatch.Value);
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
