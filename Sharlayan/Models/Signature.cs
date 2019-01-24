// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Signature.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Signature.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Models {
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;

    public class Signature {
        private Regex _regularExpress;

        public Signature() {
            this.Key = string.Empty;
            this.Value = string.Empty;
            this.RegularExpress = null;
            this.SigScanAddress = IntPtr.Zero;
            this.PointerPath = null;
        }

        public bool ASMSignature { get; set; }

        public string Key { get; set; }

        [JsonIgnore]
        public int Offset {
            get {
                return this.Value.Length / 2;
            }
        }

        public List<long> PointerPath { get; set; }

        public Regex RegularExpress {
            get {
                return this._regularExpress;
            }

            set {
                if (value != null) {
                    this._regularExpress = value;
                }
            }
        }

        [JsonIgnore]
        public IntPtr SigScanAddress { get; set; }

        public string Value { get; set; }

        public static implicit operator IntPtr(Signature signature) {
            return signature.GetAddress();
        }

        public IntPtr GetAddress() {
            IntPtr baseAddress = IntPtr.Zero;
            var IsASMSignature = false;
            if (this.SigScanAddress != IntPtr.Zero) {
                baseAddress = this.SigScanAddress; // Scanner should have already applied the base offset
                if (MemoryHandler.Instance.ProcessModel.IsWin64 && this.ASMSignature) {
                    IsASMSignature = true;
                }
            }
            else {
                if (this.PointerPath == null || this.PointerPath.Count == 0) {
                    return IntPtr.Zero;
                }

                baseAddress = MemoryHandler.Instance.GetStaticAddress(0);
            }

            if (this.PointerPath == null || this.PointerPath.Count == 0) {
                return baseAddress;
            }

            return MemoryHandler.Instance.ResolvePointerPath(this.PointerPath, baseAddress, IsASMSignature);
        }
    }
}