using System.Collections.Generic;
using GenesisUI.Data;
using GenesisUI.Foundation.Logging;
using Xunit;
namespace GenesisUI.Core.Tests
{
    public class MeasurementTests
    {
        [Fact]
        public void Measurements_keep_recent_window_and_reject_invalid_samples()
        {
            var samples = new RecentSamples(4);
            foreach (var value in new[] { 1000d, 1, 2, 3, 4, double.NaN, double.PositiveInfinity, -1 }) samples.Add(value);
            Assert.Equal(4, samples.Count); Assert.Equal(2, samples.Percentile(.5)); Assert.Equal(4, samples.Percentile(.95));
            samples.Clear(); Assert.Equal(0, samples.Count); Assert.Equal(0, samples.Percentile(.95));
        }
        [Fact]
        public void Item_custom_data_invalidation_ignores_order_but_detects_changed_content()
        {
            var first = new Dictionary<string,string> { ["gem"] = "ruby", ["name"] = "hammer" };
            var reordered = new Dictionary<string,string> { ["name"] = "hammer", ["gem"] = "ruby" };
            Assert.Equal(TextMapFingerprint.Of(first), TextMapFingerprint.Of(reordered));
            reordered["gem"] = "sapphire";
            Assert.NotEqual(TextMapFingerprint.Of(first), TextMapFingerprint.Of(reordered));
        }
    }
}
