// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translation.Papago
{
    class PapagoTranslator
    {
        WebApi.WebReader PapagoReader;
        PapagoEncoder _PapagoEncoder = null;

        ILog _Logger;

        public PapagoTranslator(ILog logger)
        {
            _Logger = logger;

            PapagoReader = new WebApi.WebReader(@"papago.naver.com", _Logger);
        }

        public string Translate(string sentence, string inLang, string outLang)
        {
            sentence = sentence.Replace(":", " : ");
            string result = string.Empty;
            string url = @"https://papago.naver.com/apis/n2mt/translate";

            if (_PapagoEncoder == null)
                _PapagoEncoder = new PapagoEncoder(GlobalTranslationSettings.PapagoEncoderPath, _Logger);

            if (inLang == "auto")
                inLang = DetectLanguage(sentence);
            if (inLang.Length == 0)
                return result;

            if (_PapagoEncoder.IsAvaliable)
            {
                try
                {
                    PapagoTranslationRequest papagoRequest = new PapagoTranslationRequest()
                    {
                        deviceId = "",
                        dict = false,
                        dictDisplay = 0,
                        honorific = false,
                        instant = false,
                        paging = false,
                        source = inLang,
                        target = outLang,
                        text = sentence
                    };

                    var reqv = _PapagoEncoder.EncodePapagoTranslationRequest(JsonConvert.SerializeObject(papagoRequest));

                    var tmpResponse = PapagoReader.GetWebData(url, WebApi.WebReader.WebMethods.POST, reqv);

                    PapagoResponse papagoResponse = JsonConvert.DeserializeObject<PapagoResponse>(tmpResponse);

                    result = papagoResponse.translatedText;
                }
                catch (Exception e)
                {
                    _Logger?.WriteLog(e.ToString());
                }
            }

            return result;
        }

        string DetectLanguage(string sentence)
        {
            string result = string.Empty;
            string url = @"https://papago.naver.com/apis/langs/dect";

            if (_PapagoEncoder == null)
                _PapagoEncoder = new PapagoEncoder(GlobalTranslationSettings.PapagoEncoderPath, _Logger);

            if (_PapagoEncoder.IsAvaliable)
            {
                try
                {
                    PapagoDetectLanguageRequest papagoRequest = new PapagoDetectLanguageRequest()
                    {
                        query = sentence
                    };

                    var reqv = _PapagoEncoder.EncodePapagoTranslationRequest(JsonConvert.SerializeObject(papagoRequest));

                    var tmpResponse = PapagoReader.GetWebData(url, WebApi.WebReader.WebMethods.POST, reqv);

                    PapagoDetectLanguageResponse papagoResponse = JsonConvert.DeserializeObject<PapagoDetectLanguageResponse>(tmpResponse);

                    result = papagoResponse.langCode;

                }
                catch (Exception e)
                {
                    _Logger?.WriteLog(e.ToString());
                }
            }

            return result;
        }
    }
}
