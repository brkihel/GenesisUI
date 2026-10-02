using System;
using System.Collections.Generic;
using GenesisUI.Foundation.Faults;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class RecoveryQueueTests
    {
        [Fact]
        public void Rebuild_runs_after_every_cleanup_subscriber_even_when_one_throws()
        {
            var faults = new FaultRegistry();
            var queue = new RecoveryQueue();
            var events = new List<string>();
            bool veil = false, lease = false;
            faults.Tripped += queue.Request;
            faults.Tripped += _ => { events.Add("broken cleanup"); throw new InvalidOperationException("cleanup fault"); };
            faults.Tripped += _ => { events.Add("veil cleanup"); veil = false; };
            faults.Tripped += _ => { events.Add("lease cleanup"); lease = false; };
            var record = faults.Report("owner", "fault", "detail", 0);
            Assert.Equal(1, record.NotificationFailures);
            queue.Drain(_ => { events.Add("rebuild"); veil = lease = true; });
            Assert.True(veil && lease);
            Assert.Equal(new[]{"broken cleanup", "veil cleanup", "lease cleanup", "rebuild"}, events);
            Assert.Equal(1, faults.Snapshot()[0].NotificationFailures);
        }

        [Fact]
        public void A_failed_rebuild_waits_for_next_drain_and_duplicate_notifications_coalesce()
        {
            var faults = new FaultRegistry();
            var queue = new RecoveryQueue();
            faults.Tripped += queue.Request;
            var first = faults.Report("owner", "fault", "detail", 0);
            queue.Request(first);
            int calls = 0;
            queue.Drain(r => { calls++; faults.Reset(r.Owner); faults.Report(r.Owner, "again", "", 1); });
            Assert.Equal(1, calls);
            Assert.Equal(1, queue.Count);
            queue.Forget("owner");
            queue.Drain(_ => calls++);
            Assert.Equal(1, calls);
        }
    }
}
