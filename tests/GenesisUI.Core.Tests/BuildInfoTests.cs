using System;
using GenesisUI.Foundation.Versioning;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class BuildInfoTests
    {
        [Theory]
        [InlineData(BuildChannel.Dev, 1, "abc1234", "0.1.0-dev+abc1234")]
        [InlineData(BuildChannel.Preview, 3, "abc1234", "0.1.0-preview.3+abc1234")]
        [InlineData(BuildChannel.Release, 1, "abc1234", "0.1.0")]
        [InlineData(BuildChannel.Dev, 1, "unknown", "0.1.0-dev+unknown")]
        [InlineData(BuildChannel.Dev, 1, "NOT A SHA", "0.1.0-dev+unknown")]
        [InlineData(BuildChannel.Dev, 1, null, "0.1.0-dev+unknown")]
        public void Formats_each_channel(BuildChannel channel, int preview, string sha, string expected)
        {
            Assert.Equal(expected, BuildInfo.Format("0.1.0", channel, preview, sha));
        }

        [Theory]
        [InlineData("0.1")]
        [InlineData("0.1.0.0")]
        [InlineData("v0.1.0")]
        [InlineData(null)]
        public void Rejects_non_semver_versions(string version)
        {
            Assert.Throws<ArgumentException>(() => BuildInfo.Format(version, BuildChannel.Release, 1, "abc1234"));
        }

        [Fact]
        public void Preview_number_starts_at_one()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildInfo.Format("0.1.0", BuildChannel.Preview, 0, "abc1234"));
        }
    }
}
