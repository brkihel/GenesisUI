using System;
using System.Collections.Generic;
using System.Linq;

namespace GenesisUI.Foundation.Input
{
    /// <summary>
    /// Bookkeeping for input leases (docs/ARCHITECTURE.md §5). Each owner holds its
    /// own leases; the book only reports the 0 → 1 and 1 → 0 transitions, so the
    /// plugin makes exactly one BlockInput(true) and one BlockInput(false) call for
    /// all of GenesisUI. That shields us from Jötunn's reset, which zeroes every
    /// caller's requests at once.
    /// </summary>
    public sealed class LeaseBook
    {
        private readonly object _lock = new object();
        private readonly Dictionary<long, string> _active = new Dictionary<long, string>();
        private long _next;

        /// <summary>Raised when the first lease opens (input must be blocked).</summary>
        public event Action FirstAcquired;

        /// <summary>Raised when the last lease closes (input must be released).</summary>
        public event Action LastReleased;

        public int ActiveCount
        {
            get { lock (_lock) return _active.Count; }
        }

        public long Acquire(string owner)
        {
            if (string.IsNullOrEmpty(owner)) throw new ArgumentException("owner is required", nameof(owner));
            long id;
            bool first;
            lock (_lock)
            {
                id = ++_next;
                first = _active.Count == 0;
                _active[id] = owner;
            }
            if (first) FirstAcquired?.Invoke();
            return id;
        }

        /// <summary>Idempotent: releasing an unknown or already released lease returns false and changes nothing.</summary>
        public bool Release(long id)
        {
            bool last;
            lock (_lock)
            {
                if (!_active.Remove(id)) return false;
                last = _active.Count == 0;
            }
            if (last) LastReleased?.Invoke();
            return true;
        }

        /// <summary>Releases every lease of an owner (teardown or fault). Returns how many were open.</summary>
        public int ReleaseAll(string owner)
        {
            int released;
            bool last;
            lock (_lock)
            {
                var ids = _active.Where(kv => kv.Value == owner).Select(kv => kv.Key).ToList();
                foreach (var id in ids) _active.Remove(id);
                released = ids.Count;
                last = released > 0 && _active.Count == 0;
            }
            if (last) LastReleased?.Invoke();
            return released;
        }

        public IReadOnlyList<KeyValuePair<long, string>> Snapshot()
        {
            lock (_lock) return _active.OrderBy(kv => kv.Key).ToList();
        }
    }
}
