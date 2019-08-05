// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using Newtonsoft.Json;
using System;

namespace FFXIITataruHelper.Translation
{
    class DeepLTranslator
    {
        int _DeepLId;

        Random Rnd;

        WebApi.DeepLWebReader DeeplWebReader;

        bool _IsFirstTime;

        bool _ServerSplit;

        public DeepLTranslator()
        {
            DeeplWebReader = new WebApi.DeepLWebReader();

            Rnd = new Random(DateTime.Now.Millisecond);

            _DeepLId = Rnd.Next(33931525, 53931525);

            _IsFirstTime = true;

            _ServerSplit = false;
        }

        public string Translate(string sentence, string inLang, string outLang)
        {
            string result = String.Empty;

            try
            {
                string _outLang = outLang;
                string _inLang = inLang;

                if (_DeepLId > 83931525)
                {
                    _DeepLId = Rnd.Next(33931525, 53931525);
                    DeeplWebReader = new WebApi.DeepLWebReader();

                    _IsFirstTime = true;
                }

                if (_IsFirstTime)
                {
                    _IsFirstTime = false;

                    DeepLRequest.DeepLHandshakeRequest deepLHandshakeRequest = new DeepLRequest.DeepLHandshakeRequest(_DeepLId);

                    string handShakeUrl = @"https://www.deepl.com/PHP/backend/clientState.php?request_type=jsonrpc&il=RU";
                    string strDeepLHandshakeRequest = JsonConvert.SerializeObject(deepLHandshakeRequest);

                    var strDeepLHandshakeResp = DeeplWebReader.GetWebData(handShakeUrl, WebApi.WebReader.WebMethods.POST, strDeepLHandshakeRequest);
                    var DeepLHandshakeResp = JsonConvert.DeserializeObject<DeepLResponse.DeepLHandshakeResponse>(strDeepLHandshakeResp);

                    Logger.WriteLog(strDeepLHandshakeResp);

                    _DeepLId++;

                    _IsFirstTime = false;
                }

                string url = @"https://www2.deepl.com/jsonrpc";

                if (_ServerSplit)
                {
                    DeepLRequest.DeepLCookieRequest deepLSentenceRequest = new DeepLRequest.DeepLCookieRequest(_DeepLId, sentence);
                    string strDeepLsentenceRequest = deepLSentenceRequest.ToJsonString();

                    var strDeepLSentencetResp = DeeplWebReader.GetWebData(url, WebApi.WebReader.WebMethods.POST, strDeepLsentenceRequest);
                    var deepLSentenceResp = JsonConvert.DeserializeObject<DeepLResponse.DeepLSentencePreprocessResponse>(strDeepLSentencetResp);

                    _DeepLId++;
                }

                DeepLRequest.DeepLTranslatorRequest deepLTranslationRequest = new DeepLRequest.DeepLTranslatorRequest(_DeepLId, sentence, inLang, outLang);
                string strDeepLTranslationRequest = deepLTranslationRequest.ToJsonString();

                var strDeepLTranslationResponse = DeeplWebReader.GetWebData(url, WebApi.WebReader.WebMethods.POST, strDeepLTranslationRequest);
                _DeepLId++;

                var DeepLTranslationResponse = JsonConvert.DeserializeObject<DeepLResponse.DeepLTranslationResponse>(strDeepLTranslationResponse);

                string temporaryResult = String.Empty;
                if (DeepLTranslationResponse != null)
                {
                    var translations = DeepLTranslationResponse.result.translations;
                    for (int i = 0; i < translations.Count; i++)
                    {
                        if (translations[i].beams.Count > 0)
                            temporaryResult += " " + translations[i].beams[0].postprocessed_sentence;
                    }
                }

                result = temporaryResult;

            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }

            return result;
        }
    }
}
