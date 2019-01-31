// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IActorItem.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   IActorItem.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core.Interfaces {
    using System.Collections.Generic;

    using Sharlayan.Core.Enums;

    public interface IActorItem {
        Actor.ActionStatus ActionStatus { get; set; }

        byte ActionStatusID { get; set; }

        byte AgroFlags { get; set; }

        short CastingID { get; set; }

        float CastingProgress { get; set; }

        uint CastingTargetID { get; set; }

        float CastingTime { get; set; }

        uint ClaimedByID { get; set; }

        byte CombatFlags { get; set; }

        short CPCurrent { get; set; }

        short CPMax { get; set; }

        byte DifficultyRank { get; set; }

        byte Distance { get; set; }

        Actor.EventObjectType EventObjectType { get; set; }

        ushort EventObjectTypeID { get; set; }

        uint Fate { get; set; }

        byte GatheringInvisible { get; set; }

        byte GatheringStatus { get; set; }

        short GPCurrent { get; set; }

        short GPMax { get; set; }

        byte GrandCompany { get; set; }

        byte GrandCompanyRank { get; set; }

        float Heading { get; set; }

        float HitBoxRadius { get; set; }

        int HPCurrent { get; set; }

        int HPMax { get; set; }

        Actor.Icon Icon { get; set; }

        byte IconID { get; set; }

        uint ID { get; set; }

        bool InCombat { get; }

        bool IsAggressive { get; }

        bool IsCasting { get; }

        bool IsGM { get; set; }

        Actor.Job Job { get; set; }

        byte JobID { get; set; }

        byte Level { get; set; }

        uint MapID { get; set; }

        uint MapIndex { get; set; }

        uint MapTerritory { get; set; }

        uint ModelID { get; set; }

        int MPCurrent { get; set; }

        int MPMax { get; set; }

        string Name { get; set; }

        uint NPCID1 { get; set; }

        uint NPCID2 { get; set; }

        uint OwnerID { get; set; }

        byte Race { get; set; }

        Actor.Sex Sex { get; set; }

        byte SexID { get; set; }

        Actor.Status Status { get; set; }

        byte StatusID { get; set; }

        List<StatusItem> StatusItems { get; }

        byte TargetFlags { get; set; }

        int TargetID { get; set; }

        Actor.TargetType TargetType { get; set; }

        byte TargetTypeID { get; set; }

        byte Title { get; set; }

        int TPCurrent { get; set; }

        Actor.Type Type { get; set; }

        byte TypeID { get; set; }

        string UUID { get; set; }

        bool WeaponUnsheathed { get; }

        double X { get; set; }

        double Y { get; set; }

        double Z { get; set; }

        ActorItem Clone();
    }
}