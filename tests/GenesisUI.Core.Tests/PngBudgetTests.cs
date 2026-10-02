using GenesisUI.Data;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class PngBudgetTests
    {
        private static byte[] Header(int width, int height)
        {
            var bytes = new byte[33];
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(bytes, 0);
            bytes[11] = 13; bytes[12] = (byte)'I'; bytes[13] = (byte)'H'; bytes[14] = (byte)'D'; bytes[15] = (byte)'R';
            for (int i = 0; i < 4; i++) { bytes[16 + i] = (byte)(width >> (24 - i * 8)); bytes[20 + i] = (byte)(height >> (24 - i * 8)); }
            return bytes;
        }
        [Fact]
        public void Rejects_compressed_bomb_dimensions_and_manifest_mismatches_before_reserving()
        {
            var budget = new PngBudget();
            Assert.False(budget.Reserve(Header(100000, 100000), 100000, 100000));
            Assert.False(budget.Reserve(Header(64, 64), 32, 32));
            Assert.False(budget.Reserve(new byte[33], 32, 32));
            Assert.Equal(0, budget.ReservedBytes);
            Assert.True(budget.Reserve(Header(64, 64), 64, 64));
        }
        [Fact]
        public void Enforces_aggregate_decoded_budget()
        {
            var budget = new PngBudget();
            Assert.True(budget.Reserve(Header(4096, 4096), 4096, 4096));
            Assert.False(budget.Reserve(Header(1, 1), 1, 1));
            Assert.Equal(PngBudget.MaxDecodedBytes, budget.ReservedBytes);
        }
    }
}
