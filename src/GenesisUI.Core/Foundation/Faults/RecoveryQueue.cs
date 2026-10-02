using System;
using System.Collections.Generic;

namespace GenesisUI.Foundation.Faults
{
    /// <summary>Coalesces faults until notification/cleanup dispatch has finished.</summary>
    public sealed class RecoveryQueue
    {
        private readonly Queue<FaultRecord> _order = new Queue<FaultRecord>();
        private readonly Dictionary<string, FaultRecord> _pending = new Dictionary<string, FaultRecord>(StringComparer.Ordinal);

        public int Count => _pending.Count;

        public void Request(FaultRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (_pending.ContainsKey(record.Owner)) return;
            _pending.Add(record.Owner, record);
            _order.Enqueue(record);
        }

        public void Forget(string owner) => _pending.Remove(owner);

        /// <summary>New faults from a rebuild wait until the next drain; never recurse into recovery.</summary>
        public void Drain(Action<FaultRecord> recover)
        {
            if (recover == null) throw new ArgumentNullException(nameof(recover));
            int count = _order.Count;
            for (int i = 0; i < count; i++)
            {
                var record = _order.Dequeue();
                if (!_pending.TryGetValue(record.Owner, out var pending) || !ReferenceEquals(record, pending)) continue;
                _pending.Remove(record.Owner);
                recover(record);
            }
        }

        public void Clear() { _pending.Clear(); _order.Clear(); }
    }
}
