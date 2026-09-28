using System;
using System.Collections.Generic;

namespace GenesisUI.Foundation.Logging
{
    /// <summary>
    /// Lets the same log key through at most once per window, and counts what it
    /// held back so the next emitted line can say "(+N suppressed)". Keeps a log
    /// readable when something fails every frame (docs/DIAGNOSTICS.md §2).
    /// </summary>
    public sealed class RateLimiter
    {
        private struct Entry
        {
            public double LastEmit;
            public int Suppressed;
        }

        private readonly object _lock = new object();
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private readonly double _windowSeconds;
        private readonly int _maxKeys;

        public RateLimiter(double windowSeconds, int maxKeys = 2048)
        {
            if (windowSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(windowSeconds));
            if (maxKeys < 1) throw new ArgumentOutOfRangeException(nameof(maxKeys));
            _windowSeconds = windowSeconds;
            _maxKeys = maxKeys;
        }

        /// <param name="suppressed">How many times this key was held back since it last got through.</param>
        public bool ShouldEmit(string key, double nowSeconds, out int suppressed)
        {
            lock (_lock)
            {
                if (!_entries.TryGetValue(key, out var e))
                {
                    // Bounded memory: a flood of distinct keys must not grow forever.
                    if (_entries.Count >= _maxKeys) _entries.Clear();
                    _entries[key] = new Entry { LastEmit = nowSeconds };
                    suppressed = 0;
                    return true;
                }

                if (nowSeconds - e.LastEmit >= _windowSeconds)
                {
                    suppressed = e.Suppressed;
                    _entries[key] = new Entry { LastEmit = nowSeconds };
                    return true;
                }

                e.Suppressed++;
                _entries[key] = e;
                suppressed = e.Suppressed;
                return false;
            }
        }
    }
}
