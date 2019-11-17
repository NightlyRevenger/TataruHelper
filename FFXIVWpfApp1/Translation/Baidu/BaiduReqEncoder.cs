﻿using FFXIVTataruHelper.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVTataruHelper.Translation.Baidu
{
    class BaiduReqEncoder
    {
        static string savedHash = @"48d57033feee48be594d4161e0e1ec13927fe30772270ec2c7ecc8fc94b15a09";

        public bool IsAvaliable { get { return _IsAvaliable; } }

        bool _IsAvaliable = false;

        string ResourceFilePath = string.Empty;

        Jurassic.ScriptEngine BaiduEncoderEngine = null;

        public BaiduReqEncoder(string resourceFilePath)
        {
            _IsAvaliable = false;

            ResourceFilePath = resourceFilePath;
            Init();
        }

        private void Init()
        {
            var BaiduEncoderResourceBase64 = File.ReadAllText(ResourceFilePath);

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

                BaiduEncoderEngine = new Jurassic.ScriptEngine();
                BaiduEncoderEngine.Evaluate(BaiduEncoderJS);
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
                _IsAvaliable = false;
                return;
            }

            _IsAvaliable = true;
        }

        public string Encode(string sentence, string from, string to, string gtk, string token)
        {
            string reqv = string.Empty;

            if(_IsAvaliable && BaiduEncoderEngine!=null)
            {
                try
                {
                    string sign = BaiduEncoderEngine.CallGlobalFunction<string>("token", sentence, gtk);

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

                    reqv = WebApi.GetQueryString(request);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(e);
                    _IsAvaliable = false;
                }
            }

            return reqv;
        }
    }
}
