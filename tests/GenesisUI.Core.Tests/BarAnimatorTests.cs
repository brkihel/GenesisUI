using GenesisUI.Vitals;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class BarAnimatorTests
    {
        [Fact]
        public void Starts_full_without_a_trail()
        {
            var b = new BarAnimator();
            b.Update(100, 100, 0.016f);

            Assert.Equal(1f, b.Fast);
            Assert.Equal(1f, b.Slow);
            Assert.Equal(100, b.Display);
            Assert.True(b.HasCapacity);
        }

        [Fact]
        public void A_loss_leaves_a_trail_that_holds_then_drains()
        {
            var b = new BarAnimator();
            b.Update(100, 100, 0.016f);
            b.Update(60, 100, 0.016f);

            Assert.Equal(0.6f, b.Fast, 3);
            Assert.Equal(1f, b.Slow, 3);

            b.Update(60, 100, BarAnimator.TrailHoldSeconds * 0.9f); // still holding
            Assert.Equal(1f, b.Slow, 3);

            b.Update(60, 100, BarAnimator.TrailHoldSeconds * 0.2f); // hold ends
            b.Update(60, 100, 0.25f);                                 // drains 0.15
            Assert.True(b.Slow < 1f && b.Slow > 0.6f);

            for (int i = 0; i < 200; i++) b.Update(60, 100, 0.05f);
            Assert.Equal(0.6f, b.Slow, 3);
        }

        [Fact]
        public void Healing_never_shows_a_trail()
        {
            var b = new BarAnimator();
            b.Update(50, 100, 0.016f);
            for (int i = 0; i < 200; i++) b.Update(50, 100, 0.05f);

            b.Update(80, 100, 0.016f);
            Assert.Equal(b.Fast, b.Slow, 3);
        }

        [Fact]
        public void A_new_loss_restarts_the_hold()
        {
            var b = new BarAnimator();
            b.Update(100, 100, 0.016f);
            b.Update(80, 100, 0.016f);
            b.Update(80, 100, 0.4f);
            b.Update(70, 100, 0.016f); // second hit before the trail moved

            b.Update(70, 100, 0.3f);
            Assert.Equal(1f, b.Slow, 3);
        }

        [Theory]
        [InlineData(1.2f, 2)]
        [InlineData(1.0f, 1)]
        [InlineData(0.01f, 1)]
        [InlineData(0f, 0)]
        [InlineData(-5f, 0)]
        public void Display_rounds_up_like_vanilla(float value, int expected)
        {
            var b = new BarAnimator();
            b.Update(value, 100, 0.016f);
            Assert.Equal(expected, b.Display);
        }

        [Fact]
        public void Display_changed_is_only_true_when_the_number_changes()
        {
            var b = new BarAnimator();
            b.Update(50, 100, 0.016f);
            Assert.True(b.DisplayChanged);
            b.Update(49.5f, 100, 0.016f);
            Assert.False(b.DisplayChanged);
            b.Update(48.9f, 100, 0.016f);
            Assert.True(b.DisplayChanged);
        }

        [Fact]
        public void No_capacity_means_empty_and_flagged()
        {
            var b = new BarAnimator();
            b.Update(0, 0, 0.016f);

            Assert.False(b.HasCapacity);
            Assert.Equal(0f, b.Fast);
        }

        [Fact]
        public void Garbage_input_is_contained()
        {
            var b = new BarAnimator();
            b.Update(float.NaN, float.PositiveInfinity, float.NaN);

            Assert.Equal(0f, b.Fast);
            Assert.Equal(0, b.Display);
        }

        [Fact]
        public void Overflow_is_clamped()
        {
            var b = new BarAnimator();
            b.Update(150, 100, 0.016f);
            Assert.Equal(1f, b.Fast);
        }
    }
}
