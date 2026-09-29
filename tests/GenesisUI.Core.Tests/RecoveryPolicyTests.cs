using GenesisUI.Foundation.Faults;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class RecoveryPolicyTests
    {
        [Fact]
        public void A_fault_restarts_the_module_up_to_three_times_in_five_minutes()
        {
            var p = new RecoveryPolicy();
            Assert.True(p.TryRestart("m", 0));
            Assert.True(p.TryRestart("m", 10));
            Assert.True(p.TryRestart("m", 20));
            Assert.False(p.TryRestart("m", 30));
            Assert.Equal(0, p.Left("m", 30));
        }

        [Fact]
        public void Old_restarts_expire_and_owners_are_independent()
        {
            var p = new RecoveryPolicy();
            for (int i = 0; i < 3; i++) p.TryRestart("a", i);
            Assert.True(p.TryRestart("b", 3));
            Assert.True(p.TryRestart("a", 3 + RecoveryPolicy.WindowSeconds));
        }
    }
}
