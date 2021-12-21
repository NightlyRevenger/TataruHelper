// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace Translation.Baidu
{
    class BaiduReqEncoder
    {
        static string savedHash = @"48d57033feee48be594d4161e0e1ec13927fe30772270ec2c7ecc8fc94b15a09";

        public bool IsAvaliable { get { return _IsAvaliable; } }

        bool _IsAvaliable = false;

        string _ResourceFilePath = string.Empty;

        Jurassic.ScriptEngine _BaiduEncoderEngine = null;

        ILog _Logger;

        public BaiduReqEncoder(string resourceFilePath, ILog logger)
        {
            _Logger = logger;
            _IsAvaliable = false;

            _ResourceFilePath = resourceFilePath;
            Init();
        }

        private void Init()
        {
            var BaiduEncoderResourceBase64 = File.ReadAllText(_ResourceFilePath);

            bool IsHashCorrect = false;

            using (SHA256 sha256Hash = SHA256.Create())
            {
                IsHashCorrect = Helper.VerifyHash(sha256Hash, BaiduEncoderResourceBase64, savedHash);
            }

            if (!IsHashCorrect)
                return;

            try
            {
                string BaiduEncoderJS = Helper.Base64Decode(BaiduEncoderResourceBase64);

                _BaiduEncoderEngine = new Jurassic.ScriptEngine();
                _BaiduEncoderEngine.Evaluate(BaiduEncoderJS);
            }
            catch (Exception e)
            {
                _Logger?.WriteLog(e.ToString());
                _IsAvaliable = false;
                return;
            }

            _IsAvaliable = true;
        }

        public string Encode(string sentence, string from, string to, string gtk, string token)
        {
            string reqv = string.Empty;

            if(_IsAvaliable && _BaiduEncoderEngine!=null)
            {
                try
                {
                    string sign = _BaiduEncoderEngine.CallGlobalFunction<string>("token", sentence, gtk);

                    BaiduRequest request = new BaiduRequest
                    {
                        from = from,
                        to = to,
                        query = sentence,
                        transtype = "realtime",
                        simple_means_flag = "3",
                        sign = sign,
                        token = token
                    };

                    reqv = GetQueryString(request);
                }
                catch (Exception e)
                {
                    _Logger?.WriteLog(e.ToString());
                    _IsAvaliable = false;
                }
            }

            return reqv;
        }

        public static string GetQueryString(object obj)
        {
            var properties = from p in obj.GetType().GetProperties()
                             where p.GetValue(obj, null) != null
                             select p.Name + "=" + HttpUtility.UrlEncode(p.GetValue(obj, null).ToString());

            return String.Join("&", properties.ToArray());
        }
    }
}
