// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

namespace Translation.Baidu
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
