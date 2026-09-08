using System;
using System.Collections.Generic;

using FFXIVTataruHelper.Services.GameMemory;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.GameMemory
{
    /// <summary>
    /// The table the game keeps of where each of its in-sentence pictures sits
    /// on the sheet: sixteen bytes of header, then sixteen bytes an icon.
    /// </summary>
    [TestFixture]
    public class GameIconSheetTests
    {
        private static byte[] Table(params (int Id, int Left, int Top, int Width, int Height)[] entries)
        {
            var bytes = new byte[16 + entries.Length * 16];
            for (var i = 0; i < entries.Length; i++)
            {
                var at = 16 + i * 16;
                foreach (var (value, offset) in new[]
                         {
                             (entries[i].Id, 0), (entries[i].Left, 2),
                             (entries[i].Top, 4), (entries[i].Width, 6), (entries[i].Height, 8)
                         })
                {
                    BitConverter.GetBytes((ushort)value).CopyTo(bytes, at + offset);
                }
            }

            return bytes;
        }

        /// <summary>
        /// The entry read off a live client on 8 September: the mentor's crown
        /// for combat, twenty by twenty at 208,60.
        /// </summary>
        [Test]
        public void AnIconIsFoundWhereTheTableSays()
        {
            var places = GameIconSheet.Read(Table((79, 208, 60, 20, 20)));

            Assert.That(places.ContainsKey(79), Is.True);
            Assert.That(places[79].Left, Is.EqualTo(208));
            Assert.That(places[79].Top, Is.EqualTo(60));
            Assert.That(places[79].Width, Is.EqualTo(20));
            Assert.That(places[79].Height, Is.EqualTo(20));
        }

        [Test]
        public void EveryIconInTheTableIsRead()
        {
            var places = GameIconSheet.Read(
                Table((79, 208, 60, 20, 20), (80, 228, 60, 20, 20), (81, 100, 80, 20, 20)));

            Assert.That(places.Keys, Is.EquivalentTo(new[] { 79, 80, 81 }));
        }

        /// <summary>
        /// The table has gaps. An id the game has stopped using keeps its row
        /// and is given no size, and a row with no size draws nothing.
        /// </summary>
        [Test]
        public void AnEntryWithNoSizeIsNotAnIcon()
        {
            var places = GameIconSheet.Read(Table((79, 208, 60, 20, 20), (90, 0, 0, 0, 0)));

            Assert.That(places.ContainsKey(90), Is.False);
        }

        /// <summary>
        /// A file that has moved on to another format says so by being
        /// unreadable, and unreadable has to mean no icons rather than wrong
        /// ones drawn from whatever the bytes happened to say.
        /// </summary>
        [TestCase(null)]
        [TestCase(new byte[0])]
        [TestCase(new byte[] { 1, 2, 3, 4 })]
        public void NothingReadable_IsNoIcons(byte[] bytes)
        {
            Assert.That(GameIconSheet.Read(bytes), Is.Empty);
        }
    }
}
