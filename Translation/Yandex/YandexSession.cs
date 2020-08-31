using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Translation.Yandex
{
    class YandexSession
    {
        YandexAuthContainer _YandexAcc;

        public string SID
        {
            get
            {
                if (_sid != null)
                    return _sid;

                _sid = GetSid();

                return _sid;
            }
        }

        public bool IsAvailable { get => SID != null && _IsAvailable && IsInitialized; }

        public bool IsInitialized { get => _IsInitialized; }

        public bool IsBad { get; set; }

        static Regex windowConfigRegex = new Regex(@"window.config(\s)*=(\s)*{");


        HttpUtilities.HttpReader _YandexWebReader = null;

        string yandexUrl = "https://translate.yandex.ru/";

        string _sid = null;

        bool _IsInitialized = false;

        ILog _Logger = null;

        int _requestNUmber = 1;

        bool _IsAvailable = true;

        public YandexSession(YandexAuthContainer authContainer, ILog logger)
        {
            _Logger = logger;

            _YandexAcc = authContainer;

            InitializeSession();
        }

        public void ReinitializeSession()
        {
            InitializeSession();
        }

        void InitializeSession()
        {
            IsBad = false;

            _YandexWebReader = new HttpUtilities.HttpReader(new Uri(yandexUrl).Host, LoadCookies(_YandexAcc), null, new HttpUtils.HttpILogWrapper(_Logger));

            _YandexWebReader.Accept = _YandexAcc.Accept;
            _YandexWebReader.ContentType = "application/x-www-form-urlencoded";
            _YandexWebReader.UserAgent = _YandexAcc.UserAgent;

            _IsAvailable = true;
            _IsInitialized = false;

            if (_YandexAcc.AcceptLanguage != null)
                _YandexWebReader.OptionalHeaders.Add("Accept-Language", _YandexAcc.AcceptLanguage);
            /*
            if (_YandexAcc.AcceptEncoding != null)
                _YandexWebReader.OptionalHeaders.Add("Accept-Encoding", _YandexAcc.AcceptEncoding);//*?

            /*
            if (_YandexAcc.Referer != null)
                _YandexWebReader.OptionalHeaders.Add("Referer", _YandexAcc.Referer);//*/
        }

        public YandexRequest CreateRequest(Jurassic.ScriptEngine yandexEncoderEngine, string toTranslate, string fromLang, string toLang)
        {
            YandexRequest yandexRequest = null;

            var sid = this.SID;

            var objectRes = yandexEncoderEngine.CallGlobalFunction<Jurassic.Library.ObjectInstance>("EncodeRequest", _requestNUmber, sid, fromLang, toLang, toTranslate);

            if (objectRes == null)
            {
                _IsAvailable = false;
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

            _requestNUmber++;
            return yandexRequest;
        }

        System.Net.CookieContainer LoadCookies(YandexAuthContainer authContainer)
        {
            System.Net.CookieContainer globalCookie = new System.Net.CookieContainer();
            var yandexUri = new Uri(yandexUrl);

            foreach (var cookie in authContainer.Cookies)
            {
                if (cookie.Key != null && cookie.Value != null)
                {

                    try
                    {
                        globalCookie.Add(yandexUri, new System.Net.Cookie(cookie.Key, cookie.Value));
                    }
                    catch
                    {
                        try
                        {

                            globalCookie.Add(yandexUri, new System.Net.Cookie(cookie.Key, System.Web.HttpUtility.UrlEncode(cookie.Value)));
                        }
                        catch { }
                    }
                }
            }

            return globalCookie;
        }

        string GetSid()
        {
            string resultSid = null;

            _IsInitialized = true;

            try
            {
                var mainPage = _YandexWebReader.RequestWebData(yandexUrl, HttpUtilities.HttpMethods.GET, true);

                if (mainPage.IsSuccessful)
                    resultSid = ParseSidFromPage(mainPage.Body);
            }
            catch (Exception ex)
            {
                _Logger?.WriteLog(ex?.ToString() ?? "Exception is null");
            }


            return resultSid;
        }

        string ParseSidFromPage(string page)
        {
            var match = windowConfigRegex.Match(page);

            string sid = null;

            if (match.Success && match.Index >= 0)
            {
                var pageConfigIndex = match.Index;

                var sidKeyStart = page.IndexOf("sid", pageConfigIndex, StringComparison.InvariantCultureIgnoreCase);

                var sidKeyEnd = page.IndexOf("\n", sidKeyStart);

                var sidTmp = page.Substring(sidKeyStart, sidKeyEnd - sidKeyStart);

                var sidStart = sidTmp.IndexOf("'") + 1;
                var sidEnd = sidTmp.LastIndexOf("'");

                sid = sidTmp.Substring(sidStart, sidEnd - sidStart);
            }

            return sid;
        }
    }
}
