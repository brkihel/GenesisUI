using System;
using System.Collections.Generic;
using GenesisUI.Foundation.Faults;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class FaultRegistryTests
    {
        [Fact]
        public void First_fault_is_count_one_and_trips_at_default_threshold()
        {
            var reg = new FaultRegistry();
            var r = reg.Report("module:a", "boom", "stack", 1.0);

            Assert.Equal(1, r.Count);
            Assert.True(r.Tripped);
            Assert.True(reg.IsTripped("module:a"));
            Assert.Equal("boom", r.FirstMessage);
        }

        [Fact]
        public void Tripped_event_fires_once_per_owner()
        {
            var reg = new FaultRegistry();
            var tripped = new List<string>();
            reg.Tripped += r => tripped.Add(r.Owner);

            reg.Report("module:a", "x", "", 1);
            reg.Report("module:a", "x", "", 2);
            reg.Report("module:b", "y", "", 3);

            Assert.Equal(new[] { "module:a", "module:b" }, tripped);
        }

        [Fact]
        public void Custom_threshold_delays_the_trip()
        {
            var reg = new FaultRegistry();
            reg.SetThreshold("adapter:x", 3);

            reg.Report("adapter:x", "e", "", 1);
            reg.Report("adapter:x", "e", "", 2);
            Assert.False(reg.IsTripped("adapter:x"));

            var third = reg.Report("adapter:x", "e", "", 3);
            Assert.True(third.Tripped);
            Assert.Equal(3, third.Count);
        }

        [Fact]
        public void First_message_is_kept_and_last_time_advances()
        {
            var reg = new FaultRegistry();
            reg.Report("o", "first", "d1", 1);
            var r = reg.Report("o", "second", "d2", 5);

            Assert.Equal("first", r.FirstMessage);
            Assert.Equal("d1", r.FirstDetail);
            Assert.Equal(1, r.FirstAtSeconds);
            Assert.Equal(5, r.LastAtSeconds);
        }

        [Fact]
        public void Reset_lets_an_owner_run_again()
        {
            var reg = new FaultRegistry();
            reg.Report("o", "e", "", 1);

            Assert.True(reg.Reset("o"));
            Assert.False(reg.IsTripped("o"));
            Assert.False(reg.Reset("o"));
        }

        [Fact]
        public void Snapshot_is_a_copy()
        {
            var reg = new FaultRegistry();
            reg.Report("o", "e", "", 1);
            var snap = reg.Snapshot();

            reg.Report("o", "e", "", 2);

            Assert.Equal(1, snap[0].Count);
            Assert.Equal(2, reg.Snapshot()[0].Count);
        }

        [Fact]
        public void Owner_is_required()
        {
            Assert.Throws<ArgumentException>(() => new FaultRegistry().Report("", "e", "", 1));
        }
    }
}
