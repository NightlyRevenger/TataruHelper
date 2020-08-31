// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MemoryHandler.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   MemoryHandler.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;

    using NLog;

    using Sharlayan.Events;
    using Sharlayan.Models;
    using Sharlayan.Models.Structures;
    using Sharlayan.Utilities;

    using BitConverter = Sharlayan.Utilities.BitConverter;

    public class MemoryHandler {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static Lazy<MemoryHandler> _instance = new Lazy<MemoryHandler>(() => new MemoryHandler());

        private AttachmentWorker AttachmentWorker;

        private bool IsNewInstance = true;

        ~MemoryHandler() {
            this.UnsetProcess();
        }

        public event EventHandler<ExceptionEvent> ExceptionEvent = (sender, args) => { };

        public event EventHandler<SignaturesFoundEvent> SignaturesFoundEvent = (sender, args) => { };

        public static MemoryHandler Instance {
            get {
                return _instance.Value;
            }
        }

        public bool IsAttached { get; set; }

        public long ScanCount { get; set; }

        internal string GameLanguage { get; set; }

        internal IntPtr ProcessHandle { get; set; }

        internal ProcessModel ProcessModel { get; set; }

        internal StructuresContainer Structures { get; set; }

        internal bool UseLocalCache { get; set; }

        private List<ProcessModule> SystemModules { get; set; } = new List<ProcessModule>();

        public byte GetByte(IntPtr address, long offset = 0) {
            byte[] data = new byte[1];
            this.Peek(new IntPtr(address.ToInt64() + offset), data);
            return data[0];
        }

        public byte[] GetByteArray(IntPtr address, int length) {
            byte[] data = new byte[length];
            this.Peek(address, data);
            return data;
        }

        public short GetInt16(IntPtr address, long offset = 0) {
            byte[] value = new byte[2];
            this.Peek(new IntPtr(address.ToInt64() + offset), value);
            return BitConverter.TryToInt16(value, 0);
        }

        public int GetInt32(IntPtr address, long offset = 0) {
            byte[] value = new byte[4];
            this.Peek(new IntPtr(address.ToInt64() + offset), value);
            return BitConverter.TryToInt32(value, 0);
        }

        public long GetInt64(IntPtr address, long offset = 0) {
            byte[] value = new byte[8];
            this.Peek(new IntPtr(address.ToInt64() + offset), value);
            return BitConverter.TryToInt64(value, 0);
        }

        public long GetPlatformInt(IntPtr address, long offset = 0) {
            byte[] bytes = new byte[this.ProcessModel.IsWin64
                                        ? 8
                                        : 4];
            this.Peek(new IntPtr(address.ToInt64() + offset), bytes);
            return this.GetPlatformIntFromBytes(bytes);
        }

        public long GetPlatformIntFromBytes(byte[] source, int index = 0) {
            if (this.ProcessModel.IsWin64) {
                return BitConverter.TryToInt64(source, index);
            }

            return BitConverter.TryToInt32(source, index);
        }

        public long GetPlatformUInt(IntPtr address, long offset = 0) {
            byte[] bytes = new byte[this.ProcessModel.IsWin64
                                        ? 8
                                        : 4];
            this.Peek(new IntPtr(address.ToInt64() + offset), bytes);
            return this.GetPlatformUIntFromBytes(bytes);
        }

        public long GetPlatformUIntFromBytes(byte[] source, int index = 0) {
            if (this.ProcessModel.IsWin64) {
                return (long) BitConverter.TryToUInt64(source, index);
            }

            return BitConverter.TryToUInt32(source, index);
        }

        public IntPtr GetStaticAddress(long offset) {
            return new IntPtr(Instance.ProcessModel.Process.MainModule.BaseAddress.ToInt64() + offset);
        }

        public string GetString(IntPtr address, long offset = 0, int size = 256) {
            byte[] bytes = new byte[size];
            this.Peek(new IntPtr(address.ToInt64() + offset), bytes);
            var realSize = 0;
            for (var i = 0; i < size; i++) {
                if (bytes[i] != 0) {
                    continue;
                }

                realSize = i;
                break;
            }

            Array.Resize(ref bytes, realSize);
            return Encoding.UTF8.GetString(bytes);
        }

        public string GetStringFromBytes(byte[] source, int offset = 0, int size = 256) {
            var safeSize = source.Length - offset;
            if (safeSize < size) {
                size = safeSize;
            }

            byte[] bytes = new byte[size];
            Array.Copy(source, offset, bytes, 0, size);
            var realSize = 0;
            for (var i = 0; i < size; i++) {
                if (bytes[i] != 0) {
                    continue;
                }

                realSize = i;
                break;
            }

            Array.Resize(ref bytes, realSize);
            return Encoding.UTF8.GetString(bytes);
        }

        public T GetStructure<T>(IntPtr address, int offset = 0) {
            IntPtr lpNumberOfBytesRead;
            IntPtr buffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(T)));
            UnsafeNativeMethods.ReadProcessMemory(this.ProcessModel.Process.Handle, address + offset, buffer, new IntPtr(Marshal.SizeOf(typeof(T))), out lpNumberOfBytesRead);
            var retValue = (T) Marshal.PtrToStructure(buffer, typeof(T));
            Marshal.FreeCoTaskMem(buffer);
            return retValue;
        }

        public ushort GetUInt16(IntPtr address, long offset = 0) {
            byte[] value = new byte[4];
            this.Peek(new IntPtr(address.ToInt64() + offset), value);
            return BitConverter.TryToUInt16(value, 0);
        }

        public uint GetUInt32(IntPtr address, long offset = 0) {
            byte[] value = new byte[4];
            this.Peek(new IntPtr(address.ToInt64() + offset), value);
            return BitConverter.TryToUInt32(value, 0);
        }

        public ulong GetUInt64(IntPtr address, long offset = 0) {
            byte[] value = new byte[8];
            this.Peek(new IntPtr(address.ToInt64() + offset), value);
            return BitConverter.TryToUInt32(value, 0);
        }

        public bool Peek(IntPtr address, byte[] buffer) {
            IntPtr lpNumberOfBytesRead;
            return UnsafeNativeMethods.ReadProcessMemory(Instance.ProcessHandle, address, buffer, new IntPtr(buffer.Length), out lpNumberOfBytesRead);
        }

        public IntPtr ReadPointer(IntPtr address, long offset = 0) {
            if (this.ProcessModel.IsWin64) {
                byte[] win64 = new byte[8];
                this.Peek(new IntPtr(address.ToInt64() + offset), win64);
                return new IntPtr(BitConverter.TryToInt64(win64, 0));
            }

            byte[] win32 = new byte[4];
            this.Peek(new IntPtr(address.ToInt64() + offset), win32);
            return IntPtr.Add(IntPtr.Zero, BitConverter.TryToInt32(win32, 0));
        }

        public IntPtr ResolvePointerPath(IEnumerable<long> path, IntPtr baseAddress, bool IsASMSignature = false) {
            IntPtr nextAddress = baseAddress;
            foreach (var offset in path) {
                try {
                    baseAddress = new IntPtr(nextAddress.ToInt64() + offset);
                    if (baseAddress == IntPtr.Zero) {
                        return IntPtr.Zero;
                    }

                    if (IsASMSignature) {
                        nextAddress = baseAddress + Instance.GetInt32(new IntPtr(baseAddress.ToInt64())) + 4;
                        IsASMSignature = false;
                    }
                    else {
                        nextAddress = Instance.ReadPointer(baseAddress);
                    }
                }
                catch {
                    return IntPtr.Zero;
                }
            }

            return baseAddress;
        }

        public void SetProcess(ProcessModel processModel, string gameLanguage = "English", string patchVersion = "latest", bool useLocalCache = true, bool scanAllMemoryRegions = false) {
            this.ProcessModel = processModel;
            this.GameLanguage = gameLanguage;
            this.UseLocalCache = useLocalCache;

            this.UnsetProcess();

            try {
                this.ProcessHandle = UnsafeNativeMethods.OpenProcess(UnsafeNativeMethods.ProcessAccessFlags.PROCESS_VM_ALL, false, (uint) this.ProcessModel.ProcessID);
            }
            catch (Exception) {
                this.ProcessHandle = processModel.Process.Handle;
            }
            finally {
                Constants.ProcessHandle = this.ProcessHandle;
                this.IsAttached = true;
            }

            if (this.IsNewInstance) {
                this.IsNewInstance = false;

                ActionLookup.Resolve();
                StatusEffectLookup.Resolve();
                ZoneLookup.Resolve();

                this.ResolveMemoryStructures(processModel, patchVersion);
            }

            this.AttachmentWorker = new AttachmentWorker();
            this.AttachmentWorker.StartScanning(processModel);

            this.SystemModules.Clear();
            this.GetProcessModules();

            Scanner.Instance.Locations.Clear();
            Scanner.Instance.LoadOffsets(Signatures.Resolve(processModel, patchVersion), scanAllMemoryRegions);
        }

        public void UnsetProcess() {
            if (this.AttachmentWorker != null) {
                this.AttachmentWorker.StopScanning();
                this.AttachmentWorker.Dispose();
            }

            try {
                if (this.IsAttached) {
                    UnsafeNativeMethods.CloseHandle(Instance.ProcessHandle);
                }
            }
            catch (Exception) {
                // IGNORED
            }
            finally {
                Constants.ProcessHandle = this.ProcessHandle = IntPtr.Zero;
                this.IsAttached = false;
            }
        }

        internal ProcessModule GetModuleByAddress(IntPtr address) {
            try {
                for (var i = 0; i < this.SystemModules.Count; i++) {
                    ProcessModule module = this.SystemModules[i];
                    var baseAddress = this.ProcessModel.IsWin64
                                          ? module.BaseAddress.ToInt64()
                                          : module.BaseAddress.ToInt32();
                    if (baseAddress <= (long) address && baseAddress + module.ModuleMemorySize >= (long) address) {
                        return module;
                    }
                }

                return null;
            }
            catch (Exception) {
                return null;
            }
        }

        internal bool IsSystemModule(IntPtr address) {
            ProcessModule moduleByAddress = this.GetModuleByAddress(address);
            if (moduleByAddress != null) {
                foreach (ProcessModule module in this.SystemModules) {
                    if (module.ModuleName == moduleByAddress.ModuleName) {
                        return true;
                    }
                }
            }

            return false;
        }

        internal void ResolveMemoryStructures(ProcessModel processModel, string patchVersion = "latest") {
            this.Structures = APIHelper.GetStructures(processModel, patchVersion);
        }

        protected internal virtual void RaiseException(Logger logger, Exception e, bool levelIsError = false) {
            this.ExceptionEvent?.Invoke(this, new ExceptionEvent(this, logger, e, levelIsError));
        }

        protected internal virtual void RaiseSignaturesFound(Logger logger, Dictionary<string, Signature> signatures, long processingTime) {
            this.SignaturesFoundEvent?.Invoke(this, new SignaturesFoundEvent(this, logger, signatures, processingTime));
        }

        private void GetProcessModules() {
            ProcessModuleCollection modules = this.ProcessModel.Process.Modules;

            for (var i = 0; i < modules.Count; i++) {
                ProcessModule module = modules[i];
                this.SystemModules.Add(module);
            }
        }

        internal struct MemoryBlock {
            public long Length;

            public long Start;
        }
    }
}