// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StructuresContainer.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   StructuresContainer.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Models.Structures {
    public class StructuresContainer {
        public ActorItem ActorItem { get; set; } = new ActorItem();

        public ChatLogPointers ChatLogPointers { get; set; } = new ChatLogPointers();

        public CurrentPlayer CurrentPlayer { get; set; } = new CurrentPlayer();

        public EnmityItem EnmityItem { get; set; } = new EnmityItem();

        public HotBarItem HotBarItem { get; set; } = new HotBarItem();

        public InventoryContainer InventoryContainer { get; set; } = new InventoryContainer();

        public InventoryItem InventoryItem { get; set; } = new InventoryItem();

        public PartyMember PartyMember { get; set; } = new PartyMember();

        public RecastItem RecastItem { get; set; } = new RecastItem();

        public StatusItem StatusItem { get; set; } = new StatusItem();

        public TargetInfo TargetInfo { get; set; } = new TargetInfo();
    }
}