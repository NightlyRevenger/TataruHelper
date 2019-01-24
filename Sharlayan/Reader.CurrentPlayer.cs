// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Reader.CurrentPlayer.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Reader.CurrentPlayer.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan {
    using System;

    using Sharlayan.Core;
    using Sharlayan.Models.ReadResults;
    using Sharlayan.Utilities;

    public static partial class Reader {
        public static bool CanGetPlayerInfo() {
            var canRead = Scanner.Instance.Locations.ContainsKey(Signatures.CharacterMapKey) && Scanner.Instance.Locations.ContainsKey(Signatures.PlayerInformationKey);
            if (canRead) {
                // OTHER STUFF?
            }

            return canRead;
        }

        public static CurrentPlayerResult GetCurrentPlayer() {
            var result = new CurrentPlayerResult();

            if (!CanGetPlayerInfo() || !MemoryHandler.Instance.IsAttached) {
                return result;
            }

            var PlayerInfoMap = (IntPtr) Scanner.Instance.Locations[Signatures.PlayerInformationKey];

            if (PlayerInfoMap.ToInt64() <= 6496) {
                return result;
            }

            try
            {
                byte[] source = MemoryHandler.Instance.GetByteArray(PlayerInfoMap, MemoryHandler.Instance.Structures.CurrentPlayer.SourceSize);

                try
                {
                    result.CurrentPlayer = CurrentPlayerResolver.ResolvePlayerFromBytes(source);
                }
                catch (Exception ex)
                {
                    MemoryHandler.Instance.RaiseException(Logger, ex, true);
                }

                if (CanGetAgroEntities()) {
                    var agroCount = MemoryHandler.Instance.GetInt16(Scanner.Instance.Locations[Signatures.AgroCountKey]);
                    var agroStructure = (IntPtr) Scanner.Instance.Locations[Signatures.AgroMapKey];

                    if (agroCount > 0 && agroCount < 32 && agroStructure.ToInt64() > 0) {
                        var agroSourceSize = MemoryHandler.Instance.Structures.EnmityItem.SourceSize;
                        for (uint i = 0; i < agroCount; i++) {
                            var address = new IntPtr(agroStructure.ToInt64() + i * agroSourceSize);
                            var agroEntry = new EnmityItem {
                                ID = (uint) MemoryHandler.Instance.GetPlatformInt(address, MemoryHandler.Instance.Structures.EnmityItem.ID),
                                Name = MemoryHandler.Instance.GetString(address + MemoryHandler.Instance.Structures.EnmityItem.Name),
                                Enmity = MemoryHandler.Instance.GetUInt32(address + MemoryHandler.Instance.Structures.EnmityItem.Enmity)
                            };
                            if (agroEntry.ID > 0) {
                                result.CurrentPlayer.EnmityItems.Add(agroEntry);
                            }
                        }
                    }
                }

            }
            catch (Exception ex) {
                MemoryHandler.Instance.RaiseException(Logger, ex, true);
            }

            return result;
        }
    }
}