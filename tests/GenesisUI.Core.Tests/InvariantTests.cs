using System.Globalization;
using System.Threading;
using GenesisUI.Foundation.Formatting;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class InvariantTests
    {
        [Fact]
        public void Numbers_ignore_the_players_culture()
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-BR");

                Assert.Equal("0.117", Invariant.Format(0.117, 3));
                Assert.True(Invariant.TryParse("0.117", out double v));
                Assert.Equal(0.117, v, 6);
                Assert.Equal("1234", Invariant.Format(1234));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Fact]
        public void Decimals_are_clamped()
        {
            Assert.Equal("2", Invariant.Format(1.5, -1));
            Assert.Equal("0.333333", Invariant.Format(1.0 / 3, 20));
        }
    }
}
