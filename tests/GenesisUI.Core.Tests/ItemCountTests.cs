using GenesisUI.Text;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class ItemCountTests
    {
        [Theory]
        [InlineData(0, "")]
        [InlineData(1, "1")]
        [InlineData(9999, "9999")]
        [InlineData(10000, "10k")]
        [InlineData(10500, "10.5k")]
        [InlineData(1000000, "1M")]
        [InlineData(1000000000, "1G")]
        [InlineData(int.MaxValue, "2.1G")]
        public void Native_counts_fit_compact_cells(int count, string expected) => Assert.Equal(expected, ItemCount.Compact(count));
    }
}
