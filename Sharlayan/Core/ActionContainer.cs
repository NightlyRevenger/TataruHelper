// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActionContainer.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ActionContainer.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core {
    using System.Collections.Generic;

    using Sharlayan.Core.Enums;
    using Sharlayan.Core.Interfaces;

    public class ActionContainer : IActionContainer {
        public List<ActionItem> ActionItems { get; } = new List<ActionItem>();

        public Action.Container ContainerType { get; set; }
    }
}