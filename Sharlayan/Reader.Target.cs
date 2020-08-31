// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Reader.Target.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Reader.Target.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan {
    using System;

    using Sharlayan.Core;
    using Sharlayan.Core.Enums;
    using Sharlayan.Delegates;
    using Sharlayan.Models.ReadResults;
    using Sharlayan.Utilities;

    using BitConverter = Sharlayan.Utilities.BitConverter;

    public static partial class Reader {
        public static bool CanGetTargetInfo() {
            var canRead = Scanner.Instance.Locations.ContainsKey(Signatures.CharacterMapKey) && Scanner.Instance.Locations.ContainsKey(Signatures.TargetKey);
            if (canRead) {
                // OTHER STUFF?
            }

            return canRead;
        }

        public static TargetResult GetTargetInfo() {
            var result = new TargetResult();

            if (!CanGetTargetInfo() || !MemoryHandler.Instance.IsAttached) {
                return result;
            }

            try {
                var targetAddress = (IntPtr) Scanner.Instance.Locations[Signatures.TargetKey];

                if (targetAddress.ToInt64() > 0) {
                    byte[] targetInfoSource = MemoryHandler.Instance.GetByteArray(targetAddress, MemoryHandler.Instance.Structures.TargetInfo.SourceSize);

                    var currentTarget = MemoryHandler.Instance.GetPlatformIntFromBytes(targetInfoSource, MemoryHandler.Instance.Structures.TargetInfo.Current);
                    var mouseOverTarget = MemoryHandler.Instance.GetPlatformIntFromBytes(targetInfoSource, MemoryHandler.Instance.Structures.TargetInfo.MouseOver);
                    var focusTarget = MemoryHandler.Instance.GetPlatformIntFromBytes(targetInfoSource, MemoryHandler.Instance.Structures.TargetInfo.Focus);
                    var previousTarget = MemoryHandler.Instance.GetPlatformIntFromBytes(targetInfoSource, MemoryHandler.Instance.Structures.TargetInfo.Previous);

                    var currentTargetID = BitConverter.TryToUInt32(targetInfoSource, MemoryHandler.Instance.Structures.TargetInfo.CurrentID);

                    if (currentTarget > 0) {
                        try {
                            ActorItem entry = GetTargetActorItemFromSource(currentTarget);
                            currentTargetID = entry.ID;
                            if (entry.IsValid) {
                                result.TargetsFound = true;
                                result.TargetInfo.CurrentTarget = entry;
                            }
                        }
                        catch (Exception ex) {
                            MemoryHandler.Instance.RaiseException(Logger, ex, true);
                        }
                    }

                    if (mouseOverTarget > 0) {
                        try {
                            ActorItem entry = GetTargetActorItemFromSource(mouseOverTarget);
                            if (entry.IsValid) {
                                result.TargetsFound = true;
                                result.TargetInfo.MouseOverTarget = entry;
                            }
                        }
                        catch (Exception ex) {
                            MemoryHandler.Instance.RaiseException(Logger, ex, true);
                        }
                    }

                    if (focusTarget > 0) {
                        try {
                            ActorItem entry = GetTargetActorItemFromSource(focusTarget);
                            if (entry.IsValid) {
                                result.TargetsFound = true;
                                result.TargetInfo.FocusTarget = entry;
                            }
                        }
                        catch (Exception ex) {
                            MemoryHandler.Instance.RaiseException(Logger, ex, true);
                        }
                    }

                    if (previousTarget > 0) {
                        try {
                            ActorItem entry = GetTargetActorItemFromSource(previousTarget);
                            if (entry.IsValid) {
                                result.TargetsFound = true;
                                result.TargetInfo.PreviousTarget = entry;
                            }
                        }
                        catch (Exception ex) {
                            MemoryHandler.Instance.RaiseException(Logger, ex, true);
                        }
                    }

                    if (currentTargetID > 0) {
                        result.TargetsFound = true;
                        result.TargetInfo.CurrentTargetID = currentTargetID;
                    }
                }

                if (result.TargetInfo.CurrentTargetID > 0) {
                    try {
                        if (CanGetEnmityEntities()) {
                            var enmityCount = MemoryHandler.Instance.GetInt16(Scanner.Instance.Locations[Signatures.EnmityCountKey]);
                            var enmityStructure = (IntPtr) Scanner.Instance.Locations[Signatures.EnmityMapKey];

                            if (enmityCount > 0 && enmityCount < 16 && enmityStructure.ToInt64() > 0) {
                                var enmitySourceSize = MemoryHandler.Instance.Structures.EnmityItem.SourceSize;
                                for (uint i = 0; i < enmityCount; i++) {
                                    try {
                                        var address = new IntPtr(enmityStructure.ToInt64() + i * enmitySourceSize);
                                        var enmityEntry = new EnmityItem {
                                            ID = (uint) MemoryHandler.Instance.GetPlatformInt(address, MemoryHandler.Instance.Structures.EnmityItem.ID),
                                            // Name = MemoryHandler.Instance.GetString(address + MemoryHandler.Instance.Structures.EnmityItem.Name),
                                            Enmity = MemoryHandler.Instance.GetUInt32(address + MemoryHandler.Instance.Structures.EnmityItem.Enmity),
                                        };
                                        if (enmityEntry.ID <= 0) {
                                            continue;
                                        }

                                        if (string.IsNullOrWhiteSpace(enmityEntry.Name)) {
                                            ActorItem pc = PCWorkerDelegate.GetActorItem(enmityEntry.ID);
                                            ActorItem npc = NPCWorkerDelegate.GetActorItem(enmityEntry.ID);
                                            ActorItem monster = MonsterWorkerDelegate.GetActorItem(enmityEntry.ID);
                                            try {
                                                enmityEntry.Name = (pc ?? npc).Name ?? monster.Name;
                                            }
                                            catch (Exception ex) {
                                                MemoryHandler.Instance.RaiseException(Logger, ex, true);
                                            }
                                        }

                                        result.TargetInfo.EnmityItems.Add(enmityEntry);
                                    }
                                    catch (Exception ex) {
                                        MemoryHandler.Instance.RaiseException(Logger, ex, true);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex) {
                        MemoryHandler.Instance.RaiseException(Logger, ex, true);
                    }
                }
            }
            catch (Exception ex) {
                MemoryHandler.Instance.RaiseException(Logger, ex, true);
            }

            return result;
        }

        private static ActorItem GetTargetActorItemFromSource(long address) {
            var targetAddress = new IntPtr(address);

            byte[] source = MemoryHandler.Instance.GetByteArray(targetAddress, MemoryHandler.Instance.Structures.TargetInfo.Size);
            ActorItem entry = ActorItemResolver.ResolveActorFromBytes(source);

            if (entry.Type == Actor.Type.EventObject) {
                var (EventObjectTypeID, EventObjectType) = GetEventObjectType(targetAddress);
                entry.EventObjectTypeID = EventObjectTypeID;
                entry.EventObjectType = EventObjectType;
            }

            EnsureMapAndZone(entry);

            return entry;
        }
    }
}