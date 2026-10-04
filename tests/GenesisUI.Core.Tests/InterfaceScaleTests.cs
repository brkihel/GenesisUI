using GenesisUI.Layout;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class InterfaceScaleTests
    {
        [Theory]
        [InlineData(1920f, 1080f)]
        [InlineData(2560f, 1080f)]
        [InlineData(1280f, 720f)]
        public void Extended_boards_can_grow_without_crossing_screen_bounds(float width, float height)
        {
            float ordinary = InterfaceScale.Window(width, height, 1580f, 850f, .75f, .75f, 1f, 1f);
            float backpack = InterfaceScale.Window(width, height, 1580f, 1200f, .75f, .75f, 1f, 1f);
            float larger = InterfaceScale.Window(width, height, 1580f, 1200f, .75f, .75f, 1.5f, 2f);
            Assert.True(backpack <= ordinary);
            Assert.True(larger > backpack);
            Assert.True(larger * 1580f <= width + .001f);
            Assert.True(larger * 1200f <= height + .001f);
        }

        [Fact]
        public void Window_and_general_scales_combine_and_invalid_settings_remain_finite()
        {
            float baseline = InterfaceScale.Window(1920f, 1080f, 1580f, 850f, .75f, .75f, 1f, 1f);
            Assert.Equal(baseline * .8f, InterfaceScale.Window(1920f, 1080f, 1580f, 850f, .75f, .75f, 1f, .8f), 5);
            Assert.Equal(baseline * .8f, InterfaceScale.Window(1920f, 1080f, 1580f, 850f, .75f, .75f, .8f, 1f), 5);
            Assert.Equal(baseline, InterfaceScale.Window(1920f, 1080f, 1580f, 850f, .75f, .75f, float.NaN, float.PositiveInfinity));
            Assert.Equal(baseline, InterfaceScale.Window(1920f, 1080f, 1580f, 850f, float.NaN, float.NegativeInfinity, 1f, 1f));
            Assert.Equal(.5f, InterfaceScale.Multiplier(-1f));
        }
    }
}
