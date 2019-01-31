// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActionLookup.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ActionLookup.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Utilities {
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using Sharlayan.Models;
    using Sharlayan.Models.XIVDatabase;

    public static class ActionLookup {
        private static ConcurrentDictionary<uint, ActionItem> Actions = new ConcurrentDictionary<uint, ActionItem>();

        private static ActionItem DefaultActionInfo = new ActionItem {
            Name = new Localization {
                Chinese = "???",
                English = "???",
                French = "???",
                German = "???",
                Japanese = "???",
                Korean = "???"
            }
        };

        private static bool Loading;

        public static List<ActionItem> DamageOverTimeActions(string patchVersion = "latest") {
            List<ActionItem> results = new List<ActionItem>();
            if (Loading) {
                return results;
            }

            lock (Actions) {
                if (Actions.Any()) {
                    results.AddRange(Actions.Where(kvp => kvp.Value.IsDamageOverTime).Select(kvp => kvp.Value));
                    return results;
                }

                Resolve(patchVersion);
                return results;
            }
        }

        public static ActionItem GetActionInfo(string name, string patchVersion = "latest") {
            if (Loading) {
                return DefaultActionInfo;
            }

            lock (Actions) {
                if (Actions.Any()) {
                    return Actions.FirstOrDefault(kvp => kvp.Value.Name.Matches(name)).Value ?? DefaultActionInfo;
                }

                Resolve(patchVersion);
                return DefaultActionInfo;
            }
        }

        public static ActionItem GetActionInfo(uint id, string patchVersion = "latest") {
            if (Loading) {
                return DefaultActionInfo;
            }

            lock (Actions) {
                if (Actions.Any()) {
                    return Actions.ContainsKey(id)
                               ? Actions[id]
                               : DefaultActionInfo;
                }

                Resolve(patchVersion);
                return DefaultActionInfo;
            }
        }

        public static List<ActionItem> HealingOverTimeActions(string patchVersion = "latest") {
            List<ActionItem> results = new List<ActionItem>();
            if (Loading) {
                return results;
            }

            lock (Actions) {
                if (Actions.Any()) {
                    results.AddRange(Actions.Where(kvp => kvp.Value.IsHealingOverTime).Select(kvp => kvp.Value));
                    return results;
                }

                Resolve(patchVersion);
                return results;
            }
        }

        internal static void Resolve(string patchVersion = "latest") {
            if (Loading) {
                return;
            }

            Loading = true;
            APIHelper.GetActions(Actions, patchVersion);
            Loading = false;
        }
    }
}