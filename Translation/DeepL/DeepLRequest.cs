// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Translation.Deepl.Requests
{
    public class DeepLTranslatorRequest
    {
        private static Regex sentenceRegex = new Regex(@"(\S.+?([.!?♪。]|$))(?=\s+|$)", RegexOptions.Compiled);

        [JsonProperty("jsonrpc")]
        public string Jsonrpc { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public Params Params { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        public DeepLTranslatorRequest(long id, string input, string sourceLanguage, string tragetLangugaue)
        {
            this.Id = id;
            this.Jsonrpc = "2.0";
            this.Method = "LMT_handle_jobs";

            List<string> computedSentences = new List<string>();

            input = Preporcess(input);

            var sentences = sentenceRegex.Matches(input);

            foreach (Match sentence in sentences)
            {
                computedSentences.Add(sentence.Value);
            }
            if (computedSentences.Count == 0)
                computedSentences.Add(input);


            List<Job> jobs = new List<Job>();
            if (computedSentences.Count > 0)
            {
                int i = 0;

                if (computedSentences.Count > 1)
                    jobs.Add(new Job(computedSentences[i], null, computedSentences[i + 1]));
                else
                    jobs.Add(new Job(computedSentences[i], null, null));

                for (i = 1; i < computedSentences.Count - 1; i++)
                {

                    jobs.Add(new Job(computedSentences[i], computedSentences[i - 1], computedSentences[i + 1]));
                }
                if (computedSentences.Count - i > 0)
                    jobs.Add(new Job(computedSentences[i], computedSentences[i - 1], null));
            }

            Lang lang = new Lang(sourceLanguage, tragetLangugaue);

            Params = new Params(jobs, lang);

        }


        public string ToJsonString()
        {
            string res = String.Empty;

            res = JsonConvert.SerializeObject(this);

            return res;
        }

        string Preporcess(string text)
        {
            var emDash = '\u2014';
            text = text.Replace(emDash, '-');

            return text;
        }

    }

    public class Params
    {
        private static Regex sentenceRegex = new Regex(@"[i]", RegexOptions.Compiled);

        [JsonProperty("jobs")]
        public List<Job> Jobs { get; set; }

        [JsonProperty("lang")]
        public Lang Lang { get; set; }

        [JsonProperty("priority")]
        public long Priority { get; set; }

        [JsonProperty("commonJobParams")]
        public CommonJobParams CommonJobParams { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        public Params(IEnumerable<Job> jobs, Lang lang)
        {
            Priority = 1;

            Jobs = jobs.ToList();

            Lang = lang;

            Timestamp = (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
        }
    }

    public class CommonJobParams
    {
        [JsonProperty("formality")]
        public object Formality { get; set; }
    }

    public class Job
    {
        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("raw_en_sentence")]
        public string RawEnSentence { get; set; }

        [JsonProperty("raw_en_context_before")]
        public List<string> RawEnContextBefore { get; set; }

        [JsonProperty("raw_en_context_after")]
        public List<string> RawEnContextAfter { get; set; }

        [JsonProperty("preferred_num_beams")]
        public long PreferredNumBeams { get; set; }

        public Job(string sentence, string contextBefore, string contextAfter)
        {
            Kind = "default";
            RawEnSentence = sentence;

            RawEnContextBefore = new List<string>();
            if (contextBefore != null)
                RawEnContextBefore.Add(contextBefore);

            RawEnContextAfter = new List<string>();
            if (contextAfter != null)
                RawEnContextAfter.Add(contextAfter);
        }
    }

    public partial class Lang
    {
        [JsonProperty("user_preferred_langs")]
        public List<string> UserPreferredLangs { get; set; }

        [JsonProperty("source_lang_computed")]
        public string SourceLangComputed { get; set; }

        [JsonProperty("target_lang")]
        public string TargetLang { get; set; }

        public Lang(string sourceLanguage, string tragetLangugaue)
        {
            SourceLangComputed = sourceLanguage;
            TargetLang = tragetLangugaue;

            UserPreferredLangs = new List<string>();
            UserPreferredLangs.Add(sourceLanguage);

            UserPreferredLangs.Add(tragetLangugaue);
        }

    }
}