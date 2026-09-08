using System;
using System.Text;

using FFXIVTataruHelper.Services.GameMemory;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.GameMemory
{
    /// <summary>
    /// Covers recognising a Utf8String by its shape.
    ///
    /// FFXIVClientStructs has no AddonTalkSubtitle type, so the offset of the
    /// subtitle string is the one value in the reader that cannot be resolved by
    /// reflection and goes stale on a game patch. Finding it again by shape is what
    /// keeps cutscene translation alive across patches, so the recogniser has to be
    /// strict enough not to latch onto arbitrary bytes.
    /// </summary>
    [TestFixture]
    public class Utf8StringDiscoveryTests
    {
        private const int PointerOffset = 0;
        private const int BufUsedOffset = 16;
        private const int LengthOffset = 24;
        private const int InlineFlagOffset = 33;
        private const int InlineBufferOffset = 34;

        private static byte[] BuildInlineUtf8String(string text, int leadingPadding = 0)
        {
            var payload = Encoding.UTF8.GetBytes(text);
            var buffer = new byte[leadingPadding + InlineBufferOffset + payload.Length + 1];

            var at = leadingPadding;
            BitConverter.GetBytes((long)(payload.Length + 1)).CopyTo(buffer, at + BufUsedOffset);
            BitConverter.GetBytes((long)payload.Length).CopyTo(buffer, at + LengthOffset);
            buffer[at + InlineFlagOffset] = 1;
            payload.CopyTo(buffer, at + InlineBufferOffset);

            return buffer;
        }

        [Test]
        public void InlineString_IsRecognised()
        {
            var buffer = BuildInlineUtf8String("Crystal bearer...");

            Assert.That(
                TalkAddonRealtimeReader.TryParseUtf8StringHeader(buffer, 0, out var byteCount, out var isInline,
                    out _),
                Is.True);

            Assert.That(isInline, Is.True);
            Assert.That(byteCount, Is.EqualTo(Encoding.UTF8.GetByteCount("Crystal bearer...") + 1));
        }

        [Test]
        public void StringAtAnyOffset_IsRecognised()
        {
            // The whole point is finding it after it moved.
            var buffer = BuildInlineUtf8String("I am Hydaelyn. All made one.", leadingPadding: 0x120);

            Assert.That(
                TalkAddonRealtimeReader.TryParseUtf8StringHeader(buffer, 0x120, out _, out var isInline, out _),
                Is.True);

            Assert.That(isInline, Is.True);
        }

        [Test]
        public void ZeroLength_IsRejected()
        {
            var buffer = new byte[64];

            Assert.That(TalkAddonRealtimeReader.TryParseUtf8StringHeader(buffer, 0, out _, out _, out _), Is.False);
        }

        [Test]
        public void ImplausibleLength_IsRejected()
        {
            var buffer = new byte[64];
            BitConverter.GetBytes(1L << 40).CopyTo(buffer, BufUsedOffset);

            Assert.That(TalkAddonRealtimeReader.TryParseUtf8StringHeader(buffer, 0, out _, out _, out _), Is.False);
        }

        [Test]
        public void HeapStringWithBogusPointer_IsRejected()
        {
            var buffer = new byte[64];
            BitConverter.GetBytes(32L).CopyTo(buffer, BufUsedOffset);
            BitConverter.GetBytes(31L).CopyTo(buffer, LengthOffset);
            buffer[InlineFlagOffset] = 0;
            BitConverter.GetBytes(0x40L).CopyTo(buffer, PointerOffset);

            Assert.That(TalkAddonRealtimeReader.TryParseUtf8StringHeader(buffer, 0, out _, out _, out _), Is.False);
        }

        [Test]
        public void TruncatedBuffer_IsRejected()
        {
            var buffer = new byte[InlineBufferOffset - 1];

            Assert.That(TalkAddonRealtimeReader.TryParseUtf8StringHeader(buffer, 0, out _, out _, out _), Is.False);
        }

        [TestCase("I am Hydaelyn. All made one.")]
        [TestCase("Crystal bearer, hear me now.")]
        [TestCase("For the sake of all, I beseech thee: deliver us from this fate!")]
        [TestCase("Добро пожаловать в Карлайн Каноли")]
        public void DialogueText_IsAccepted(string text)
        {
            Assert.That(TalkAddonRealtimeReader.LooksLikeDialogueText(text), Is.True);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("ab")]
        [TestCase("")]
        // Everything below was seen reaching the translator as if it were a
        // subtitle, because the first version of this rule only asked for letters.
        [TestCase("cbbp_a_deact")]
        [TestCase("ПЭ 2")]
        [TestCase("atr_parts_a")]
        [TestCase("Talk")]
        [TestCase("Crystal bearer...")]
        [TestCase("0x40 0x00 0x12 0x99")]
        public void NonDialogue_IsRejected(string text)
        {
            Assert.That(TalkAddonRealtimeReader.LooksLikeDialogueText(text), Is.False);
        }

        // Every one of these was read off a running client's window list.
        [TestCase("Talk")]
        [TestCase("TalkSubtitle")]
        [TestCase("_MiniTalk")]
        [TestCase("ChatLog")]
        [TestCase("_ActionBar01")]
        [TestCase("NamePlate")]
        public void AddonName_IsAccepted(string addonName)
        {
            Assert.That(TalkAddonRealtimeReader.LooksLikeAnAddonName(addonName), Is.True);
        }

        // What a stray pointer produces when its bytes are read as a name:
        // nothing, punctuation, the middle of a sentence, a run of high bytes.
        [TestCase("")]
        [TestCase(null)]
        [TestCase("Ta")]
        [TestCase("Talk window")]
        [TestCase("1Talk")]
        [TestCase("Talk-Subtitle")]
        [TestCase("")]
        [TestCase("a name far longer than any addon the game has ever had")]
        public void NonAddonName_IsRejected(string addonName)
        {
            Assert.That(TalkAddonRealtimeReader.LooksLikeAnAddonName(addonName), Is.False);
        }
    }
}
