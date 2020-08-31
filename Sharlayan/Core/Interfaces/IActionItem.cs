// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IActionItem.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   IActionItem.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core.Interfaces {
    using System.Collections.Generic;

    public interface IActionItem {
        string ActionKey { get; set; }

        int Amount { get; set; }

        int Category { get; set; }

        int CoolDownPercent { get; set; }

        int Icon { get; set; }

        int ID { get; set; }

        bool InRange { get; set; }

        bool IsAvailable { get; set; }

        bool IsProcOrCombo { get; set; }

        string KeyBinds { get; set; }

        List<string> Modifiers { get; }

        string Name { get; set; }

        int RemainingCost { get; set; }

        int Slot { get; set; }

        int Type { get; set; }
    }
}