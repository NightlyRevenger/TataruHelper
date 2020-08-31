using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translation.Yandex
{
    public class YandexAuthContainer
    {
        public string UserAgent { get; set; }

        public string Accept { get; set; }

        public string AcceptLanguage { get; set; }

        public string AcceptEncoding { get; set; }

        public string Referer { get; set; }

        public List<KeyValuePair<string, string>> Cookies { get; set; } = new List<KeyValuePair<string, string>>();

        public long SuccessfulRequests { get; set; } = 0;

        public long FailedRequests { get; set; } = 0;

        public DateTime LastFailedRequest { get; set; } = default(DateTime);

        public DateTime LastSuccessfulRequest { get; set; } = default(DateTime);

    }
}
