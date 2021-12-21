using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Translation.Yandex.API;

namespace Translation.Yandex
{
    class YandexRequestsEncoder
    {
        static string _Password = @"";
        static string _EncoderHash = @"46fa3599b9177021e64b0950a21ed92791fd4fba0c3ed50002e1998cabd41e59";

        public bool IsAvaliable { get { return _IsAvaliable; } }

        bool _IsAvaliable = false;

        string _ResourceFilePath = string.Empty;

        Jurassic.ScriptEngine _YandexEncoderEngine = null;

        ILog _Logger;

        public YandexRequestsEncoder(string resourceFilePath, ILog logger)
        {
            _Logger = logger;
            _IsAvaliable = false;

            _ResourceFilePath = resourceFilePath;

            Init();
        }

        private void Init()
        {
            try
            {
                var YandexEncoderResource = File.ReadAllText(_ResourceFilePath);

                bool IsHashCorrect = false;

                using (SHA256 sha256Hash = SHA256.Create())
                {
                    IsHashCorrect = Helper.VerifyHash(sha256Hash, YandexEncoderResource, _EncoderHash);
                }

                if (!IsHashCorrect)
                    return;

                string YandexEncoderResourceDecrypted = StringCipher.Decrypt(YandexEncoderResource, _Password);
                string YandexEncoderJS = Helper.Base64Decode(YandexEncoderResourceDecrypted);

                _YandexEncoderEngine = new Jurassic.ScriptEngine();
                _YandexEncoderEngine.Evaluate(YandexEncoderJS);

                _IsAvaliable = true;

            }
            catch (Exception e)
            {
                _Logger?.WriteLog(e.ToString());
                _IsAvaliable = false;
                return;
            }
        }

        public YandexRequest Encode(string sentence, string fromLang, string toLang, int requestNumber, string sid)
        {
            YandexRequest yandexRequest = null;

            lock (_YandexEncoderEngine)
            {
                var objectRes = _YandexEncoderEngine.CallGlobalFunction<Jurassic.Library.ObjectInstance>("EncodeRequest", requestNumber, sid, fromLang, toLang, sentence);

                if (objectRes == null)
                {
                    _IsAvaliable = false;
                }
                else
                {
                    var reqParams = objectRes.GetPropertyValue("resParams").ToString();
                    var reqBodey = objectRes.GetPropertyValue("trReqv").ToString();

                    yandexRequest = new YandexRequest()
                    {
                        BodyRequest = reqBodey,
                        UrlParams = reqParams
                    };

                }
            }

            return yandexRequest;
        }

    }
}
