// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System;

namespace Translation.Multillect
{
    class MultillectTranslator
    {
        ILog _Logger;

        WebApi.WebReader _MultillectWebReader;

        public MultillectTranslator(ILog logger)
        {
            _Logger = logger;
            _MultillectWebReader = new WebApi.WebReader(@"translate.multillect.com", _Logger);
        }

        public string Translate(string sentence, string inLang, string outLang)
        {
            string result = String.Empty;
            try
            {
                string _outLang = outLang;
                string _inLang = inLang;

                string _baseUrl = @"https://translate.multillect.com/form.json?from={0}&to={1}&text={2}";
                string url = string.Format(_baseUrl, _inLang, _outLang, sentence);

                var tmpResult = _MultillectWebReader.GetWebData(url, WebApi.WebReader.WebMethods.GET);

                var resp = JsonConvert.DeserializeObject<MultillectResponse.MultillectRoot>(tmpResult);

                result = resp.result.translations;
            }
            catch (Exception e)
            {
                _Logger?.WriteLog(Convert.ToString(e));
            }

            return result;
        }

    }
}
