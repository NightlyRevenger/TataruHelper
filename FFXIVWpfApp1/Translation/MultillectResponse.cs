// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

namespace FFXIITataruHelper.Translation
{
    class MultillectResponse
    {
        public class Result
        {
            public string translations { get; set; }
        }

        public class MultillectRoot
        {
            public Result result { get; set; }
            public object error { get; set; }
            public int timestamp { get; set; }
            public object id { get; set; }
        }
    }
}
