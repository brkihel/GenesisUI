using GenesisUI.Foundation.Logging;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class RateLimiterTests
    {
        [Fact]
        public void First_line_passes_repeats_inside_the_window_are_counted()
        {
            var rl = new RateLimiter(10);

            Assert.True(rl.ShouldEmit("k", 0, out int s0));
            Assert.Equal(0, s0);
            Assert.False(rl.ShouldEmit("k", 1, out _));
            Assert.False(rl.ShouldEmit("k", 9.9, out int s2));
            Assert.Equal(2, s2);
        }

        [Fact]
        public void After_the_window_the_line_passes_with_the_suppressed_count()
        {
            var rl = new RateLimiter(10);
            rl.ShouldEmit("k", 0, out _);
            rl.ShouldEmit("k", 1, out _);
            rl.ShouldEmit("k", 2, out _);

            Assert.True(rl.ShouldEmit("k", 10, out int suppressed));
            Assert.Equal(2, suppressed);
            Assert.False(rl.ShouldEmit("k", 11, out int again));
            Assert.Equal(1, again);
        }

        [Fact]
        public void Keys_are_independent()
        {
            var rl = new RateLimiter(10);
            rl.ShouldEmit("a", 0, out _);

            Assert.True(rl.ShouldEmit("b", 0.5, out _));
        }

        [Fact]
        public void Memory_is_bounded_by_max_keys()
        {
            var rl = new RateLimiter(10, maxKeys: 2);
            rl.ShouldEmit("a", 0, out _);
            rl.ShouldEmit("b", 0, out _);
            rl.ShouldEmit("c", 0, out _); // clears, then stores c

            // "a" was forgotten, so it passes again inside its old window.
            Assert.True(rl.ShouldEmit("a", 1, out _));
        }
    }
}
