// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatLogPointers.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ChatLogPointers.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Models.Structures {
    public class ChatLogPointers {
        public int LogEnd { get; set; }

        public int LogNext { get; set; }

        public int LogStart { get; set; }

        public int OffsetArrayEnd { get; set; }

        public int OffsetArrayPos { get; set; }

        public int OffsetArrayStart { get; set; }
    }
}