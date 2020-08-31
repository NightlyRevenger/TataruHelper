// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorResult.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ActorResult.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Models.ReadResults {
    using System.Collections.Concurrent;

    using Sharlayan.Core;
    using Sharlayan.Delegates;

    public class ActorResult {
        public ConcurrentDictionary<uint, ActorItem> CurrentMonsters => MonsterWorkerDelegate.ActorItems;

        public ConcurrentDictionary<uint, ActorItem> CurrentNPCs => NPCWorkerDelegate.ActorItems;

        public ConcurrentDictionary<uint, ActorItem> CurrentPCs => PCWorkerDelegate.ActorItems;

        public ConcurrentDictionary<uint, ActorItem> NewMonsters { get; } = new ConcurrentDictionary<uint, ActorItem>();

        public ConcurrentDictionary<uint, ActorItem> NewNPCs { get; } = new ConcurrentDictionary<uint, ActorItem>();

        public ConcurrentDictionary<uint, ActorItem> NewPCs { get; } = new ConcurrentDictionary<uint, ActorItem>();

        public ConcurrentDictionary<uint, ActorItem> RemovedMonsters { get; } = new ConcurrentDictionary<uint, ActorItem>();

        public ConcurrentDictionary<uint, ActorItem> RemovedNPCs { get; } = new ConcurrentDictionary<uint, ActorItem>();

        public ConcurrentDictionary<uint, ActorItem> RemovedPCs { get; } = new ConcurrentDictionary<uint, ActorItem>();
    }
}