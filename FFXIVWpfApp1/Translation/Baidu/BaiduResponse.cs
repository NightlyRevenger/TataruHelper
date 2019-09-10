﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVTataruHelper.Translation.Baidu
{
    class BaiduResponse
    {
        public Result trans_result { get; set; }
        
        public class Result
        {
            public Data[] data { get; set; }
        }

        public class Data
        {
            public string dst { get; set; }
            public object prefixWrap { get; set; }
            public object[] result { get; set; }
            public string src { get; set; }
        }
    }
}
