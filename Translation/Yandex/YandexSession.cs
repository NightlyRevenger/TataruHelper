using IvanAkcheurov.Commons;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Translation.Yandex.API;

namespace Translation.Yandex
{
    class YandexSession
    {
        public bool IsInitialized { get => _IsInitialized; }

        public bool IsBad { get => _IsBad; }

        HttpUtilities.HttpReader _YandexWebReader;

        YandexRequestsEncoder _RequestsEncoder;

        YandexAuthContainer _YandexAuth;

        bool _IsInitialized = false;
        bool _IsBad = false;

        string _sid = null;

        string yandexTranslateUrl = "https://translate.yandex.ru/";

        static Regex windowConfigRegex = new Regex(@"window.config(\s)*=(\s)*{", RegexOptions.Compiled);

        ILog _Logger = null;

        int _requestNUmber = 1;

        public YandexSession(YandexRequestsEncoder requestsEncoder, YandexAuthContainer yandexAuth = null, ILog logger = null)
        {
            _Logger = logger;

            this._RequestsEncoder = requestsEncoder;

            this._YandexAuth = yandexAuth;

            _IsInitialized = false;
            _IsBad = false;
        }

        void InitializeSession()
        {
            _IsBad = false;

            CreateYandexReader();

            _sid = GetSid();

            _IsInitialized = true;

            if (_sid == null)
                _IsBad = true;
        }

        public string Translate(string textToTranslate, string fromLang, string toLang)
        {
            string result = string.Empty;

            try
            {
                if (!IsBad && !IsInitialized)
                {
                    InitializeSession();
                }

                if (!IsBad && IsInitialized)
                {
                    result = TranslateInternal(textToTranslate, fromLang, toLang);
                }
            }
            catch (Exception ex)
            {
                _IsBad = true;
                _Logger?.WriteLog(ex?.ToString() ?? "Exception in yandex is null");
            }

            if (result == string.Empty)
                _IsBad = true;

            return result;
        }

        public string TranslateInternal(string textToTranslate, string fromLang, string toLang)
        {
            string result = string.Empty;

            var request = _RequestsEncoder.Encode(textToTranslate, fromLang, toLang, _requestNUmber, _sid);

            var reqvUrl = string.Format("https://translate.yandex.net/api/v1/tr.json/translate?{0}", request.UrlParams);
            var requestBody = string.Format("text={0}&options=4", Uri.EscapeDataString(textToTranslate));

            _YandexWebReader.ContentType = "application/x-www-form-urlencoded";
            var yandexRsponse = _YandexWebReader.RequestWebData(reqvUrl, HttpUtilities.HttpMethods.POST, requestBody, true);
            _requestNUmber++;

            if (yandexRsponse.IsSuccessful)
            {
                try
                {
                    var translationResponse = JsonConvert.DeserializeObject<TranslationResponse>(yandexRsponse.Body);

                    if (translationResponse?.Text != null && translationResponse.Text.Count > 0)
                    {
                        var translationResult = new StringBuilder();

                        foreach (var str in translationResponse.Text)
                        {
                            translationResult.Append(str);
                            translationResult.Append(" ");
                        }

                        result = translationResult.ToString().Trim();
                    }
                }
                catch (Exception ex)
                {
                    _Logger?.WriteLog(ex?.ToString() ?? "Exception in yandex is null");
                }
            }
            else
            {
                _Logger?.WriteLog(yandexRsponse?.InnerException?.ToString() ?? "Yandex Exception is null");
            }

            if (result.Length > 1)
            {
                result = result.Replace(":", ": ");
            }

            return result;
        }

        void CreateYandexReader()
        {
            _YandexWebReader = new HttpUtilities.HttpReader();

            _YandexWebReader.UserAgent = "Mozilla/5.0 (Windows NT 6.2; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36";
            _YandexWebReader.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";

            _YandexWebReader.OptionalHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            _YandexWebReader.OptionalHeaders.Add("DNT", "1");
            _YandexWebReader.OptionalHeaders.Add("Upgrade-Insecure-Requests", "1");

            _YandexWebReader.OptionalHeaders.Add("Pragma", "no-cache");
            _YandexWebReader.OptionalHeaders.Add("Cache-Control", "no-cache");

            _YandexWebReader.OptionalHeaders.Add("Sec-Fetch-Site", "none");
            _YandexWebReader.OptionalHeaders.Add("Sec-Fetch-Mode", "navigate");
            _YandexWebReader.OptionalHeaders.Add("Sec-Fetch-User", "?1");
            _YandexWebReader.OptionalHeaders.Add("Sec-Fetch-Dest", "document");

            //string _baseUrl = "https://translate.google.com/m";
            //var requestResult = _YandexWebReader.RequestWebData(_baseUrl, HttpUtilities.HttpMethods.GET, true);

            if (_YandexAuth?.Cookies != null)
            {
                var yandexUri = new Uri(yandexTranslateUrl);
                foreach (var cookie in _YandexAuth.Cookies)
                {
                    try
                    {
                        System.Net.Cookie cook = null;

                        // if (cookie.Path!="/")
                        cook = new System.Net.Cookie(name: cookie.Name, value: cookie.Value, path: cookie.Path, domain: cookie.Domain);
                        // else
                        //cook = new System.Net.Cookie(name: cookie.Name, value: cookie.Value, domain: cookie.Domain);

                        _YandexWebReader.Cookies.Add(yandexUri, cook);
                    }
                    catch (Exception ex)
                    {
                        var t = ex.ToString();
                    }
                }
            }
        }

        string GetSid()
        {
            string resultSid = null;

            _IsInitialized = true;

            try
            {
                _YandexWebReader.ContentType = null;
                var mainPage = _YandexWebReader.RequestWebData(yandexTranslateUrl, HttpUtilities.HttpMethods.GET, true);

                if (mainPage.IsSuccessful)
                {
                    resultSid = ParseSidFromPage(mainPage.Body);
                }
                else
                {
                    _IsBad = true;
                }
            }
            catch (Exception ex)
            {
                _IsBad = true;
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
            else
            {
                _IsBad = true;
            }

            return sid;
        }
    }
}
