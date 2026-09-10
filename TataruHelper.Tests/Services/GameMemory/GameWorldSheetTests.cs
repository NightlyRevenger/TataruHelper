using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

using FFXIVTataruHelper.Services.GameMemory;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.GameMemory
{
    /// <summary>
    /// The sheet the game keeps its worlds in, read the way the game writes it:
    /// big-endian, a header of columns, then a row at a time with its strings
    /// after its fixed part.
    ///
    /// The shapes here are the ones a live client gave on 2026-09-11: a row is
    /// sixteen bytes, the name is the first string column at offset 0, and the
    /// flag saying a world is one people are on is the second bit of the byte
    /// at offset 13.
    /// </summary>
    [TestFixture]
    public class GameWorldSheetTests
    {
        private const int RowSize = 16;

        private const int NameColumn = 0;

        private const int FlagColumn = 13;

        private static byte[] Header()
        {
            // Two columns: the name, and the byte the public flag lives in.
            var header = new byte[0x20 + 2 * 4];
            Encoding.ASCII.GetBytes("EXHF").CopyTo(header, 0);
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0x06), RowSize);
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0x08), 2);

            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0x20), 0);           // string
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0x22), NameColumn);
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0x24), 26);          // the second bit of a byte
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0x26), FlagColumn);

            return header;
        }

        private static byte[] Rows(params (string Name, bool Public)[] entries)
        {
            var strings = new List<byte>();
            var offsets = new List<int>();

            foreach (var entry in entries)
            {
                offsets.Add(strings.Count);
                strings.AddRange(Encoding.UTF8.GetBytes(entry.Name));
                strings.Add(0);
            }

            var rowBytes = 6 + RowSize;
            var rows = new byte[0x20 + entries.Length * 8 + entries.Length * (rowBytes + strings.Count)];
            Encoding.ASCII.GetBytes("EXDF").CopyTo(rows, 0);
            BinaryPrimitives.WriteUInt32BigEndian(rows.AsSpan(0x08), (uint)(entries.Length * 8));

            var at = 0x20 + entries.Length * 8;

            for (var i = 0; i < entries.Length; i++)
            {
                BinaryPrimitives.WriteUInt32BigEndian(rows.AsSpan(0x20 + i * 8), (uint)i);
                BinaryPrimitives.WriteUInt32BigEndian(rows.AsSpan(0x20 + i * 8 + 4), (uint)at);

                BinaryPrimitives.WriteUInt32BigEndian(rows.AsSpan(at), (uint)(RowSize + strings.Count));
                BinaryPrimitives.WriteUInt16BigEndian(rows.AsSpan(at + 4), 1);

                var fixedAt = at + 6;
                BinaryPrimitives.WriteUInt32BigEndian(rows.AsSpan(fixedAt + NameColumn), (uint)offsets[i]);
                rows[fixedAt + FlagColumn] = (byte)(entries[i].Public ? 0b10 : 0);

                strings.CopyTo(rows, fixedAt + RowSize);
                at = fixedAt + RowSize + strings.Count;
            }

            return rows;
        }

        [Test]
        public void EveryPublicWorldIsRead()
        {
            var worlds = GameWorldSheet.Read(
                Header(), Rows(("Raiden", true), ("Louisoix", true), ("Phoenix", true)));

            Assert.That(worlds, Is.EquivalentTo(new[] { "Raiden", "Louisoix", "Phoenix" }));
        }

        /// <summary>
        /// The sheet holds data-centre names and development leftovers besides.
        /// Chaos is a data centre, not a world, and nobody's name ends in it.
        /// </summary>
        [Test]
        public void WhatTheSheetDoesNotCallPublicIsNotAWorld()
        {
            var worlds = GameWorldSheet.Read(Header(), Rows(("Raiden", true), ("Chaos", false)));

            Assert.That(worlds, Is.EquivalentTo(new[] { "Raiden" }));
        }

        [TestCase("crossworld")]
        [TestCase("reserved1")]
        [TestCase("c-contents")]
        [TestCase("")]
        public void WhatDoesNotLookLikeAWorldIsNotOne(string name)
        {
            var worlds = GameWorldSheet.Read(Header(), Rows((name, true), ("Raiden", true)));

            Assert.That(worlds, Is.EquivalentTo(new[] { "Raiden" }));
        }

        /// <summary>
        /// A file that has moved on to another format says so by being
        /// unreadable, and unreadable has to mean no worlds rather than wrong
        /// ones taken from whatever the bytes happened to say.
        /// </summary>
        [Test]
        public void NothingReadableIsNoWorlds()
        {
            Assert.That(GameWorldSheet.Read(null, null), Is.Empty);
            Assert.That(GameWorldSheet.Read(new byte[0], new byte[0]), Is.Empty);
            Assert.That(GameWorldSheet.Read(Header(), new byte[] { 1, 2, 3, 4 }), Is.Empty);
            Assert.That(GameWorldSheet.Read(new byte[64], Rows(("Raiden", true))), Is.Empty);
        }
    }
}
