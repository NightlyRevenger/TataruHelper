// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITargetInfo.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ITargetInfo.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core.Interfaces {
    using System.Collections.Generic;

    public interface ITargetInfo {
        ActorItem CurrentTarget { get; set; }

        uint CurrentTargetID { get; set; }

        List<EnmityItem> EnmityItems { get; }

        ActorItem FocusTarget { get; set; }

        ActorItem MouseOverTarget { get; set; }

        ActorItem PreviousTarget { get; set; }
    }
}