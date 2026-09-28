using System;
using GenesisUI.Motion;
using GenesisUI.Theme;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class ThemeTests
    {
        [Fact]
        public void Hex_colours_parse_with_and_without_alpha()
        {
            var c = ColorRgba.FromHex("#FACF72");
            Assert.Equal(0xFA / 255f, c.R, 4);
            Assert.Equal(1f, c.A);

            var t = ColorRgba.FromHex("#15181680");
            Assert.Equal(0x80 / 255f, t.A, 4);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("FACF72")]
        [InlineData("#FACF7")]
        [InlineData("#GGGGGG")]
        [InlineData("#FACF7200FF")]
        public void Bad_hex_is_rejected(string hex)
        {
            Assert.False(ColorRgba.TryFromHex(hex, out _));
            Assert.Throws<FormatException>(() => ColorRgba.FromHex(hex));
        }

        [Fact]
        public void Every_default_token_is_a_valid_colour()
        {
            // Instantiating runs every FromHex in the field initializers.
            var tokens = ThemeTokens.Default();
            Assert.True(tokens.PanelBackground.A > 0.5f);
            Assert.InRange(tokens.BarTrailAlpha, 0f, 1f);
        }

        [Fact]
        public void Lerp_is_clamped()
        {
            var a = ColorRgba.FromHex("#000000");
            var b = ColorRgba.FromHex("#FFFFFF");
            Assert.Equal(b, ColorRgba.Lerp(a, b, 2f));
            Assert.Equal(a, ColorRgba.Lerp(a, b, -1f));
        }

        [Fact]
        public void Pulse_stays_in_range_and_starts_at_zero()
        {
            Assert.Equal(0f, Pulse.Evaluate(0), 4);
            for (double t = 0; t < 10; t += 0.037)
                Assert.InRange(Pulse.Evaluate(t), 0f, 1f);
            Assert.Equal(1f, Pulse.Evaluate(0.7, 1.4f), 3);
        }

        [Fact]
        public void Pulse_period_is_capped_at_one_and_a_half_seconds()
        {
            // A 10 s request behaves like 1.5 s: at 0.75 s it is at its peak.
            Assert.Equal(1f, Pulse.Evaluate(0.75, 10f), 3);
        }
    }
}
