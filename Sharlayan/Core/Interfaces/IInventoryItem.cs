// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IInventoryItem.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   IInventoryItem.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core.Interfaces {
    public interface IInventoryItem {
        uint Amount { get; set; }

        uint Durability { get; set; }

        double DurabilityPercent { get; }

        uint GlamourID { get; set; }

        uint ID { get; set; }

        uint SB { get; set; }

        double SBPercent { get; }

        int Slot { get; set; }
    }
}