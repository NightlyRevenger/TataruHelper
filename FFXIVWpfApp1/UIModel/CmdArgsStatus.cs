// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;

namespace FFXIVTataruHelper
{
    public static class CmdArgsStatus
    {
        public static bool IsPreRelease { get; private set; }

        public static bool LogPlotChat { get; private set; }

        public static bool LogAllChat { get; private set; }

        public static void LoadArgs()
        {
            IsPreRelease = false;
            LogPlotChat = false;
            LogAllChat = false;


            string[] args = Environment.GetCommandLineArgs();

            List<string> argsList = new List<string>();

            for (int i = 1; i < args.Length; i++)
            {
                argsList.Add(args[i]);
            }

            if (argsList.Count > 0)
            {
                if (argsList.Any(x => x.ToLower() == "-prerelease"))
                    IsPreRelease = true;

                if (argsList.Any(x => x.ToLower() == "-logall"))
                    LogAllChat = true;

                if (argsList.Any(x => x.ToLower() == "-logplot"))
                    LogPlotChat = true;
            }
        }
    }
}
