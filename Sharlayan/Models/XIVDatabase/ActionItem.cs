// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActionItem.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ActionItem.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Models.XIVDatabase {
    public class ActionItem {
        public int ActionCategory { get; set; }

        public int ActionCombo { get; set; }

        public int ActionData { get; set; }

        public int ActionProcStatus { get; set; }

        public int ActionTimelineHit { get; set; }

        public int ActionTimelineUse { get; set; }

        public int CanTargetDead { get; set; }

        public int CanTargetFriendly { get; set; }

        public int CanTargetHostile { get; set; }

        public int CanTargetParty { get; set; }

        public int CanTargetSelf { get; set; }

        public int CastRange { get; set; }

        public decimal CastTime { get; set; }

        public int ClassJob { get; set; }

        public int ClassJobCategory { get; set; }

        public int Cost { get; set; }

        public int CostCP { get; set; }

        public object CostHP { get; set; }

        public object CostMP { get; set; }

        public object CostTP { get; set; }

        public decimal Duration { get; set; }

        public int EffectRange { get; set; }

        public bool HasNoInitialResult { get; set; }

        public int Icon { get; set; }

        public bool IsDamageOverTime { get; set; }

        public bool IsHealingOverTime { get; set; }

        public int IsInGame { get; set; }

        public int IsPvp { get; set; }

        public int IsTargetArea { get; set; }

        public int IsTrait { get; set; }

        public int Level { get; set; }

        public Localization Name { get; set; }

        public int OverTimePotency { get; set; }

        public int Potency { get; set; }

        public decimal RecastTime { get; set; }

        public int SpellGroup { get; set; }

        public int StatusGainSelf { get; set; }

        public int StatusRequired { get; set; }

        public int Type { get; set; }
    }
}