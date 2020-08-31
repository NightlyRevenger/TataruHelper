using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translation.Yandex
{
    class YandexRequest
    {
        public string UrlParams { get; set; }

        public string BodyRequest { get; set; }

        public YandexSession YandexSession { get; set; }
    }
}
