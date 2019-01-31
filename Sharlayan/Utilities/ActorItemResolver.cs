// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActorItemResolver.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ActorItemResolver.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Utilities {
    using System;

    using NLog;

    using Sharlayan.Core;
    using Sharlayan.Core.Enums;
    using Sharlayan.Delegates;
    using System.Linq;
    using System.Collections.Generic;

    internal static class ActorItemResolver {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        public static ActorItem ResolveActorFromBytes(byte[] source, bool isCurrentUser = false, ActorItem entry = null)
        {
            entry = entry ?? new ActorItem();
            var defaultBaseOffset = MemoryHandler.Instance.Structures.ActorItem.DefaultBaseOffset;
            var defaultStatOffset = MemoryHandler.Instance.Structures.ActorItem.DefaultStatOffset;
            var defaultStatusEffectOffset = MemoryHandler.Instance.Structures.ActorItem.DefaultStatusEffectOffset;
            try {
                entry.MapTerritory = 0;
                entry.MapIndex = 0;
                entry.MapID = 0;
                entry.TargetID = 0;
                entry.Name = MemoryHandler.Instance.GetStringFromBytes(source, MemoryHandler.Instance.Structures.ActorItem.Name);
                entry.ID = BitConverter.TryToUInt32(source, MemoryHandler.Instance.Structures.ActorItem.ID);
                entry.UUID = string.IsNullOrEmpty(entry.UUID)
                                 ? Guid.NewGuid().ToString()
                                 : entry.UUID;
                entry.NPCID1 = BitConverter.TryToUInt32(source, MemoryHandler.Instance.Structures.ActorItem.NPCID1);
                entry.NPCID2 = BitConverter.TryToUInt32(source, MemoryHandler.Instance.Structures.ActorItem.NPCID2);
                entry.OwnerID = BitConverter.TryToUInt32(source, MemoryHandler.Instance.Structures.ActorItem.OwnerID);
                entry.TypeID = source[MemoryHandler.Instance.Structures.ActorItem.Type];
                entry.Type = (Actor.Type) entry.TypeID;

                entry.TargetTypeID = source[MemoryHandler.Instance.Structures.ActorItem.TargetType];
                entry.TargetType = (Actor.TargetType) entry.TargetTypeID;

                entry.GatheringStatus = source[MemoryHandler.Instance.Structures.ActorItem.GatheringStatus];
                entry.Distance = source[MemoryHandler.Instance.Structures.ActorItem.Distance];

                entry.X = BitConverter.TryToSingle(source, MemoryHandler.Instance.Structures.ActorItem.X + defaultBaseOffset);
                entry.Z = BitConverter.TryToSingle(source, MemoryHandler.Instance.Structures.ActorItem.Z + defaultBaseOffset);
                entry.Y = BitConverter.TryToSingle(source, MemoryHandler.Instance.Structures.ActorItem.Y + defaultBaseOffset);
                entry.Heading = BitConverter.TryToSingle(source, MemoryHandler.Instance.Structures.ActorItem.Heading + defaultBaseOffset);
                entry.HitBoxRadius = BitConverter.TryToSingle(source, MemoryHandler.Instance.Structures.ActorItem.HitBoxRadius + defaultBaseOffset);
                entry.Fate = BitConverter.TryToUInt32(source, MemoryHandler.Instance.Structures.ActorItem.Fate + defaultBaseOffset); // ??
                entry.TargetFlags = source[MemoryHandler.Instance.Structures.ActorItem.TargetFlags]; // ??
                entry.GatheringInvisible = source[MemoryHandler.Instance.Structures.ActorItem.GatheringInvisible]; // ??
                entry.ModelID = BitConverter.TryToUInt32(source, MemoryHandler.Instance.Structures.ActorItem.ModelID);
                entry.ActionStatusID = source[MemoryHandler.Instance.Structures.ActorItem.ActionStatus];
                entry.ActionStatus = (Actor.ActionStatus) entry.ActionStatusID;

                // 0x17D - 0 = Green name, 4 = non-agro (yellow name)
                entry.IsGM = BitConverter.TryToBoolean(source, MemoryHandler.Instance.Structures.ActorItem.IsGM); // ?
                entry.IconID = source[MemoryHandler.Instance.Structures.ActorItem.Icon];
                entry.Icon = (Actor.Icon) entry.IconID;

                entry.StatusID = source[MemoryHandler.Instance.Structures.ActorItem.Status];
                entry.Status = (Actor.Status) entry.StatusID;

                entry.ClaimedByID = BitConverter.TryToUInt32(source, MemoryHandler.Instance.Structures.ActorItem.ClaimedByID);
                var targetID = BitConverter.TryToUInt32(source, MemoryHandler.Instance.Structures.ActorItem.TargetID);
                var pcTargetID = targetID;

                entry.JobID = source[MemoryHandler.Instance.Structures.ActorItem.Job + defaultStatOffset];
                entry.Job = (Actor.Job) entry.JobID;

                entry.Level = source[MemoryHandler.Instance.Structures.ActorItem.Level + defaultStatOffset];
                entry.GrandCompany = source[MemoryHandler.Instance.Structures.ActorItem.GrandCompany + defaultStatOffset];
                entry.GrandCompanyRank = source[MemoryHandler.Instance.Structures.ActorItem.GrandCompanyRank + defaultStatOffset];
                entry.Title = source[MemoryHandler.Instance.Structures.ActorItem.Title + defaultStatOffset];
                entry.HPCurrent = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.ActorItem.HPCurrent + defaultStatOffset);
                entry.HPMax = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.ActorItem.HPMax + defaultStatOffset);
                entry.MPCurrent = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.ActorItem.MPCurrent + defaultStatOffset);
                entry.MPMax = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.ActorItem.MPMax + defaultStatOffset);
                entry.TPCurrent = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.ActorItem.TPCurrent + defaultStatOffset);
                entry.TPMax = 1000;
                entry.GPCurrent = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.ActorItem.GPCurrent + defaultStatOffset);
                entry.GPMax = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.ActorItem.GPMax + defaultStatOffset);
                entry.CPCurrent = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.ActorItem.CPCurrent + defaultStatOffset);
                entry.CPMax = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.ActorItem.CPMax + defaultStatOffset);

                // entry.Race = source[0x2578]; // ??
                // entry.Sex = (Actor.Sex) source[0x2579]; //?
                entry.AgroFlags = source[MemoryHandler.Instance.Structures.ActorItem.AgroFlags];
                entry.CombatFlags = source[MemoryHandler.Instance.Structures.ActorItem.CombatFlags];
                entry.DifficultyRank = source[MemoryHandler.Instance.Structures.ActorItem.DifficultyRank];
                entry.CastingID = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.ActorItem.CastingID); // 0x2C94);
                entry.CastingTargetID = BitConverter.TryToUInt32(source, MemoryHandler.Instance.Structures.ActorItem.CastingTargetID); // 0x2CA0);
                entry.CastingProgress = BitConverter.TryToSingle(source, MemoryHandler.Instance.Structures.ActorItem.CastingProgress); // 0x2CC4);
                entry.CastingTime = BitConverter.TryToSingle(source, MemoryHandler.Instance.Structures.ActorItem.CastingTime); // 0x2DA8);
                entry.Coordinate = new Coordinate(entry.X, entry.Z, entry.Y);
                if (targetID > 0) {
                    entry.TargetID = (int) targetID;
                }
                else {
                    if (pcTargetID > 0) {
                        entry.TargetID = (int) pcTargetID;
                    }
                }

                if (entry.CastingTargetID == 3758096384) {
                    entry.CastingTargetID = 0;
                }

                entry.MapIndex = 0;
                var limit = 60;
                switch (entry.Type) {
                    case Actor.Type.PC:
                        limit = 30;
                        break;
                }

                int statusSize = MemoryHandler.Instance.Structures.StatusItem.SourceSize;
                byte[] statusesSource = new byte[limit * statusSize];

                List<StatusItem> foundStatuses = new List<StatusItem>();

                Buffer.BlockCopy(source, defaultStatusEffectOffset, statusesSource, 0, limit * statusSize);
                for (var i = 0; i < limit; i++) {

                    bool isNewStatus = false;

                    byte[] statusSource = new byte[statusSize];
                    Buffer.BlockCopy(statusesSource, i * statusSize, statusSource, 0, statusSize);

                    short StatusID = BitConverter.TryToInt16(statusSource, MemoryHandler.Instance.Structures.StatusItem.StatusID);
                    uint CasterID = BitConverter.TryToUInt32(statusSource, MemoryHandler.Instance.Structures.StatusItem.CasterID);

                    var statusEntry = entry.StatusItems.FirstOrDefault(x => x.CasterID == CasterID && x.StatusID == StatusID);

                    if (statusEntry == null)
                    {
                        statusEntry = new StatusItem();
                        isNewStatus = true;
                    }

                    statusEntry.TargetEntity = entry;
                    statusEntry.TargetName = entry.Name;
                    statusEntry.StatusID = StatusID;
                    statusEntry.Stacks = statusSource[MemoryHandler.Instance.Structures.StatusItem.Stacks];
                    statusEntry.Duration = BitConverter.TryToSingle(statusSource, MemoryHandler.Instance.Structures.StatusItem.Duration);
                    statusEntry.CasterID = CasterID;



                    try {
                        ActorItem pc = PCWorkerDelegate.GetActorItem(statusEntry.CasterID);
                        ActorItem npc = NPCWorkerDelegate.GetActorItem(statusEntry.CasterID);
                        ActorItem monster = MonsterWorkerDelegate.GetActorItem(statusEntry.CasterID);
                        statusEntry.SourceEntity = (pc ?? npc) ?? monster;
                    }
                    catch (Exception ex) {
                        MemoryHandler.Instance.RaiseException(Logger, ex, true);
                    }

                    try {
                        Models.XIVDatabase.StatusItem statusInfo = StatusEffectLookup.GetStatusInfo((uint) statusEntry.StatusID);
                        if (statusInfo != null) {
                            statusEntry.IsCompanyAction = statusInfo.CompanyAction;
                            var statusKey = statusInfo.Name.English;
                            switch (MemoryHandler.Instance.GameLanguage) {
                                case "French":
                                    statusKey = statusInfo.Name.French;
                                    break;
                                case "Japanese":
                                    statusKey = statusInfo.Name.Japanese;
                                    break;
                                case "German":
                                    statusKey = statusInfo.Name.German;
                                    break;
                                case "Chinese":
                                    statusKey = statusInfo.Name.Chinese;
                                    break;
                                case "Korean":
                                    statusKey = statusInfo.Name.Korean;
                                    break;
                            }

                            statusEntry.StatusName = statusKey;
                        }
                    }
                    catch (Exception) {
                        statusEntry.StatusName = "UNKNOWN";
                    }

                    if (statusEntry.IsValid()) {
                        if (isNewStatus)
                        {
                            entry.StatusItems.Add(statusEntry);
                        }
                        foundStatuses.Add(statusEntry);
                    }
                }
                
                entry.StatusItems.RemoveAll(x => !foundStatuses.Contains(x));

                // handle empty names
                if (string.IsNullOrEmpty(entry.Name)) {
                    if (entry.Type == Actor.Type.EventObject) {
                        entry.Name = $"{nameof(entry.EventObjectTypeID)}: {entry.EventObjectTypeID}";
                    }
                    else {
                        entry.Name = $"{nameof(entry.TypeID)}: {entry.TypeID}";
                    }
                }
            }
            catch (Exception ex) {
                MemoryHandler.Instance.RaiseException(Logger, ex, true);
            }

            CleanXPValue(ref entry);

            if (isCurrentUser) {
                PCWorkerDelegate.CurrentUser = entry;
            }

            return entry;
        }

        private static void CleanXPValue(ref ActorItem entity) {
            if (entity.HPCurrent < 0 || entity.HPMax < 0) {
                entity.HPCurrent = 1;
                entity.HPMax = 1;
            }

            if (entity.HPCurrent > entity.HPMax) {
                if (entity.HPMax == 0) {
                    entity.HPCurrent = 1;
                    entity.HPMax = 1;
                }
                else {
                    entity.HPCurrent = entity.HPMax;
                }
            }

            if (entity.MPCurrent < 0 || entity.MPMax < 0) {
                entity.MPCurrent = 1;
                entity.MPMax = 1;
            }

            if (entity.MPCurrent > entity.MPMax) {
                if (entity.MPMax == 0) {
                    entity.MPCurrent = 1;
                    entity.MPMax = 1;
                }
                else {
                    entity.MPCurrent = entity.MPMax;
                }
            }

            if (entity.GPCurrent < 0 || entity.GPMax < 0) {
                entity.GPCurrent = 1;
                entity.GPMax = 1;
            }

            if (entity.GPCurrent > entity.GPMax) {
                if (entity.GPMax == 0) {
                    entity.GPCurrent = 1;
                    entity.GPMax = 1;
                }
                else {
                    entity.GPCurrent = entity.GPMax;
                }
            }

            if (entity.CPCurrent < 0 || entity.CPMax < 0) {
                entity.CPCurrent = 1;
                entity.CPMax = 1;
            }

            if (entity.CPCurrent > entity.CPMax) {
                if (entity.CPMax == 0) {
                    entity.CPCurrent = 1;
                    entity.CPMax = 1;
                }
                else {
                    entity.CPCurrent = entity.CPMax;
                }
            }
        }
    }
}