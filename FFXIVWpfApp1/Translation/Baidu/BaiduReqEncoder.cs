using FFXIVTataruHelper.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVTataruHelper.Translation.Baidu
{
    class BaiduReqEncoder
    {
        public string encoding(string sentence, string from, string to, string gtk, string token)
        {
            var engine = new Jurassic.ScriptEngine();
            engine.ExecuteFile("../../Resources/BaiduEncoder.js");
            string sign = engine.CallGlobalFunction<string>("token", sentence, gtk);

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

            return URLEncoding.GetQueryString(request);
        }
    }
}
