// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

namespace Sharlayan
{
    static class GlobalSettings
    {
        public static string FFxivActions = "https://raw.githubusercontent.com/NightlyRevenger/sharlayan-resources/master/xivdatabase/{0}/actions.json";
        //https://raw.githubusercontent.com/FFXIVAPP/sharlayan-resources/master/xivdatabase/{patchVersion}/actions.json

        public static string FFxivSignatures = "https://raw.githubusercontent.com/NightlyRevenger/sharlayan-resources/master/signatures/{0}/{1}.json";
        //https://raw.githubusercontent.com/FFXIVAPP/sharlayan-resources/master/signatures/{patchVersion}/{architecture}.json

        public static string FFxivStatuses = "https://raw.githubusercontent.com/NightlyRevenger/sharlayan-resources/master/xivdatabase/{0}/statuses.json";
        //https://raw.githubusercontent.com/FFXIVAPP/sharlayan-resources/master/xivdatabase/{patchVersion}/statuses.json

        public static string FFxivStructures = "https://raw.githubusercontent.com/NightlyRevenger/sharlayan-resources/master/structures/{0}/{1}.json";
        //https://raw.githubusercontent.com/FFXIVAPP/sharlayan-resources/master/structures/{patchVersion}/{architecture}.json

        public static string FFxivZones = "https://raw.githubusercontent.com/NightlyRevenger/sharlayan-resources/master/xivdatabase/{0}/zones.json";
        //https://raw.githubusercontent.com/FFXIVAPP/sharlayan-resources/master/xivdatabase/{patchVersion}/zones.json


        public static double LineLettersCoefficient = 4.0;
    }
}
