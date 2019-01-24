// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PartyResult.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   PartyResult.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Models.ReadResults {
    using System.Collections.Concurrent;

    using Sharlayan.Core;
    using Sharlayan.Delegates;

    public class PartyResult {
        public ConcurrentDictionary<uint, PartyMember> NewPartyMembers { get; } = new ConcurrentDictionary<uint, PartyMember>();

        public ConcurrentDictionary<uint, PartyMember> PartyMembers => PartyWorkerDelegate.PartyMembers;

        public ConcurrentDictionary<uint, PartyMember> RemovedPartyMembers { get; } = new ConcurrentDictionary<uint, PartyMember>();
    }
}