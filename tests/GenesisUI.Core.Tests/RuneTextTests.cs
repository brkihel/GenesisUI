using System;
using GenesisUI.Text;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class RuneTextTests
    {
        [Fact]
        public void Runes_reveal_in_order_and_restore_accents_tags_and_line_breaks_exactly()
        {
            const string text = "<b>Áb</b>\nÇd! <color=#FFD080>fé</color>";
            var reveal = new RuneText(text);
            Assert.True(reveal.Write(0));
            Assert.Equal("<b>ᚨᛒ</b>\nᚲᛞ! <color=#FFD080>ᚠᛖ</color>", new string(reveal.Buffer));
            Assert.True(reveal.Write(0.5f));
            Assert.Equal("<b>Áb</b>\nÇᛞ! <color=#FFD080>ᚠᛖ</color>", new string(reveal.Buffer));
            Assert.True(reveal.Write(1));
            Assert.Equal(text, new string(reveal.Buffer));
            Assert.False(reveal.Write(1));
        }

        [Fact]
        public void Repeated_frames_reuse_the_same_buffer_and_skip_unchanged_letter_counts()
        {
            var reveal = new RuneText("ABC");
            var buffer = reveal.Buffer;
            Assert.True(reveal.Write(0));
            Assert.False(reveal.Write(0.2f));
            Assert.True(reveal.Write(0.5f));
            Assert.Same(buffer, reveal.Buffer);
        }

        [Fact]
        public void Native_runes_symbols_and_empty_text_remain_readable()
        {
            foreach (string text in new[] { "", "ᚨᚹ ◆ 123\n!", "<sprite=1>" })
            {
                var reveal = new RuneText(text);
                Assert.True(reveal.Write(float.NaN));
                Assert.Equal(text, new string(reveal.Buffer));
                Assert.False(reveal.Write(1));
            }
        }

        [Fact]
        public void Duration_and_native_text_size_are_bounded()
        {
            Assert.Equal(2.5f, new RuneText("AB").Duration);
            var longText = new RuneText(new string('A', RuneText.Limit * 2));
            Assert.Equal(8f, longText.Duration);
            Assert.Equal(RuneText.Limit, longText.Buffer.Length);
            longText.Write(float.PositiveInfinity);
            Assert.Equal('…', longText.Buffer[longText.Buffer.Length - 1]);
        }
    }
}
