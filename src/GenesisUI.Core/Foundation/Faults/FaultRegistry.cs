using System;
using System.Collections.Generic;
using System.Linq;

namespace GenesisUI.Foundation.Faults
{
    /// <summary>
    /// One owner's fault history. An owner is anything the guard protects:
    /// "module:hud.vitals", "adapter:jewelcrafting", "patch:HudPatches".
    /// </summary>
    public sealed class FaultRecord
    {
        public string Owner { get; internal set; }
        public string FirstMessage { get; internal set; }
        public string FirstDetail { get; internal set; }
        public int Count { get; internal set; }
        public double FirstAtSeconds { get; internal set; }
        public double LastAtSeconds { get; internal set; }
        public bool Tripped { get; internal set; }
        public int NotificationFailures { get; internal set; }
        public string LastNotificationError { get; internal set; }

        internal FaultRecord Copy() => (FaultRecord)MemberwiseClone();
    }

    /// <summary>
    /// Counts faults per owner and trips an owner once it reaches its threshold.
    /// A tripped owner stays tripped for the session unless explicitly reset
    /// (Debug/Preview "retry"). The default threshold is 1: a UI piece that threw
    /// once steps aside, it does not get to throw every frame.
    /// </summary>
    public sealed class FaultRegistry
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, FaultRecord> _records = new Dictionary<string, FaultRecord>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _thresholds = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly int _defaultThreshold;

        public FaultRegistry(int defaultThreshold = 1)
        {
            if (defaultThreshold < 1) throw new ArgumentOutOfRangeException(nameof(defaultThreshold));
            _defaultThreshold = defaultThreshold;
        }

        /// <summary>Raised once, outside the lock, when an owner trips.</summary>
        public event Action<FaultRecord> Tripped;

        public void SetThreshold(string owner, int threshold)
        {
            if (threshold < 1) throw new ArgumentOutOfRangeException(nameof(threshold));
            lock (_lock) _thresholds[owner] = threshold;
        }

        public bool IsTripped(string owner)
        {
            lock (_lock) return _records.TryGetValue(owner, out var r) && r.Tripped;
        }

        /// <summary>
        /// Records a fault. Returns a copy of the owner's record after this fault;
        /// <c>Count == 1</c> means this is the first one, which is when the caller logs
        /// the full detail.
        /// </summary>
        public FaultRecord Report(string owner, string message, string detail, double nowSeconds)
        {
            if (string.IsNullOrEmpty(owner)) throw new ArgumentException("owner is required", nameof(owner));

            FaultRecord snapshot;
            bool justTripped = false;
            lock (_lock)
            {
                if (!_records.TryGetValue(owner, out var r))
                {
                    r = new FaultRecord { Owner = owner, FirstMessage = message, FirstDetail = detail, FirstAtSeconds = nowSeconds };
                    _records[owner] = r;
                }
                r.Count++;
                r.LastAtSeconds = nowSeconds;

                int threshold = _thresholds.TryGetValue(owner, out var t) ? t : _defaultThreshold;
                if (!r.Tripped && r.Count >= threshold)
                {
                    r.Tripped = true;
                    justTripped = true;
                }
                snapshot = r.Copy();
            }

            if (justTripped && Tripped != null)
            {
                foreach (Action<FaultRecord> subscriber in Tripped.GetInvocationList())
                {
                    try { subscriber(snapshot.Copy()); }
                    catch (Exception e)
                    {
                        snapshot.NotificationFailures++;
                        snapshot.LastNotificationError = e.GetType().Name + ": " + e.Message;
                    }
                }
                lock (_lock)
                {
                    if (_records.TryGetValue(owner, out var stored))
                    {
                        stored.NotificationFailures += snapshot.NotificationFailures;
                        stored.LastNotificationError = snapshot.LastNotificationError;
                    }
                }
            }
            return snapshot;
        }

        /// <summary>Forgets an owner's faults so it may run again. Returns false if it had none.</summary>
        public bool Reset(string owner)
        {
            lock (_lock) return _records.Remove(owner);
        }

        public IReadOnlyList<FaultRecord> Snapshot()
        {
            lock (_lock) return _records.Values.OrderBy(r => r.Owner, StringComparer.Ordinal).Select(r => r.Copy()).ToList();
        }
    }
}
