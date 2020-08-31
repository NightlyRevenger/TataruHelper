// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Reader.ChatLog.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Reader.ChatLog.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Sharlayan.Core;
    using Sharlayan.Models;
    using Sharlayan.Models.ReadResults;

    public static partial class Reader
    {
        public static bool CanGetChatLog()
        {
            var canRead = Scanner.Instance.Locations.ContainsKey(Signatures.ChatLogKey);
            if (canRead)
            {
                // OTHER STUFF?
            }

            return canRead;
        }

        public static ChatLogResult GetChatLog(int previousArrayIndex = 0, int previousOffset = 0)
        {
            var result = new ChatLogResult();

            if (!CanGetChatLog() || !MemoryHandler.Instance.IsAttached)
            {
                return result;
            }

            ChatLogReader.PreviousArrayIndex = previousArrayIndex;
            ChatLogReader.PreviousOffset = previousOffset;

            Signature ChatLogKey = Scanner.Instance.Locations[Signatures.ChatLogKey];

            var chatPointerMap = (IntPtr)Scanner.Instance.Locations[Signatures.ChatLogKey];

            if (chatPointerMap.ToInt64() <= 20)
            {
                return result;
            }

            List<List<byte>> buffered = new List<List<byte>>();

            try
            {
                var LineCount = (uint)MemoryHandler.Instance.GetPlatformUInt(chatPointerMap);

                ChatLogReader.Indexes.Clear();
                ChatLogReader.ChatLogPointers = new ChatLogPointers
                {
                    LineCount = (uint)MemoryHandler.Instance.GetPlatformUInt(chatPointerMap),
                    OffsetArrayStart = MemoryHandler.Instance.GetPlatformUInt(chatPointerMap, MemoryHandler.Instance.Structures.ChatLogPointers.OffsetArrayStart),
                    OffsetArrayPos = MemoryHandler.Instance.GetPlatformUInt(chatPointerMap, MemoryHandler.Instance.Structures.ChatLogPointers.OffsetArrayPos),
                    OffsetArrayEnd = MemoryHandler.Instance.GetPlatformUInt(chatPointerMap, MemoryHandler.Instance.Structures.ChatLogPointers.OffsetArrayEnd),
                    LogStart = MemoryHandler.Instance.GetPlatformUInt(chatPointerMap, MemoryHandler.Instance.Structures.ChatLogPointers.LogStart),
                    LogNext = MemoryHandler.Instance.GetPlatformUInt(chatPointerMap, MemoryHandler.Instance.Structures.ChatLogPointers.LogNext),
                    LogEnd = MemoryHandler.Instance.GetPlatformUInt(chatPointerMap, MemoryHandler.Instance.Structures.ChatLogPointers.LogEnd),
                };

                ChatLogReader.EnsureArrayIndexes();

                var currentArrayIndex = (ChatLogReader.ChatLogPointers.OffsetArrayPos - ChatLogReader.ChatLogPointers.OffsetArrayStart) / 4;
                if (ChatLogReader.ChatLogFirstRun)
                {
                    ChatLogReader.ChatLogFirstRun = false;
                    ChatLogReader.PreviousOffset = ChatLogReader.Indexes[(int)currentArrayIndex - 1];
                    ChatLogReader.PreviousArrayIndex = (int)currentArrayIndex - 1;
                }
                else
                {
                    if (currentArrayIndex < ChatLogReader.PreviousArrayIndex)
                    {
                        buffered.AddRange(ChatLogReader.ResolveEntries(ChatLogReader.PreviousArrayIndex, 1000));
                        ChatLogReader.PreviousOffset = 0;
                        ChatLogReader.PreviousArrayIndex = 0;
                    }

                    if (ChatLogReader.PreviousArrayIndex < currentArrayIndex)
                    {
                        buffered.AddRange(ChatLogReader.ResolveEntries(ChatLogReader.PreviousArrayIndex, (int)currentArrayIndex));
                    }

                    ChatLogReader.PreviousArrayIndex = (int)currentArrayIndex;
                }
            }
            catch (Exception ex)
            {
                MemoryHandler.Instance.RaiseException(Logger, ex, true);
            }

            foreach (List<byte> bytes in buffered.Where(b => b.Count > 0))
            {
                try
                {
                    ChatLogItem chatLogEntry = ChatEntry.Process(bytes.ToArray());
                    if (Regex.IsMatch(chatLogEntry.Combined, @"[\w\d]{4}::?.+"))
                    {
                        result.ChatLogItems.Add(chatLogEntry);
                    }
                }
                catch (Exception ex)
                {
                    MemoryHandler.Instance.RaiseException(Logger, ex, true);
                }
            }

            result.PreviousArrayIndex = ChatLogReader.PreviousArrayIndex;
            result.PreviousOffset = ChatLogReader.PreviousOffset;

            return result;
        }

        private static class ChatLogReader
        {
            public static readonly List<int> Indexes = new List<int>();

            public static bool ChatLogFirstRun = true;

            public static ChatLogPointers ChatLogPointers;

            public static int PreviousArrayIndex;

            public static int PreviousOffset;

            public static void EnsureArrayIndexes()
            {
                Indexes.Clear();
                for (var i = 0; i < 1000; i++)
                {
                    Indexes.Add((int)MemoryHandler.Instance.GetPlatformUInt(new IntPtr(ChatLogPointers.OffsetArrayStart + i * 4)));
                }
            }

            public static IEnumerable<List<byte>> ResolveEntries(int offset, int length)
            {
                List<List<byte>> entries = new List<List<byte>>();
                for (var i = offset; i < length; i++)
                {
                    EnsureArrayIndexes();
                    var currentOffset = Indexes[i];
                    entries.Add(ResolveEntry(PreviousOffset, currentOffset));
                    PreviousOffset = currentOffset;
                }

                return entries;
            }

            private static List<byte> ResolveEntry(int offset, int length)
            {
                return new List<byte>(MemoryHandler.Instance.GetByteArray(new IntPtr(ChatLogPointers.LogStart + offset), length - offset));
            }
        }
    }
}