// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Translation.Papago
{
    class PapagoEncoder
    {
        static string password = @"";
        static string savedHash = @"";

        public bool IsAvaliable { get { return _IsAvaliable; } }

        bool _IsAvaliable = false;

        string ResourceFilePath = string.Empty;

        Jurassic.ScriptEngine PapgoEncoderEngine = null;

        ILog _Logger;

        public PapagoEncoder(string resourceFilePath, ILog logger)
        {
            _Logger = logger;

            _IsAvaliable = false;

            ResourceFilePath = resourceFilePath;
            Init();
        }

        private void Init()
        {
            var PapagoEncoderResource = File.ReadAllText(ResourceFilePath);

            bool IsHashCorrect = false;
            using (SHA256 sha256Hash = SHA256.Create())
            {
                IsHashCorrect = Helper.VerifyHash(sha256Hash, PapagoEncoderResource, savedHash);
            }

            if (!IsHashCorrect)
                return;
            try
            {
                string PapagoEncoderResourceDecrypted = StringCipher.Decrypt(PapagoEncoderResource, password);
                string PapagoEncoderJS = Helper.Base64Decode(PapagoEncoderResourceDecrypted);

                PapgoEncoderEngine = new Jurassic.ScriptEngine();
                PapgoEncoderEngine.Evaluate(PapagoEncoderJS);
            }
            catch (Exception e)
            {
                _Logger?.WriteLog(e.ToString());
                _IsAvaliable = false;
                return;
            }

            _IsAvaliable = true;
        }

        public string EncodePapagoTranslationRequest(string str)
        {
            string reqv = string.Empty;

            if (_IsAvaliable && PapgoEncoderEngine != null)
            {
                try
                {
                    reqv = PapgoEncoderEngine.CallGlobalFunction<string>("EncodeTransaltionRequest", str);

                    reqv = "data=" + System.Net.WebUtility.UrlEncode(reqv);

                }
                catch (Exception e)
                {
                    _Logger?.WriteLog(e.ToString());
                    _IsAvaliable = false;
                }
            }

            return reqv;
        }

        public string EncodePapagoString(string str)
        {
            string reqv = string.Empty;

            if (_IsAvaliable && PapgoEncoderEngine != null)
            {
                try
                {
                    reqv = PapgoEncoderEngine.CallGlobalFunction<string>("EncodeRequest", str);

                    reqv = "data=" + System.Net.WebUtility.UrlEncode(reqv);

                }
                catch (Exception e)
                {
                    _Logger?.WriteLog(e.ToString());
                    _IsAvaliable = false;
                }
            }

            return reqv;
        }

    }
}
