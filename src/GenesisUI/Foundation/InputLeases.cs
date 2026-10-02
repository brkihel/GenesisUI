using System;
using GenesisUI.Foundation.Input;
using Jotunn.Managers;

namespace GenesisUI.Foundation
{
    /// <summary>
    /// Per-owner input leases over Jötunn's GUIManager.BlockInput (docs/ARCHITECTURE.md §5).
    /// All of GenesisUI holds at most one Jötunn block request; a faulted owner loses
    /// its leases automatically, so a crashed window can never leave the player
    /// unable to move.
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Input.LeaseBook), typeof(GenesisUI.Foundation.Guard), typeof(GenesisUI.Foundation.Faults.FaultRegistry), typeof(GenesisUI.Foundation.Faults.FaultRecord), typeof(GenesisUI.Foundation.GenesisLog))]
    internal static class InputLeases
    {
        private static readonly LeaseBook Book = new LeaseBook();

        static InputLeases()
        {
            Book.FirstAcquired += () => GUIManager.BlockInput(true);
            Book.LastReleased += () => GUIManager.BlockInput(false);
            Guard.Faults.Tripped += record =>
            {
                int released = Book.ReleaseAll(record.Owner);
                if (released > 0) GenesisLog.Warn("Input", record.Owner + " faulted; released its " + released + " input lease(s)");
            };
        }

        public static int ActiveCount => Book.ActiveCount;

        public static IDisposable Acquire(string owner) => new Lease(Book.Acquire(owner));

        public static int ReleaseAll(string owner) => Book.ReleaseAll(owner);
        internal static void Shutdown() { foreach (var lease in Book.Snapshot()) Guard.Try("release input lease", () => Book.Release(lease.Key)); }

        private sealed class Lease : IDisposable
        {
            private readonly long _id;

            public Lease(long id) => _id = id;

            public void Dispose() => Book.Release(_id);
        }
    }
}
