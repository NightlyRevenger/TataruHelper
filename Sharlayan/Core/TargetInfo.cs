// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TargetInfo.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   TargetInfo.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core {
    using System.Collections.Generic;

    using Sharlayan.Core.Interfaces;

    public class TargetInfo : ITargetInfo {
        public ActorItem CurrentTarget { get; set; }

        public uint CurrentTargetID { get; set; }

        public List<EnmityItem> EnmityItems { get; } = new List<EnmityItem>();

        public ActorItem FocusTarget { get; set; }

        public ActorItem MouseOverTarget { get; set; }

        public ActorItem PreviousTarget { get; set; }
    }
}