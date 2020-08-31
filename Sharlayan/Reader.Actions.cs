// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Reader.Actions.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Reader.Actions.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sharlayan.Core;
    using Sharlayan.Models;
    using Sharlayan.Models.ReadResults;

    using Action = Sharlayan.Core.Enums.Action;
    using BitConverter = Sharlayan.Utilities.BitConverter;

    public static partial class Reader {
        private static readonly Regex KeyBindsRegex = new Regex(@"[\[\]]", RegexOptions.Compiled);

        public static bool CanGetActions() {
            var canRead = Scanner.Instance.Locations.ContainsKey(Signatures.HotBarKey) && Scanner.Instance.Locations.ContainsKey(Signatures.RecastKey);
            if (canRead) {
                // OTHER STUFF
            }

            return canRead;
        }

        public static ActionResult GetActions() {
            var result = new ActionResult();

            if (!CanGetActions() || !MemoryHandler.Instance.IsAttached) {
                return result;
            }

            try {
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.HOTBAR_1));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.HOTBAR_2));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.HOTBAR_3));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.HOTBAR_4));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.HOTBAR_5));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.HOTBAR_6));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.HOTBAR_7));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.HOTBAR_8));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.HOTBAR_9));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.HOTBAR_10));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.CROSS_HOTBAR_1));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.CROSS_HOTBAR_2));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.CROSS_HOTBAR_3));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.CROSS_HOTBAR_4));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.CROSS_HOTBAR_5));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.CROSS_HOTBAR_6));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.CROSS_HOTBAR_7));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.CROSS_HOTBAR_8));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.PETBAR));
                result.ActionContainers.Add(GetHotBarRecast(Action.Container.CROSS_PETBAR));
            }
            catch (Exception ex) {
                MemoryHandler.Instance.RaiseException(Logger, ex, true);
            }

            return result;
        }

        private static ActionContainer GetHotBarRecast(Action.Container type) {
            Signature HotBarMap = Scanner.Instance.Locations[Signatures.HotBarKey];
            Signature RecastMap = Scanner.Instance.Locations[Signatures.RecastKey];

            var hotbarContainerSize = MemoryHandler.Instance.Structures.HotBarItem.ContainerSize;
            IntPtr hotbarContainerAddress = IntPtr.Add(HotBarMap, (int) type * hotbarContainerSize);

            var recastContainerSize = MemoryHandler.Instance.Structures.RecastItem.ContainerSize;
            IntPtr recastContainerAddress = IntPtr.Add(RecastMap, (int) type * recastContainerSize);

            var container = new ActionContainer {
                ContainerType = type,
            };

            var canUseKeyBinds = false;

            var hotbarItemSize = MemoryHandler.Instance.Structures.HotBarItem.ItemSize;
            var recastItemSize = MemoryHandler.Instance.Structures.RecastItem.ItemSize;

            int limit;

            switch (type) {
                case Action.Container.CROSS_HOTBAR_1:
                case Action.Container.CROSS_HOTBAR_2:
                case Action.Container.CROSS_HOTBAR_3:
                case Action.Container.CROSS_HOTBAR_4:
                case Action.Container.CROSS_HOTBAR_5:
                case Action.Container.CROSS_HOTBAR_6:
                case Action.Container.CROSS_HOTBAR_7:
                case Action.Container.CROSS_HOTBAR_8:
                case Action.Container.CROSS_PETBAR:
                    limit = 16;
                    break;
                default:
                    limit = 16;
                    canUseKeyBinds = true;
                    break;
            }

            byte[] hotbarItemsSource = MemoryHandler.Instance.GetByteArray(hotbarContainerAddress, hotbarContainerSize);
            byte[] recastItemsSource = MemoryHandler.Instance.GetByteArray(recastContainerAddress, recastContainerSize);

            for (var i = 0; i < limit; i++) {
                byte[] hotbarSource = new byte[hotbarItemSize];
                byte[] recastSource = new byte[recastItemSize];

                Buffer.BlockCopy(hotbarItemsSource, i * hotbarItemSize, hotbarSource, 0, hotbarItemSize);
                Buffer.BlockCopy(recastItemsSource, i * recastItemSize, recastSource, 0, recastItemSize);

                var name = MemoryHandler.Instance.GetStringFromBytes(hotbarSource, MemoryHandler.Instance.Structures.HotBarItem.Name);
                var slot = i;

                if (string.IsNullOrWhiteSpace(name)) {
                    continue;
                }

                var item = new ActionItem {
                    Name = name,
                    ID = BitConverter.TryToInt16(hotbarSource, MemoryHandler.Instance.Structures.HotBarItem.ID),
                    KeyBinds = MemoryHandler.Instance.GetStringFromBytes(hotbarSource, MemoryHandler.Instance.Structures.HotBarItem.KeyBinds),
                    Slot = slot,
                };

                if (canUseKeyBinds) {
                    if (!string.IsNullOrWhiteSpace(item.KeyBinds)) {
                        item.Name = item.Name.Replace($" {item.KeyBinds}", string.Empty);
                        item.KeyBinds = KeyBindsRegex.Replace(item.KeyBinds, string.Empty);
                        List<string> buttons = item.KeyBinds.Split(
                            new[] {
                                '+',
                            }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        if (buttons.Count > 0) {
                            item.ActionKey = buttons.Last();
                        }

                        if (buttons.Count > 1) {
                            for (var x = 0; x < buttons.Count - 1; x++) {
                                item.Modifiers.Add(buttons[x]);
                            }
                        }
                    }
                }

                item.Category = BitConverter.TryToInt32(recastSource, MemoryHandler.Instance.Structures.RecastItem.Category);
                item.Type = BitConverter.TryToInt32(recastSource, MemoryHandler.Instance.Structures.RecastItem.Type);
                item.Icon = BitConverter.TryToInt32(recastSource, MemoryHandler.Instance.Structures.RecastItem.Icon);
                item.CoolDownPercent = recastSource[MemoryHandler.Instance.Structures.RecastItem.CoolDownPercent];
                item.IsAvailable = BitConverter.TryToBoolean(recastSource, MemoryHandler.Instance.Structures.RecastItem.IsAvailable);

                var remainingCost = BitConverter.TryToInt32(recastSource, MemoryHandler.Instance.Structures.RecastItem.RemainingCost);

                item.RemainingCost = remainingCost != -1
                                         ? remainingCost
                                         : 0;
                item.Amount = BitConverter.TryToInt32(recastSource, MemoryHandler.Instance.Structures.RecastItem.Amount);
                item.InRange = BitConverter.TryToBoolean(recastSource, MemoryHandler.Instance.Structures.RecastItem.InRange);
                item.IsProcOrCombo = recastSource[MemoryHandler.Instance.Structures.RecastItem.ActionProc] > 0;

                container.ActionItems.Add(item);
            }

            return container;
        }
    }
}