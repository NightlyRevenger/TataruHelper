// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Translation.Papago
{
    class PapagoTranslator
    {
        HttpUtilities.HttpReader _PapagoReader;
        PapagoEncoder _PapagoEncoder = null;

        ILog _Logger;

        public PapagoTranslator(ILog logger)
        {
            _Logger = logger;

            CreatePapagoReader();
        }

        void CreatePapagoReader()
        {
            _PapagoReader = new HttpUtilities.HttpReader(new HttpUtils.HttpILogWrapper(_Logger));
            _PapagoReader.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
        }

        public string Translate(string sentence, string inLang, string outLang)
        {
            string result = string.Empty;

            result = TranslateInternal(sentence, inLang, outLang);

            if (result == string.Empty)
            {
                Thread.Sleep(2000);
                result = TranslateInternal(sentence, inLang, outLang);
            }

            return result;
        }

        string TranslateInternal(string sentence, string inLang, string outLang)
        {
            sentence = sentence.Replace(":", " : ");
            string result = string.Empty;

            string url = @"https://papago.naver.com/apis/n2mt/translate";

            if (_PapagoEncoder == null)
                _PapagoEncoder = new PapagoEncoder(GlobalTranslationSettings.PapagoEncoderPath, _Logger);

            /*
            if (inLang == "auto")
                inLang = DetectLanguage(sentence);//*/
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
                        locale = "ko-KR",
                        text = Uri.EscapeDataString(sentence)
                    };

                    var reqvObj = _PapagoEncoder.EncodePapagoTranslationRequest(papagoRequest);

                    if (reqvObj != null)
                    {
                        _PapagoReader.OptionalHeaders.Clear();

                        _PapagoReader.OptionalHeaders.Add("device-type", "pc");
                        _PapagoReader.OptionalHeaders.Add("x-apigw-partnerid", "papago");
                        _PapagoReader.OptionalHeaders.Add("Origin", @"https://papago.naver.com");
                        _PapagoReader.OptionalHeaders.Add("Sec-Fetch-Site", "same-origin");
                        _PapagoReader.OptionalHeaders.Add("Sec-Fetch-Mode", "cors");
                        _PapagoReader.OptionalHeaders.Add("Sec-Fetch-Dest", "empty");

                        _PapagoReader.OptionalHeaders.Add("Authorization", reqvObj.AuthorizationHeader);
                        _PapagoReader.OptionalHeaders.Add("Timestamp", reqvObj.Timestamp);

                        var requestBody = reqvObj.StringRequest + $"&authroization={Uri.EscapeDataString(reqvObj.AuthorizationHeader)}" + $"&timestamp={reqvObj.Timestamp}";

                        var papagoWebResponse = _PapagoReader.RequestWebData(url, HttpUtilities.HttpMethods.POST, requestBody, true);

                        if (papagoWebResponse.IsSuccessful)
                        {
                            PapagoResponse papagoResponse = JsonConvert.DeserializeObject<PapagoResponse>(papagoWebResponse.Body);

                            result = papagoResponse.translatedText;
                        }
                        else
                        {
                            CreatePapagoReader();

                            _Logger?.WriteLog(papagoWebResponse?.InnerException?.ToString() ?? "Papago Exception is null");
                        }

                    }
                    else
                    {
                        _Logger?.WriteLog("reqvObj == null");
                    }

                }
                catch (Exception e)
                {
                    _Logger?.WriteLog(e.ToString());
                }
            }

            if (result == null)
                result = string.Empty;

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
                throw new NotImplementedException($"{nameof(DetectLanguage)} is not implementd");
                /*
                try
                {
                    PapagoDetectLanguageRequest papagoRequest = new PapagoDetectLanguageRequest()
                    {
                        query = sentence
                    };

                    var reqv = _PapagoEncoder.EncodePapagoTranslationRequest(JsonConvert.SerializeObject(papagoRequest));

                    var tmpResponse = _PapagoReader.RequestWebData(url, HttpUtilities.HttpMethods.POST, reqv);

                    PapagoDetectLanguageResponse papagoResponse = JsonConvert.DeserializeObject<PapagoDetectLanguageResponse>(tmpResponse.Body);

                    result = papagoResponse.langCode;

                }
                catch (Exception e)
                {
                    _Logger?.WriteLog(e.ToString());
                }//*/
            }

            return result;
        }
    }
}
