using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVTataruHelper.Translation.Baidu
{
    class BaiduRequest
    {
        public string from { get; set; }
        public string to { get; set; }
        public string query { get; set; }
        public string transtype { get; set; }
        public string simple_means_flag { get; set; }
        public string sign { get; set; }
        public string token { get; set; }
    }
}
