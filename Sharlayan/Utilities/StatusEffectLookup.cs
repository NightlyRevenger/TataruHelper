// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StatusEffectLookup.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   StatusEffectLookup.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Utilities {
    using System.Collections.Concurrent;
    using System.Linq;

    using Sharlayan.Models;
    using Sharlayan.Models.XIVDatabase;

    public static class StatusEffectLookup {
        private static StatusItem DefaultStatusInfo = new StatusItem {
            Name = new Localization {
                Chinese = "???",
                English = "???",
                French = "???",
                German = "???",
                Japanese = "???",
                Korean = "???",
            },
            CompanyAction = false,
        };

        private static bool Loading;

        private static ConcurrentDictionary<uint, StatusItem> StatusEffects = new ConcurrentDictionary<uint, StatusItem>();

        public static StatusItem GetStatusInfo(uint id, string patchVersion = "latest") {
            if (Loading) {
                return DefaultStatusInfo;
            }

            lock (StatusEffects) {
                if (StatusEffects.Any()) {
                    return StatusEffects.ContainsKey(id)
                               ? StatusEffects[id]
                               : DefaultStatusInfo;
                }

                Resolve(patchVersion);
                return DefaultStatusInfo;
            }
        }

        internal static void Resolve(string patchVersion = "latest") {
            if (Loading) {
                return;
            }

            Loading = true;
            APIHelper.GetStatusEffects(StatusEffects, patchVersion);
            Loading = false;
        }
    }
}