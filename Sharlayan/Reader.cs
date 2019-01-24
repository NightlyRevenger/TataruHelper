// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Reader.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Reader.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan {
    using System;

    using NLog;

    using Sharlayan.Core;
    using Sharlayan.Core.Enums;

    public static partial class Reader {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static bool CanGetAgroEntities() {
            var canRead = Scanner.Instance.Locations.ContainsKey(Signatures.AgroCountKey) && Scanner.Instance.Locations.ContainsKey(Signatures.AgroMapKey);
            if (canRead) {
                // OTHER STUFF?
            }

            return canRead;
        }

        public static bool CanGetEnmityEntities() {
            var canRead = Scanner.Instance.Locations.ContainsKey(Signatures.EnmityCountKey) && Scanner.Instance.Locations.ContainsKey(Signatures.EnmityMapKey);
            if (canRead) {
                // OTHER STUFF?
            }

            return canRead;
        }

        private static void EnsureMapAndZone(ActorItem entry) {
            if (Scanner.Instance.Locations.ContainsKey(Signatures.MapInformationKey)) {
                try {
                    entry.MapTerritory = (uint) MemoryHandler.Instance.GetPlatformUInt(Scanner.Instance.Locations[Signatures.MapInformationKey]);
                    entry.MapID = (uint) MemoryHandler.Instance.GetPlatformUInt(Scanner.Instance.Locations[Signatures.MapInformationKey], 8);
                }
                catch (Exception ex) {
                    MemoryHandler.Instance.RaiseException(Logger, ex, true);
                }
            }

            if (Scanner.Instance.Locations.ContainsKey(Signatures.ZoneInformationKey)) {
                try {
                    entry.MapIndex = (uint) MemoryHandler.Instance.GetPlatformUInt(Scanner.Instance.Locations[Signatures.ZoneInformationKey], 8);

                    // current map is 0 if the map the actor is in does not have more than 1 layer.
                    // if the map has more than 1 layer, overwrite the map id.
                    var currentActiveMapID = (uint) MemoryHandler.Instance.GetPlatformUInt(Scanner.Instance.Locations[Signatures.ZoneInformationKey]);
                    if (currentActiveMapID > 0) {
                        entry.MapID = currentActiveMapID;
                    }
                }
                catch (Exception ex) {
                    MemoryHandler.Instance.RaiseException(Logger, ex, true);
                }
            }
        }

        private static (ushort EventObjectTypeID, Actor.EventObjectType EventObjectType) GetEventObjectType(IntPtr address) {
            IntPtr eventObjectTypePointer = IntPtr.Add(address, MemoryHandler.Instance.Structures.ActorItem.EventObjectType);
            IntPtr eventObjectTypeAddress = MemoryHandler.Instance.ReadPointer(eventObjectTypePointer, 4);

            var eventObjectTypeID = MemoryHandler.Instance.GetUInt16(eventObjectTypeAddress);

            return (eventObjectTypeID, (Actor.EventObjectType) eventObjectTypeID);
        }
    }
}