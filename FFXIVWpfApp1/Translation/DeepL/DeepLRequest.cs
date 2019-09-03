// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FFXIVTataruHelper.Translation
{
    class DeepLRequest
    {
        public class DeepLTranslatorRequest
        {

            public string jsonrpc { get; set; }
            public string method { get; set; }
            public Params @params { get; set; }
            public int id { get; set; }

            public DeepLTranslatorRequest(int _id, string sentence, string sourceLanguage, string tragetLangugaue)
            {
                id = _id;
                jsonrpc = "2.0";
                method = "LMT_handle_jobs";

                List<string> regexValues = new List<string>();

                int nameInd = 0;
                if ((nameInd = sentence.IndexOf(":")) > 0)
                {
                    nameInd++;

                    var name = sentence.Substring(0, nameInd);
                    var text = sentence.Substring(nameInd, sentence.Length - nameInd);

                    if (name.Length > 0)
                    {
                        name = name.Replace(":", " :");
                        regexValues.Add(name);
                        sentence = text;
                    }
                }

                Regex sentenceRegex = new Regex(@"(\S.+?([.!?♪]|$))(?=\s+|$)");
                var regexResult = sentenceRegex.Matches(sentence);

                for (int i = 0; i < regexResult.Count; i++)
                {
                    regexValues.Add(regexResult[i].Value);
                }


                List<Job> _jobs = new List<Job>();
                if (regexValues.Count > 0)
                {
                    int i = 0;

                    if (regexValues.Count > 1)
                        _jobs.Add(new Job(regexValues[i], null, regexValues[i + 1]));
                    else
                        _jobs.Add(new Job(regexValues[i], null, null));

                    for (i = 1; i < regexValues.Count - 1; i++)
                    {
                        var rxRex = regexValues[i];

                        _jobs.Add(new Job(regexValues[i], regexValues[i - 1], regexValues[i + 1]));
                    }
                    if (regexValues.Count - i > 0)
                        _jobs.Add(new Job(regexValues[i], regexValues[i - 1], null));
                }

                Lang lang = new Lang(sourceLanguage, tragetLangugaue);

                @params = new Params(_jobs, lang);

            }

            public class Params
            {
                public Params(List<Job> _jobs, Lang _lang)
                {
                    priority = 1;

                    jobs = _jobs;

                    lang = _lang;

                    timestamp = (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                }

                public List<Job> jobs { get; set; }
                public Lang lang { get; set; }
                public int priority { get; set; }
                public long timestamp { get; set; }
            }

            public class Job
            {
                public Job(string sentence, string context_before, string context_after)
                {
                    kind = "default";
                    raw_en_sentence = sentence;

                    raw_en_context_before = new List<string>();
                    if (context_before != null)
                        raw_en_context_before.Add(context_before);

                    raw_en_context_after = new List<string>();
                    if (context_after != null)
                        raw_en_context_after.Add(context_after);
                }

                public string kind { get; set; }
                public string raw_en_sentence { get; set; }

                public List<string> raw_en_context_before { get; set; }
                public List<string> raw_en_context_after { get; set; }
            }

            public class Lang
            {
                public Lang(string sourceLanguage, string tragetLangugaue)
                {
                    source_lang_computed = sourceLanguage;
                    target_lang = tragetLangugaue;

                    user_preferred_langs = new List<string>();
                    user_preferred_langs.Add(source_lang_computed);

                    user_preferred_langs.Add(tragetLangugaue);
                }
                public List<string> user_preferred_langs { get; set; }
                public string source_lang_computed { get; set; }
                public string target_lang { get; set; }
            }

            public string ToJsonString()
            {
                string res = String.Empty;

                res = JsonConvert.SerializeObject(this);

                return res;
            }
        }

        public class DeepLHandshakeRequest
        {
            public DeepLHandshakeRequest(int _id)
            {
                id = _id;
                jsonrpc = "2.0";
                method = "getClientState";

                @params = new Params();
            }
            public string jsonrpc { get; set; }
            public string method { get; set; }
            public Params @params { get; set; }
            public int id { get; set; }

            public class Params
            {
                public Params()
                {
                    v = "20180814";
                }
                public string v { get; set; }
            }
        }

        public class DeepLCookieRequest
        {
            public string jsonrpc { get; set; }
            public string method { get; set; }
            public Params @params { get; set; }
            public int id { get; set; }

            public DeepLCookieRequest(int _id, string text)
            {
                id = _id;
                jsonrpc = "2.0";
                method = "LMT_split_into_sentences";

                @params = new Params(text);
            }

            public class Lang
            {
                public Lang()
                {
                    lang_user_selected = "auto";
                    user_preferred_langs = new List<string>();
                    user_preferred_langs.Add("EN");
                }
                public string lang_user_selected { get; set; }
                public List<string> user_preferred_langs { get; set; }
            }

            public class Params
            {
                public Params(string text)
                {
                    lang = new Lang();

                    texts = new List<string>();

                    texts.Add(text);
                }

                public List<string> texts { get; set; }
                public Lang lang { get; set; }
            }

            public string ToJsonString()
            {
                string res = String.Empty;

                res = JsonConvert.SerializeObject(this);

                return res;
            }
        }
    }
}
