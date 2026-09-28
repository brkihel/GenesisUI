using GenesisUI.Foundation.Logging;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class RedactorTests
    {
        [Fact]
        public void Known_values_are_replaced_case_insensitively()
        {
            var r = new Redactor();
            r.AddSecret("Eirik", "character");

            Assert.Equal("player <character> joined as <character>", r.Redact("player Eirik joined as EIRIK"));
        }

        [Fact]
        public void Longer_values_win_over_their_prefixes()
        {
            var r = new Redactor();
            r.AddSecret("Eirik", "character");
            r.AddSecret("Eirik Stormborn", "profile");

            Assert.Equal("<profile>", r.Redact("Eirik Stormborn"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ab")]
        [InlineData("  ")]
        public void Short_or_empty_values_are_ignored(string value)
        {
            var r = new Redactor();
            r.AddSecret(value, "x");

            Assert.Equal("ab cd", r.Redact("ab cd"));
        }

        [Fact]
        public void Steam_ids_are_redacted()
        {
            Assert.Equal("id <platform-id>.", new Redactor().Redact("id 76561198012345678."));
        }

        [Theory]
        [InlineData("connect 45.67.89.10", "connect <address>")]
        [InlineData("host 192.168.0.7", "host <address>")]
        [InlineData("peer 1.2.3.4:2456 ok", "peer <address> ok")]
        public void Ip_addresses_are_redacted(string input, string expected)
        {
            Assert.Equal(expected, new Redactor().Redact(input));
        }

        [Theory]
        [InlineData("Game: 1.0.15.0")]
        [InlineData("BepInEx: 5.4.2333.0")]
        [InlineData("GenesisUI 0.1.0.0")]
        [InlineData("Unity: 6000.0.75f1")]
        public void Version_strings_survive(string line)
        {
            Assert.Equal(line, new Redactor().Redact(line));
        }
    }
}
