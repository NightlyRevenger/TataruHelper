// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProcessModel.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ProcessModel.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Models {
    using System.Diagnostics;

    public class ProcessModel {
        public bool IsWin64 { get; set; }

        public Process Process { get; set; }

        public int ProcessID => this.Process?.Id ?? -1;

        public string ProcessName => this.Process?.ProcessName ?? string.Empty;
    }
}