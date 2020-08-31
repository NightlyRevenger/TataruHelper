// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SignaturesFoundEvent.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   SignaturesFoundEvent.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Events {
    using System;
    using System.Collections.Generic;

    using NLog;

    using Sharlayan.Models;

    public class SignaturesFoundEvent : EventArgs {
        public SignaturesFoundEvent(object sender, Logger logger, Dictionary<string, Signature> signatures, long processingTime) {
            this.Sender = sender;
            this.Logger = logger;
            this.Signatures = signatures;
            this.ProcessingTime = processingTime;
        }

        public Logger Logger { get; set; }

        public long ProcessingTime { get; set; }

        public object Sender { get; set; }

        public Dictionary<string, Signature> Signatures { get; }
    }
}