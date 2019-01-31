// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PartyMember.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   PartyMember.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Models.Structures {
    public class PartyMember {
        public int DefaultStatusEffectOffset { get; set; }

        public int HPCurrent { get; set; }

        public int HPMax { get; set; }

        public int ID { get; set; }

        public int Job { get; set; }

        public int Level { get; set; }

        public int MPCurrent { get; set; }

        public int MPMax { get; set; }

        public int Name { get; set; }

        public int SourceSize { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Z { get; set; }
    }
}