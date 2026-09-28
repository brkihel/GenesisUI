using GenesisUI.HudModel;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class HudFormattingTests
    {
        [Theory]
        [InlineData(1800f, 30, 'm', false)]
        [InlineData(61f, 2, 'm', false)]   // vanilla rounds minutes up
        [InlineData(60f, 1, 'm', false)]
        [InlineData(59.9f, 59, 's', true)] // and seconds down
        [InlineData(0.4f, 0, 's', true)]
        [InlineData(-3f, 0, 's', true)]
        [InlineData(float.NaN, 0, 's', true)]
        public void Food_time_matches_vanilla(float seconds, int value, char unit, bool urgent)
        {
            var t = TimeText.Food(seconds);
            Assert.Equal(value, t.Value);
            Assert.Equal(unit, t.Unit);
            Assert.Equal(urgent, t.Urgent);
        }

        [Theory]
        [InlineData(268f, 4, 28)]
        [InlineData(0.2f, 0, 1)]
        [InlineData(0f, 0, 0)]
        [InlineData(3600f, 60, 0)]
        public void Clock_is_m_ss_rounded_up(float seconds, int minutes, int secs)
        {
            var t = TimeText.Clock(seconds);
            Assert.Equal(':', t.Unit);
            Assert.Equal(minutes, t.Minutes);
            Assert.Equal(secs, t.Seconds);
        }

        [Fact]
        public void Clock_is_urgent_only_in_the_last_minute()
        {
            Assert.True(TimeText.Clock(30f).Urgent);
            Assert.False(TimeText.Clock(90f).Urgent);
            Assert.False(TimeText.Clock(0f).Urgent);
        }

        [Theory]
        [InlineData(0, 0f, 0f)]
        [InlineData(1, -84f, 0f)]
        [InlineData(5, -420f, 0f)]
        [InlineData(6, 0f, -100f)]
        [InlineData(7, -84f, -100f)]
        public void Tiles_fill_right_to_left_then_down(int index, float x, float y)
        {
            TileGrid.RightToLeft(index, 6, 84f, 100f, out float gx, out float gy);
            Assert.Equal(x, gx);
            Assert.Equal(y, gy);
        }

        [Fact]
        public void Tile_grid_tolerates_bad_input()
        {
            TileGrid.RightToLeft(-2, 0, 84f, 100f, out float x, out float y);
            Assert.Equal(0f, x);
            Assert.Equal(0f, y);
        }
    }
}
