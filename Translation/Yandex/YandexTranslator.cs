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

        YandexRequestsEncoder _RequestsEncoder;

        bool _IsInitialised = false;
        bool _InitializationFailed = false;

        YandexSession _YandexBaseSession;

        List<YandexSession> _YandexSessions = new List<YandexSession>();

        static string _Password = @"8c9c932bb1424b4c-a6f530447fe19972faafe633eff34d84";

        public YandexTranslator(ILog logger)
        {
            _Logger = logger;


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
                    _RequestsEncoder = new YandexRequestsEncoder(GlobalTranslationSettings.YandexEncoderPath, _Logger);

                    _YandexBaseSession = new YandexSession(_RequestsEncoder, null, _Logger);

                    var yandexAuthText = Helper.Base64Decode(File.ReadAllText(GlobalTranslationSettings.YandexAuthFile));
                    yandexAuthText = StringCipher.Decrypt(yandexAuthText, _Password);

                    List<YandexAuthContainer> yandexAuthContainers = JsonConvert.DeserializeObject<List<YandexAuthContainer>>(yandexAuthText);
                    yandexAuthContainers = yandexAuthContainers.Shuffle();

                    List<YandexSession> sessions = new List<YandexSession>();
                    foreach (var auth in yandexAuthContainers)
                    {
                        sessions.Add(new YandexSession(_RequestsEncoder, auth, _Logger));
                    }

                    _YandexSessions = sessions.Shuffle();

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
                        if (!_YandexBaseSession.IsBad)
                        {
                            result = _YandexBaseSession.Translate(sentence, inLang, outLang);

                            if (result == string.Empty)
                                _YandexBaseSession = new YandexSession(_RequestsEncoder, null, _Logger);

                            result = _YandexBaseSession.Translate(sentence, inLang, outLang);
                        }

                        if (result == string.Empty)
                        {
                            var seesion = _YandexSessions.FirstOrDefault(x => x.IsBad == false);

                            if (seesion != null)
                                result = seesion.Translate(sentence, inLang, outLang);
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
    }
}
