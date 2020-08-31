// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Inventory.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Inventory.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core.Enums {
    public class Inventory {
        public enum Container : byte {
            INVENTORY_1 = 0x0,

            INVENTORY_2 = 0x1,

            INVENTORY_3 = 0x2,

            INVENTORY_4 = 0x3,

            CURRENT_EQ = 0x4,

            EXTRA_EQ = 0x5,

            CRYSTALS = 0x6,

            QUESTS_KI = 0x9,

            HIRE_1 = 0x12,

            HIRE_2 = 0x13,

            HIRE_3 = 0x14,

            HIRE_4 = 0x15,

            HIRE_5 = 0x16,

            HIRE_6 = 0x17,

            HIRE_7 = 0x18,

            AC_MH = 0x1D,

            AC_OH = 0x1E,

            AC_HEAD = 0x1F,

            AC_BODY = 0x20,

            AC_HANDS = 0x21,

            AC_BELT = 0x22,

            AC_LEGS = 0x23,

            AC_FEET = 0x24,

            AC_EARRINGS = 0x25,

            AC_NECK = 0x26,

            AC_WRISTS = 0x27,

            AC_RINGS = 0x28,

            AC_SOULS = 0x29,

            COMPANY_1 = 0x2A,

            COMPANY_2 = 0x2B,

            COMPANY_3 = 0x2C,

            COMPANY_CRYSTALS = 0x2D,
        }
    }
}