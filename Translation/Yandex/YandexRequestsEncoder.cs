using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Translation.Yandex
{
    class YandexRequestsEncoder
    {
        static string _Password = @"";
        static string _EncoderHash = @"";
        static string _AuthHash = @"a1f5408dde806160ef8c0ed74dcf8c81b794eb69dcd224a8ac34c4c403f90f0f";

        public bool IsAvaliable { get { return _IsAvaliable; } }

        bool _IsAvaliable = false;

        string _ResourceFilePath = string.Empty;

        string _AccountsFilePath = string.Empty;

        Jurassic.ScriptEngine _YandexEncoderEngine = null;

        List<YandexSession> _YandexSessions = new List<YandexSession>();

        YandexSession _LastUsedSession = null;

        ILog _Logger;

        public YandexRequestsEncoder(string resourceFilePath, string accountsFilePath, ILog logger)
        {
            _Logger = logger;
            _IsAvaliable = false;

            _ResourceFilePath = resourceFilePath;
            _AccountsFilePath = accountsFilePath;

            Init();
        }

        private void Init()
        {
            try
            {
                var YandexEncoderResource = File.ReadAllText(_ResourceFilePath);
                var YandexAuthResource = File.ReadAllText(_AccountsFilePath);

                bool IsHashCorrect = false;

                using (SHA256 sha256Hash = SHA256.Create())
                {
                    IsHashCorrect = Helper.VerifyHash(sha256Hash, YandexEncoderResource, _EncoderHash);
                    IsHashCorrect = IsHashCorrect && Helper.VerifyHash(sha256Hash, YandexAuthResource, _AuthHash);
                }

                if (!IsHashCorrect)
                    return;

                string YandexEncoderResourceDecrypted = StringCipher.Decrypt(YandexEncoderResource, _Password);
                string YandexEncoderJS = Helper.Base64Decode(YandexEncoderResourceDecrypted);

                _YandexEncoderEngine = new Jurassic.ScriptEngine();
                _YandexEncoderEngine.Evaluate(YandexEncoderJS);


                string yandexAuthResourceDecrypted = StringCipher.Decrypt(YandexAuthResource, _Password);
                string yandexAuthJson = Helper.Base64Decode(yandexAuthResourceDecrypted);

                var yandexAccs = JsonConvert.DeserializeObject<List<YandexAuthContainer>>(yandexAuthJson);

                List<YandexSession> sessions = new List<YandexSession>();
                foreach (var acc in yandexAccs)
                    sessions.Add(new YandexSession(acc, _Logger));

                _YandexSessions = sessions.Shuffle();

                _IsAvaliable = true;

            }
            catch (Exception e)
            {
                _Logger?.WriteLog(e.ToString());
                _IsAvaliable = false;
                return;
            }
        }

        public YandexRequest Encode(string sentence, string from, string to)
        {
            YandexSession seesion = null;
            YandexRequest request = null;

            if (_LastUsedSession != null && _LastUsedSession.IsBad == false)
                _LastUsedSession = null;

            if (_YandexSessions.All(s => s.IsBad))
            {
                foreach (var s in _YandexSessions)
                    s.ReinitializeSession();
            }

            if (_LastUsedSession == null)
            {
                foreach (var s in _YandexSessions)
                {
                    if (s.IsBad)
                        continue;

                    if (s.IsInitialized && s.IsAvailable)
                    {
                        seesion = s;
                        break;
                    }
                    else if (!s.IsInitialized)
                    {
                        if (s.SID != null)
                        {
                            seesion = s;
                            break;
                        }
                    }
                }
            }
            else
            {
                seesion = _LastUsedSession;
            }

            if (seesion != null && seesion?.SID != null)
            {
                request = seesion.CreateRequest(_YandexEncoderEngine, sentence, from, to);
                request.YandexSession = seesion;
                _LastUsedSession = seesion;

            }

            return request;
        }
    }
}
