// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Scanner.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Scanner.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;

    using NLog;

    using Sharlayan.Models;

    public sealed class Scanner {
        private const int MemCommit = 0x1000;

        private const int PageExecuteReadwrite = 0x40;

        private const int PageExecuteWritecopy = 0x80;

        private const int PageGuard = 0x100;

        private const int PageNoAccess = 0x01;

        private const int PageReadwrite = 0x04;

        private const int PageWritecopy = 0x08;

        private const int WildCardChar = 63;

        private const int Writable = PageReadwrite | PageWritecopy | PageExecuteReadwrite | PageExecuteWritecopy | PageGuard;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static Lazy<Scanner> _instance = new Lazy<Scanner>(() => new Scanner());

        private List<UnsafeNativeMethods.MEMORY_BASIC_INFORMATION> _regions;

        public static Scanner Instance => _instance.Value;

        public bool IsScanning { get; private set; }

        public Dictionary<string, Signature> Locations { get; } = new Dictionary<string, Signature>();

        public void LoadOffsets(IEnumerable<Signature> signatures, bool scanAllMemoryRegions = false) {
            if (MemoryHandler.Instance.ProcessModel?.Process == null) {
                return;
            }

            this.IsScanning = true;

            Func<bool> scanningFunc = delegate {
                var sw = new Stopwatch();
                sw.Start();

                if (scanAllMemoryRegions) {
                    this.LoadRegions();
                }

                List<Signature> scanable = signatures as List<Signature> ?? signatures.ToList();
                if (scanable.Any()) {
                    foreach (Signature signature in scanable) {
                        if (signature.Value == string.Empty) {
                            // doesn't need a signature scan
                            this.Locations[signature.Key] = signature;
                            continue;
                        }

                        signature.Value = signature.Value.Replace("*", "?"); // allows either ? or * to be used as wildcard
                    }

                    scanable.RemoveAll(a => this.Locations.ContainsKey(a.Key));

                    this.FindExtendedSignatures(scanable, scanAllMemoryRegions);
                }

                sw.Stop();

                MemoryHandler.Instance.RaiseSignaturesFound(Logger, this.Locations, sw.ElapsedMilliseconds);

                this.IsScanning = false;

                return true;
            };
            scanningFunc.BeginInvoke(delegate { }, scanningFunc);
        }

        private void FindExtendedSignatures(IEnumerable<Signature> signatures, bool scanAllMemoryRegions = false) {
            List<Signature> notFound = new List<Signature>(signatures);

            IntPtr baseAddress = MemoryHandler.Instance.ProcessModel.Process.MainModule.BaseAddress;
            IntPtr searchEnd = IntPtr.Add(baseAddress, MemoryHandler.Instance.ProcessModel.Process.MainModule.ModuleMemorySize);
            IntPtr searchStart = baseAddress;

            this.ResolveLocations(baseAddress, searchStart, searchEnd, ref notFound);

            if (scanAllMemoryRegions) {
                foreach (UnsafeNativeMethods.MEMORY_BASIC_INFORMATION region in this._regions) {
                    baseAddress = region.BaseAddress;
                    searchEnd = new IntPtr(baseAddress.ToInt64() + region.RegionSize.ToInt64());
                    searchStart = baseAddress;

                    this.ResolveLocations(baseAddress, searchStart, searchEnd, ref notFound);
                }
            }
        }

        private int FindSuperSignature(byte[] buffer, byte[] pattern) {
            var result = -1;
            if (buffer.Length <= 0 || pattern.Length <= 0 || buffer.Length < pattern.Length) {
                return result;
            }

            for (var i = 0; i <= buffer.Length - pattern.Length; i++) {
                if (buffer[i] != pattern[0]) {
                    continue;
                }

                if (buffer.Length > 1) {
                    var matched = true;
                    for (var y = 1; y <= pattern.Length - 1; y++) {
                        if (buffer[i + y] == pattern[y] || pattern[y] == WildCardChar) {
                            continue;
                        }

                        matched = false;
                        break;
                    }

                    if (!matched) {
                        continue;
                    }

                    result = i;
                    break;
                }

                result = i;
                break;
            }

            return result;
        }

        private void LoadRegions() {
            try {
                this._regions = new List<UnsafeNativeMethods.MEMORY_BASIC_INFORMATION>();
                IntPtr address = IntPtr.Zero;
                while (true) {
                    var info = new UnsafeNativeMethods.MEMORY_BASIC_INFORMATION();
                    var result = UnsafeNativeMethods.VirtualQueryEx(MemoryHandler.Instance.ProcessHandle, address, out info, (uint) Marshal.SizeOf(info));
                    if (result == 0) {
                        break;
                    }

                    if (!MemoryHandler.Instance.IsSystemModule(info.BaseAddress) && (info.State & MemCommit) != 0 && (info.Protect & Writable) != 0 && (info.Protect & PageGuard) == 0) {
                        this._regions.Add(info);
                    }
                    else {
                        MemoryHandler.Instance.RaiseException(Logger, new Exception(info.ToString()));
                    }

                    unchecked {
                        switch (IntPtr.Size) {
                            case sizeof(int):
                                address = new IntPtr(info.BaseAddress.ToInt32() + info.RegionSize.ToInt32());
                                break;
                            default:
                                address = new IntPtr(info.BaseAddress.ToInt64() + info.RegionSize.ToInt64());
                                break;
                        }
                    }
                }
            }
            catch (Exception ex) {
                MemoryHandler.Instance.RaiseException(Logger, ex, true);
            }
        }

        private void ResolveLocations(IntPtr baseAddress, IntPtr searchStart, IntPtr searchEnd, ref List<Signature> notFound) {
            const int bufferSize = 0x1200;
            const int regionIncrement = 0x1000;

            byte[] buffer = new byte[bufferSize];
            List<Signature> temp = new List<Signature>();
            var regionCount = 0;

            while (searchStart.ToInt64() < searchEnd.ToInt64()) {
                try {
                    IntPtr lpNumberOfBytesRead;
                    var regionSize = new IntPtr(bufferSize);
                    if (IntPtr.Add(searchStart, bufferSize).ToInt64() > searchEnd.ToInt64()) {
                        regionSize = (IntPtr) (searchEnd.ToInt64() - searchStart.ToInt64());
                    }

                    if (UnsafeNativeMethods.ReadProcessMemory(MemoryHandler.Instance.ProcessHandle, searchStart, buffer, regionSize, out lpNumberOfBytesRead)) {
                        foreach (Signature signature in notFound) {
                            var index = this.FindSuperSignature(buffer, this.SignatureToByte(signature.Value, WildCardChar));
                            if (index < 0) {
                                temp.Add(signature);
                                continue;
                            }

                            var baseResult = new IntPtr((long) (baseAddress + regionCount * regionIncrement));
                            IntPtr searchResult = IntPtr.Add(baseResult, index + signature.Offset);

                            signature.SigScanAddress = new IntPtr(searchResult.ToInt64());

                            if (!this.Locations.ContainsKey(signature.Key)) {
                                this.Locations.Add(signature.Key, signature);
                            }
                        }

                        notFound = new List<Signature>(temp);
                        temp.Clear();
                    }

                    regionCount++;
                    searchStart = IntPtr.Add(searchStart, regionIncrement);
                }
                catch (Exception ex) {
                    MemoryHandler.Instance.RaiseException(Logger, ex, true);
                }
            }
        }

        private byte[] SignatureToByte(string signature, byte wildcard) {
            byte[] pattern = new byte[signature.Length / 2];
            int[] hexTable = {
                0x00,
                0x01,
                0x02,
                0x03,
                0x04,
                0x05,
                0x06,
                0x07,
                0x08,
                0x09,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x0A,
                0x0B,
                0x0C,
                0x0D,
                0x0E,
                0x0F
            };
            try {
                for (int x = 0,
                         i = 0; i < signature.Length; i += 2, x += 1) {
                    if (signature[i] == wildcard) {
                        pattern[x] = wildcard;
                    }
                    else {
                        pattern[x] = (byte) ((hexTable[char.ToUpper(signature[i]) - '0'] << 4) | hexTable[char.ToUpper(signature[i + 1]) - '0']);
                    }
                }

                return pattern;
            }
            catch {
                return null;
            }
        }
    }
}