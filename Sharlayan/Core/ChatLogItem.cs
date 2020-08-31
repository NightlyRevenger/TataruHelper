// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatLogItem.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ChatLogItem.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core {
    using System;

    using Sharlayan.Core.Interfaces;

    public class ChatLogItem : IChatLogItem {
        public byte[] Bytes { get; set; }

        public string Code { get; set; }

        public string Combined { get; set; }

        public bool JP { get; set; }

        public string Line { get; set; }

        public string Raw { get; set; }

        public DateTime TimeStamp { get; set; }
    }
}