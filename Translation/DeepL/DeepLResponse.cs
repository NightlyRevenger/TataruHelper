// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Translation.Deepl.Responses
{
    class DeepLTranslationResponse
    {
        /*
        [JsonProperty("jsonrpc")]
        public string Jsonrpc { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }//*/

        [JsonProperty("result")]
        public Result Result { get; set; }
    }

    public partial class Result
    {
        [JsonProperty("translations")]
        public List<Translation> Translations { get; set; }

        /*
        [JsonProperty("target_lang")]
        public string TargetLang { get; set; }

        [JsonProperty("source_lang")]
        public string SourceLang { get; set; }

        [JsonProperty("source_lang_is_confident")]
        public bool SourceLangIsConfident { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }//*/
    }

    public partial class Translation
    {
        [JsonProperty("beams")]
        public List<Beam> Beams { get; set; }

        /*
        [JsonProperty("quality")]
        public string Quality { get; set; }//*/
    }

    public partial class Beam
    {
        [JsonProperty("postprocessed_sentence")]
        public string PostprocessedSentence { get; set; }

        /*
        [JsonProperty("num_symbols")]
        public long NumSymbols { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonProperty("totalLogProb")]
        public double TotalLogProb { get; set; }//*/
    }

}
