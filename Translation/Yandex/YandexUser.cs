using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translation.Yandex
{
    public class YandexUser
    {
        public string Login { get; set; }

        public string Password { get; set; }

        public List<SimpleCookie> Cookies { get; set; }
    }
}
