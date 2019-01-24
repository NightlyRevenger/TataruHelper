// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PartyMemberResolver.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   PartyMemberResolver.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Utilities {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NLog;

    using Sharlayan.Core;
    using Sharlayan.Core.Enums;
    using Sharlayan.Delegates;

    internal static class PartyMemberResolver {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static PartyMember ResolvePartyMemberFromBytes(byte[] source, ActorItem actorItem = null) {
            if (actorItem != null) {
                var entry = new PartyMember {
                    X = actorItem.X,
                    Y = actorItem.Y,
                    Z = actorItem.Z,
                    Coordinate = actorItem.Coordinate,
                    ID = actorItem.ID,
                    UUID = actorItem.UUID,
                    Name = actorItem.Name,
                    Job = actorItem.Job,
                    Level = actorItem.Level,
                    HPCurrent = actorItem.HPCurrent,
                    HPMax = actorItem.HPMax,
                    MPCurrent = actorItem.MPCurrent,
                    MPMax = actorItem.MPMax,
                    HitBoxRadius = actorItem.HitBoxRadius
                };
                entry.StatusItems.AddRange(actorItem.StatusItems);
                CleanXPValue(ref entry);
                return entry;
            }
            else {
                var defaultStatusEffectOffset = MemoryHandler.Instance.Structures.PartyMember.DefaultStatusEffectOffset;
                var entry = new PartyMember();
                try {
                    entry.X = BitConverter.TryToSingle(source, MemoryHandler.Instance.Structures.PartyMember.X);
                    entry.Z = BitConverter.TryToSingle(source, MemoryHandler.Instance.Structures.PartyMember.Z);
                    entry.Y = BitConverter.TryToSingle(source, MemoryHandler.Instance.Structures.PartyMember.Y);
                    entry.Coordinate = new Coordinate(entry.X, entry.Z, entry.Z);
                    entry.ID = BitConverter.TryToUInt32(source, MemoryHandler.Instance.Structures.PartyMember.ID);
                    entry.UUID = Guid.NewGuid().ToString();
                    entry.Name = MemoryHandler.Instance.GetStringFromBytes(source, MemoryHandler.Instance.Structures.PartyMember.Name);
                    entry.JobID = source[MemoryHandler.Instance.Structures.PartyMember.Job];
                    entry.Job = (Actor.Job) entry.JobID;
                    entry.HitBoxRadius = 0.5f;

                    entry.Level = source[MemoryHandler.Instance.Structures.PartyMember.Level];
                    entry.HPCurrent = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.PartyMember.HPCurrent);
                    entry.HPMax = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.PartyMember.HPMax);
                    entry.MPCurrent = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.PartyMember.MPCurrent);
                    entry.MPMax = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.PartyMember.MPMax);
                    const int limit = 15;

                    int statusSize = MemoryHandler.Instance.Structures.StatusItem.SourceSize;
                    byte[] statusesSource = new byte[limit * statusSize];

                    List<StatusItem> foundStatuses = new List<StatusItem>();

                    Buffer.BlockCopy(source, defaultStatusEffectOffset, statusesSource, 0, limit * statusSize);
                    for (var i = 0; i < limit; i++)
                    {
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

                        statusEntry.TargetEntity = null;
                        statusEntry.TargetName = entry.Name;
                        statusEntry.StatusID = StatusID;
                        statusEntry.Stacks = statusSource[MemoryHandler.Instance.Structures.StatusItem.Stacks];
                        statusEntry.Duration = BitConverter.TryToSingle(statusSource, MemoryHandler.Instance.Structures.StatusItem.Duration);
                        statusEntry.CasterID = CasterID;

                        foundStatuses.Add(statusEntry);


                        try
                        {
                            ActorItem pc = PCWorkerDelegate.GetActorItem(statusEntry.CasterID);
                            ActorItem npc = NPCWorkerDelegate.GetActorItem(statusEntry.CasterID);
                            ActorItem monster = MonsterWorkerDelegate.GetActorItem(statusEntry.CasterID);
                            statusEntry.SourceEntity = (pc ?? npc) ?? monster;
                        }
                        catch (Exception ex) {
                            MemoryHandler.Instance.RaiseException(Logger, ex, true);
                        }

                        try {
                            if (statusEntry.StatusID > 0) {
                                Models.XIVDatabase.StatusItem statusInfo = StatusEffectLookup.GetStatusInfo((uint) statusEntry.StatusID);
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

                        if (statusEntry.IsValid())
                        {
                            if (isNewStatus)
                            {
                                entry.StatusItems.Add(statusEntry);
                            }
                            foundStatuses.Add(statusEntry);
                        }
                    }

                    entry.StatusItems.RemoveAll(x => !foundStatuses.Contains(x));

                }
                catch (Exception ex) {
                    MemoryHandler.Instance.RaiseException(Logger, ex, true);
                }

                CleanXPValue(ref entry);
                return entry;
            }
        }

        private static void CleanXPValue(ref PartyMember entity) {
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
        }
    }
}