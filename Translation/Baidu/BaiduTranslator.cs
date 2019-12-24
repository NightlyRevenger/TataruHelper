// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Translation.Baidu
{
    class BaiduTranslater
    {
        string _Token;
        string _Gtk;

        BaiduReqEncoder _BaiduEncoder;
        WebApi.WebReader _BaiduWebRead;

        bool _IsInitialised = false;
        bool _InitializationFailed = false;

        ILog _Logger;

        public BaiduTranslater(ILog logger)
        {
            _Logger = logger;
            _BaiduWebRead = new WebApi.WebReader(@"fanyi.baidu.com", _Logger);

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
                    _BaiduEncoder = new BaiduReqEncoder(GlobalTranslationSettings.BaiduEncoder, _Logger);

                    string url = "https://fanyi.baidu.com/";

                    var tmpResult = _BaiduWebRead.GetWebData(url, WebApi.WebReader.WebMethods.GET);

                    tmpResult = _BaiduWebRead.GetWebDataAndSetCookie(url, WebApi.WebReader.WebMethods.GET);
                    Regex tokenRegex = new Regex("token: '(.*)'");
                    Regex gtkRegex = new Regex("gtk = '(.*)'");

                    Match tokenMatch = tokenRegex.Match(tmpResult);
                    Match gtkMatch = gtkRegex.Match(tmpResult);

                    if (tokenMatch.Success && gtkMatch.Success)
                    {
                        _Token = tokenMatch.Value;
                        _Gtk = gtkMatch.Value;
                        _Token = _Token.Substring(8, _Token.Length - 8).TrimEnd(new char[] { (char)39 });
                        _Gtk = _Gtk.Substring(7, _Gtk.Length - 7).TrimEnd(new char[] { (char)39 });

                        _IsInitialised = true;
                    }
                    else
                    {
                        _IsInitialised = false;
                        _InitializationFailed = true;
                    }
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
            string translationResult = String.Empty;

            string serviceUrl = @"https://fanyi.baidu.com/v2transapi";

            if (!_InitializationFailed)
            {
                if (!_IsInitialised)
                    SpinWait.SpinUntil(() => _IsInitialised || _InitializationFailed);

                if (!_InitializationFailed)
                {
                    try
                    {
                        string reqv = _BaiduEncoder.Encode(sentence, inLang, outLang, _Gtk, _Token);

                        var tmpResponse = _BaiduWebRead.GetWebData(serviceUrl, WebApi.WebReader.WebMethods.POST, reqv);

                        var unescaped = Regex.Unescape(tmpResponse);

                        var result = JsonConvert.DeserializeObject<BaiduResponse>(unescaped);

                        translationResult = result.trans_result.data[0].dst;
                    }
                    catch (Exception e)
                    {
                        _Logger?.WriteLog(e.ToString());
                    }
                }
            }

            return translationResult;
        }
    }

}
