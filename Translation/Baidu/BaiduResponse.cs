// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace Translation.Baidu
{
    class BaiduResponse
    {
        [JsonProperty("trans_result")]
        public TranslationResult TransResult { get; set; }

        /*
        [JsonProperty("liju_result")]
        public LijuResult LijuResult { get; set; }

        [JsonProperty("logid")]
        public long Logid { get; set; }

        public class LijuResult
        {
            [JsonProperty("double")]
            public string Double { get; set; }

            [JsonProperty("single")]
            public string Single { get; set; }
        }//*/

        public class TranslationResult
        {
            [JsonProperty("data")]
            public List<Datum> Data { get; set; }

            /*
            [JsonProperty("from")]
            public string From { get; set; }

            [JsonProperty("status")]
            public long Status { get; set; }

            [JsonProperty("to")]
            public string To { get; set; }

            [JsonProperty("type")]
            public long Type { get; set; }//*/
        }

        public partial class Datum
        {
            [JsonProperty("dst")]
            public string Dst { get; set; }

            /*
            [JsonProperty("prefixWrap")]
            public long PrefixWrap { get; set; }

            [JsonProperty("result")]
            public string Result { get; set; }

            [JsonProperty("src")]
            public string Src { get; set; }//*/
        }//*/
    }
}
