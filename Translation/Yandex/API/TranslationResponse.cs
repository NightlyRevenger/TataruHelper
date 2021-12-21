using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Translation.Yandex.API
{
    public class TranslationResponse
    {
        [JsonProperty("align")]
        public List<string> Align { get; set; }

        [JsonProperty("code")]
        public long Code { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }

        [JsonProperty("text")]
        public List<string> Text { get; set; }
    }
}
