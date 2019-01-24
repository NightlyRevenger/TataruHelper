// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Reader.PartyMembers.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Reader.PartyMembers.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan {
    using System;
    using System.Collections.Generic;

    using Sharlayan.Core;
    using Sharlayan.Delegates;
    using Sharlayan.Models;
    using Sharlayan.Models.ReadResults;
    using Sharlayan.Utilities;

    using BitConverter = Sharlayan.Utilities.BitConverter;

    public static partial class Reader {
        public static bool CanGetPartyMembers() {
            var canRead = Scanner.Instance.Locations.ContainsKey(Signatures.CharacterMapKey) && Scanner.Instance.Locations.ContainsKey(Signatures.PartyMapKey) && Scanner.Instance.Locations.ContainsKey(Signatures.PartyCountKey);
            if (canRead) {
                // OTHER STUFF?
            }

            return canRead;
        }

        public static PartyResult GetPartyMembers() {
            var result = new PartyResult();

            if (!CanGetPartyMembers() || !MemoryHandler.Instance.IsAttached) {
                return result;
            }

            var PartyInfoMap = (IntPtr) Scanner.Instance.Locations[Signatures.PartyMapKey];
            Signature PartyCountMap = Scanner.Instance.Locations[Signatures.PartyCountKey];

            foreach (KeyValuePair<uint, PartyMember> kvp in PartyWorkerDelegate.PartyMembers) {
                result.RemovedPartyMembers.TryAdd(kvp.Key, kvp.Value.Clone());
            }

            try {
                var partyCount = MemoryHandler.Instance.GetByte(PartyCountMap);
                var sourceSize = MemoryHandler.Instance.Structures.PartyMember.SourceSize;

                if (partyCount > 1 && partyCount < 9) {
                    for (uint i = 0; i < partyCount; i++) {
                        var address = PartyInfoMap.ToInt64() + i * (uint) sourceSize;
                        byte[] source = MemoryHandler.Instance.GetByteArray(new IntPtr(address), sourceSize);
                        var ID = BitConverter.TryToUInt32(source, MemoryHandler.Instance.Structures.PartyMember.ID);
                        ActorItem existing = null;
                        var newEntry = false;

                        if (result.RemovedPartyMembers.ContainsKey(ID)) {
                            result.RemovedPartyMembers.TryRemove(ID, out PartyMember removedPartyMember);
                            if (MonsterWorkerDelegate.ActorItems.ContainsKey(ID)) {
                                existing = MonsterWorkerDelegate.GetActorItem(ID);
                            }

                            if (PCWorkerDelegate.ActorItems.ContainsKey(ID)) {
                                existing = PCWorkerDelegate.GetActorItem(ID);
                            }
                        }
                        else {
                            newEntry = true;
                        }

                        PartyMember entry = PartyMemberResolver.ResolvePartyMemberFromBytes(source, existing);
                        if (!entry.IsValid) {
                            continue;
                        }

                        if (existing != null) {
                            continue;
                        }

                        if (newEntry) {
                            PartyWorkerDelegate.EnsurePartyMember(entry.ID, entry);
                            result.NewPartyMembers.TryAdd(entry.ID, entry.Clone());
                        }
                    }
                }

                if (partyCount <= 1) {
                    PartyMember entry = PartyMemberResolver.ResolvePartyMemberFromBytes(Array.Empty<byte>(), PCWorkerDelegate.CurrentUser);
                    if (result.RemovedPartyMembers.ContainsKey(entry.ID)) {
                        result.RemovedPartyMembers.TryRemove(entry.ID, out PartyMember removedPartyMember);
                    }

                    PartyWorkerDelegate.EnsurePartyMember(entry.ID, entry);
                }
            }
            catch (Exception ex) {
                MemoryHandler.Instance.RaiseException(Logger, ex, true);
            }

            try {
                // REMOVE OLD PARTY MEMBERS FROM LIVE CURRENT DICTIONARY
                foreach (KeyValuePair<uint, PartyMember> kvp in result.RemovedPartyMembers) {
                    PartyWorkerDelegate.RemovePartyMember(kvp.Key);
                }
            }
            catch (Exception ex) {
                MemoryHandler.Instance.RaiseException(Logger, ex, true);
            }

            return result;
        }
    }
}