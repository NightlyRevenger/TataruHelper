// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IStatusItem.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   IStatusItem.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core.Interfaces {
    public interface IStatusItem {
        uint CasterID { get; set; }

        float Duration { get; set; }

        bool IsCompanyAction { get; set; }

        ActorItem SourceEntity { get; set; }

        byte Stacks { get; set; }

        short StatusID { get; set; }

        string StatusName { get; set; }

        ActorItem TargetEntity { get; set; }

        string TargetName { get; set; }

        bool IsValid();
    }
}