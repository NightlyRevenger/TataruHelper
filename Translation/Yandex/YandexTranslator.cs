// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Translation.Yandex
{
    class YandexTranslator
    {
        ILog _Logger;

        HttpUtilities.HttpReader _YandexWebReader;

        YandexRequestsEncoder _RequestsEncoder;

        bool _IsInitialised = false;
        bool _InitializationFailed = false;

        public YandexTranslator(ILog logger)
        {
            _Logger = logger;

            _YandexWebReader = new HttpUtilities.HttpReader(@"translate.yandex.net", new HttpUtils.HttpILogWrapper(_Logger));

            InitTranslator();
        }

        private void InitTranslator()
        {
            _IsInitialised = false;
            _InitializationFailed = false;

            Task.Run(() =>
            {
                try
                {
                    _RequestsEncoder = new YandexRequestsEncoder(GlobalTranslationSettings.YandexEncoderPath, GlobalTranslationSettings.YandexAuthFile, _Logger);

                    _IsInitialised = true;
                }
                catch (Exception e)
                {
                    _InitializationFailed = true;
                    _IsInitialised = false;
                    _Logger?.WriteLog(e.ToString());
                }
            });
        }


        public string Translate(string sentence, string inLang, string outLang)
        {
            string result = string.Empty;

            if (!_InitializationFailed)
            {
                if (!_IsInitialised)
                    SpinWait.SpinUntil(() => _IsInitialised || _InitializationFailed);

                if (!_InitializationFailed)
                {
                    if (!_RequestsEncoder.IsAvaliable)
                        return string.Empty;

                    try
                    {

                        var request = _RequestsEncoder.Encode(sentence, inLang, outLang);

                        if (request != null)
                        {

                            var reqvUrl = string.Format("https://translate.yandex.net/api/v1/tr.json/translate?{0}", request.UrlParams);
                            var requestBodey = string.Format("text={0}&options=4", Uri.EscapeDataString(sentence));

                            //var response = _YandexWebReader.RequestWebData(reqvUrl, HttpUtilities.HttpMethods.POST, request.BodyRequest);
                            var response = _YandexWebReader.RequestWebData(reqvUrl, HttpUtilities.HttpMethods.POST, requestBodey);

                            if (response.IsSuccessful)
                            {
                                try
                                {
                                    var translationResponse = JsonConvert.DeserializeObject<TranslationResponse>(response.Body);

                                    if (translationResponse?.Text != null && translationResponse.Text.Count > 0)
                                    {
                                        foreach (var str in translationResponse.Text)
                                            result += str + " ";
                                    }
                                }
                                catch (Exception e)
                                {
                                    request.YandexSession.IsBad = true;
                                }
                            }
                            else
                                request.YandexSession.IsBad = true;

                            if (result.Length < 1 && request?.YandexSession != null)
                            {
                                request.YandexSession.IsBad = true;
                            }
                            if (result.Length > 1)
                            {
                                result = result.Replace(":", ": ");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _Logger?.WriteLog(ex?.ToString() ?? "Exception is null");
                    }
                }
            }

            return result;
        }

        public string TranslateOld(string sentence, string inLang, string outLang)
        {
            string result = String.Empty;
            try
            {
                string yaApiKey = @"trnsl.1.1.20190204T134422Z.621647c1cffc2039.c2005599368f64d39003df38affa93a62699bcfe";
                string _outLang = outLang;
                string _inLang = inLang;

                string _baseUrl = @"https://translate.yandex.net/api/v1.5/tr.json/translate?lang={0}-{1}&key={2}";
                string url = string.Format(_baseUrl, _inLang, _outLang, yaApiKey);

                var tmpResult = _YandexWebReader.RequestWebData(url, HttpUtilities.HttpMethods.POST, "text=" + sentence);

                var resp = JsonConvert.DeserializeObject<YandexResponse>(tmpResult.Body);

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
