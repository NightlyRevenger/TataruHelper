// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

namespace Translation.Baidu
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
