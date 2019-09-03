// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System.Collections.Generic;

namespace FFXIVTataruHelper.Translation
{
    public class DeepLResponse
    {
        public class DeepLTranslationResponse
        {
            public int id { get; set; }
            public string jsonrpc { get; set; }
            public Result result { get; set; }

            public class Beam
            {
                public int num_symbols { get; set; }
                public string postprocessed_sentence { get; set; }
                public double score { get; set; }
                public double totalLogProb { get; set; }
            }

            public class Translation
            {
                public List<Beam> beams { get; set; }
                public string quality { get; set; }
                public long timeAfterPreprocessing { get; set; }
                public long timeReceivedFromEndpoint { get; set; }
                public long timeSentToEndpoint { get; set; }
                public long total_time_endpoint { get; set; }
            }

            public class Result
            {
                public string date { get; set; }
                public string source_lang { get; set; }
                public int source_lang_is_confident { get; set; }
                public string target_lang { get; set; }
                public long timestamp { get; set; }
                public List<Translation> translations { get; set; }
            }
        }

        public class DeepLHandshakeResponse
        {
            public string jsonrpc { get; set; }
            public Result result { get; set; }
            public int id { get; set; }

            public class Result
            {
                public string ip { get; set; }
                public bool proAvailable { get; set; }
                public bool updateNecessary { get; set; }
                public bool ep { get; set; }
            }
        }

        public class DeepLSentencePreprocessResponse
        {
            public int id { get; set; }
            public string jsonrpc { get; set; }
            public Result result { get; set; }

            public class Result
            {
                public string lang { get; set; }
                public int lang_is_confident { get; set; }
                public List<List<string>> splitted_texts { get; set; }
            }
        }
    }
}
