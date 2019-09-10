﻿using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace FFXIVTataruHelper.Translation.Baidu
{
    class BaiduTranslater
    {
        private string token;
        private string gtk;
        private BaiduReqEncoder encoder = new BaiduReqEncoder();

        WebApi.WebReader BaiduWebRead;
        public BaiduTranslater()
        {
            try
            {
                string url = "https://fanyi.baidu.com/";

                BaiduWebRead = new WebApi.WebReader(@"fanyi.baidu.com");
                var tmpResult = BaiduWebRead.GetWebData(url, WebApi.WebReader.WebMethods.GET);

                tmpResult = BaiduWebRead.GetWebDataAndSetCookie(url, WebApi.WebReader.WebMethods.GET);
                Regex tokenRegex = new Regex("token: '(.*)'");
                Regex gtkRegex = new Regex("gtk = '(.*)'");

                var tokenMatch = tokenRegex.Match(tmpResult);
                var gtkMatch = gtkRegex.Match(tmpResult);

                if (tokenMatch.Success && gtkMatch.Success)
                {
                    token = tokenMatch.Value;
                    gtk = gtkMatch.Value;
                    token = token.Substring(8, token.Length - 8).TrimEnd(new char[] { (char)39 });
                    gtk = gtk.Substring(7, gtk.Length - 7).TrimEnd(new char[] { (char)39 });
                }
                string s = "In order to resolve this, Kindly go to the below path";
                Translate(s, "en", "zh");
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        public string Translate(string sentence, string inLang, string outLang)
        {
            string reqv = encoder.encoding(sentence, inLang, outLang, gtk, token);

            var tmpResponse = BaiduWebRead.GetWebData("https://fanyi.baidu.com/v2transapi", WebApi.WebReader.WebMethods.POST, reqv);

            var unescaped = Regex.Unescape(tmpResponse);

            var result = JsonConvert.DeserializeObject<BaiduResponse>(unescaped);

            return result.trans_result.data[0].dst;
        }
    }

}
