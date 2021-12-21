// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using HttpUtilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Translation.Deepl.Requests;
using Translation.Deepl.Responses;

namespace Translation.Deepl
{
    class DeepLTranslator
    {
        long _DeepLId;

        Random Rnd;

        HttpReader _DeeplReader;

        bool _ServerSplit;

        ILog _Logger;

        string deeplApiUrl = @"https://www2.deepl.com/jsonrpc";

        public DeepLTranslator(ILog logger)
        {
            _Logger = logger;

            _DeeplReader = null;

            Rnd = new Random((int)Math.Round(DateTime.Now.TimeOfDay.TotalMilliseconds));

            _ServerSplit = false;
        }

        private void CreateDeeplReader()
        {
            long baseIdMult = 10000;
            _DeeplReader = new HttpReader(new HttpUtils.HttpILogWrapper(_Logger));

            _DeeplReader.Referer = "https://www.deepl.com/translator";
            _DeeplReader.ContentType = "application/json";
            _DeeplReader.Accept = "*/*";
            _DeeplReader.OptionalHeaders.Add("Accept-Language", "en-US;q=0.5,en;q=0.3");
            _DeeplReader.OptionalHeaders.Add("DNT", "1");
            _DeeplReader.OptionalHeaders.Add("TE", "Trailers");

            _DeepLId = baseIdMult * (long)Math.Round((double)baseIdMult * Rnd.NextDouble());
        }

        public string Translate(string sentence, string inLang, string outLang)
        {
            string result = string.Empty;

            if (_DeeplReader == null)
                CreateDeeplReader();

            result = TranslateInternal(sentence, inLang, outLang);

            if (result == string.Empty)
            {
                CreateDeeplReader();

                result = TranslateInternal(sentence, inLang, outLang);

                if (result == string.Empty)
                    _DeeplReader = null;
            }

            return result;
        }

        string TranslateInternal(string sentence, string inLang, string outLang)
        {
            string result = String.Empty;

            if (inLang == outLang)
                return sentence;

            try
            {
                var reqv = new DeepLTranslatorRequest(_DeepLId, sentence, inLang, outLang);
                var reqvBody = reqv.ToJsonString();

                var strDeepLTranslationResponse = _DeeplReader.RequestWebData(deeplApiUrl, HttpMethods.POST, reqvBody, true);
                _DeepLId++;

                if (strDeepLTranslationResponse.IsSuccessful)
                {
                    var deepLTranslationResponse = JsonConvert.DeserializeObject<DeepLTranslationResponse>(strDeepLTranslationResponse.Body);

                    if (deepLTranslationResponse?.Result?.Translations != null)
                    {
                        StringBuilder temporaryResult = new StringBuilder();

                        List<Responses.Translation> translations = deepLTranslationResponse.Result.Translations;

                        foreach (var transaltion in translations)
                        {
                            var beam = transaltion.Beams.FirstOrDefault();
                            if (beam?.PostprocessedSentence != null)
                            {
                                temporaryResult.Append(beam.PostprocessedSentence);
                                temporaryResult.Append(" ");
                            }
                        }

                        result = temporaryResult.ToString();
                    }
                    else
                    {
                        _Logger?.WriteLog("Deepl strange response;");
                        _Logger?.WriteLog(strDeepLTranslationResponse?.Body ?? "Deepl body is null");
                    }
                }
                else
                {
                    _Logger?.WriteLog(strDeepLTranslationResponse?.InnerException?.ToString() ?? "Deepl Exception is null");
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

