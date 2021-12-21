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

        public string AcceptLanguage { get; set; }

        public string AcceptEncoding { get; set; }

        public string Referer { get; set; }

        public List<SimpleCookie> Cookies { get; set; } = new List<SimpleCookie>();

        public long SuccessfulRequests { get; set; } = 0;

        public long FailedRequests { get; set; } = 0;

        public DateTime LastFailedRequest { get; set; } = default(DateTime);

        public DateTime LastSuccessfulRequest { get; set; } = default(DateTime);

        public YandexAuthContainer() { }

        public YandexAuthContainer(List<SimpleCookie> cookies)
        {
            this.Cookies = cookies.Select(x => new SimpleCookie(x)).ToList();
        }

    }

    public class SimpleCookie
    {
        public string Domain { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Path { get; set; }

        public SimpleCookie() { }

        public SimpleCookie(string name, string value, string path, string domain)
        {
            this.Domain = domain;
            this.Name = name;
            this.Value = value;
            this.Path = path;
        }

        public SimpleCookie(SimpleCookie cookie)
        {
            this.Domain = cookie.Domain;
            this.Name = cookie.Name;
            this.Value = cookie.Value;
            this.Path = cookie.Path;
        }
    }
}
