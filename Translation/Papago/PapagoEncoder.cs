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
        static string savedHash = @"c7e0e74290b52beb135a108ea181c6633c38a37f9ee136c457e48cfc2c14d881";

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

        public PapagoSerializedRequest EncodePapagoTranslationRequest(PapagoTranslationRequest translationRequest)
        {
            PapagoSerializedRequest reqv = null;

            if (_IsAvaliable && PapgoEncoderEngine != null)
            {
                try
                {
                    string pageoReqv = PapgoEncoderEngine.CallGlobalFunction<string>("EncodeTransaltionRequest", JsonConvert.SerializeObject(translationRequest));

                    PapagoEncodedRequest encodedRequestObj = JsonConvert.DeserializeObject<PapagoEncodedRequest>(pageoReqv);

                    string signString = PapagoHmacFin(encodedRequestObj.HmacInput, encodedRequestObj.HmacKey);

                    string authorizationHeader = $"PPG {encodedRequestObj.Guid}:{signString}";

                    return new PapagoSerializedRequest()
                    {
                        AuthorizationHeader = authorizationHeader,
                        StringRequest = encodedRequestObj.EncodedTranslationRequest,
                        Timestamp = encodedRequestObj.GuidTime
                    };

                }
                catch (Exception e)
                {
                    _Logger?.WriteLog(e.ToString());
                    _IsAvaliable = false;
                }
            }

            return reqv;
        }

        static string PapagoHmacFin(string plaintext, string transactionKey)
        {
            var data = Encoding.UTF8.GetBytes(plaintext);
            // key
            var key = Encoding.UTF8.GetBytes(transactionKey);

            // Create HMAC-MD5 Algorithm;
            using (HMACMD5 hmac = new HMACMD5(key))
            {
                // Compute hash.
                var hashBytes = hmac.ComputeHash(data);

                return Convert.ToBase64String(hashBytes);
            }
        }

    }
}
