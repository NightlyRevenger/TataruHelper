// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translation.Yandex
{
    class YandexTranslator
    {
        ILog _Logger;

        WebApi.WebReader _YandexWebReader;

        public YandexTranslator(ILog logger)
        {
            _Logger = logger;

            _YandexWebReader = new WebApi.WebReader(@"translate.yandex.net", _Logger);
        }

        public string Translate(string sentence, string inLang, string outLang)
        {
            string result = String.Empty;
            try
            {
                string yaApiKey = @"";
                string _outLang = outLang;
                string _inLang = inLang;

                string _baseUrl = @"https://translate.yandex.net/api/v1.5/tr.json/translate?lang={0}-{1}&key={2}";
                string url = string.Format(_baseUrl, _inLang, _outLang, yaApiKey);

                var tmpResult = _YandexWebReader.GetWebData(url, WebApi.WebReader.WebMethods.POST, "text=" + sentence);

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
                _Logger?.WriteLog(Convert.ToString(e));
            }

            return result;
        }
    }
}
