// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVTataruHelper.Translation
{
    class PapagoTranslator
    {
        WebApi.WebReader PapagoReader;
        PapagoEncoder _PapagoEncoder = null;


        public PapagoTranslator()
        {
            PapagoReader = new WebApi.WebReader(@"papago.naver.com");
        }

        public string Translate(string sentence, string inLang, string outLang)
        {
            sentence=sentence.Replace(":"," : ");
            string result = string.Empty;
            string url = @"https://papago.naver.com/apis/n2mt/translate";

            if (_PapagoEncoder == null)
                _PapagoEncoder = new PapagoEncoder(GlobalSettings.PapagoEncoderPath);

            if (_PapagoEncoder.IsAvaliable)
            {
                try
                {
                    PapagoRequest papagoRequest = new PapagoRequest()
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

                    var reqv = _PapagoEncoder.EncodePapagoString(JsonConvert.SerializeObject(papagoRequest));

                    var tmpResponse = PapagoReader.GetWebData(url, WebApi.WebReader.WebMethods.POST, reqv);

                    PapagoResponse papagoResponse = JsonConvert.DeserializeObject<PapagoResponse>(tmpResponse);

                    result = papagoResponse.translatedText;
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                }
            }

            return result;
        }
    }
}
