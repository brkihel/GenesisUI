using System;
using System.Collections.Generic;

namespace GenesisUI.InventoryModel
{
    /// <summary>Position plans bind to original references, even when callbacks reorder the live list.</summary>
    public sealed class PositionSnapshot<T> where T : class
    {
        private readonly T[] _items;
        private readonly int[] _counts;
        private readonly ItemAt[] _positions;
        private readonly Func<T, int> _count;
        private readonly Func<T, (int X, int Y)> _position;
        private readonly bool[] _written;
        private readonly (int X, int Y)[] _lastWrite;
        public IReadOnlyList<ItemAt> Positions => _positions;
        public IReadOnlyList<T> Items => _items;

        public PositionSnapshot(IReadOnlyList<T> items, Func<T, int> count, Func<T, (int X, int Y)> position)
        {
            if (items == null || count == null || position == null) throw new ArgumentException("Snapshot inputs are required");
            _count = count;
            _position = position;
            _items = new T[items.Count]; _counts = new int[items.Count]; _positions = new ItemAt[items.Count];
            _written = new bool[items.Count]; _lastWrite = new (int X, int Y)[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) throw new ArgumentException("Null item");
                for (int j = 0; j < i; j++) if (ReferenceEquals(items[j], items[i])) throw new ArgumentException("Duplicate item reference");
                var p = position(items[i]);
                _items[i] = items[i]; _counts[i] = count(items[i]); _positions[i] = new ItemAt(i, p.X, p.Y);
            }
        }

        private static bool Contains(IReadOnlyList<T> live, T item)
        {
            for (int i = 0; i < live.Count; i++) if (ReferenceEquals(live[i], item)) return true;
            return false;
        }

        public bool Matches(IReadOnlyList<T> live, bool checkPositions = false)
        {
            if (live == null || live.Count != _items.Length) return false;
            for (int i = 0; i < _items.Length; i++)
            {
                if (!Contains(live, _items[i]) || _count(_items[i]) != _counts[i]) return false;
                var position = _position(_items[i]);
                if (checkPositions && (position.X != _positions[i].X || position.Y != _positions[i].Y)) return false;
            }
            return true;
        }

        public void Apply(IReadOnlyList<T> live, IReadOnlyList<Move> moves, int width, int height, Action<T, int, int> write)
        {
            if (!Matches(live, true)) throw new InvalidOperationException("Inventory identity/count/position changed during transition");
            var final = new (int X, int Y)[_items.Length];
            for (int i = 0; i < final.Length; i++) final[i] = (_positions[i].X, _positions[i].Y);
            var moved = new HashSet<int>();
            foreach (var move in moves)
            {
                if (move.Id < 0 || move.Id >= final.Length || !moved.Add(move.Id)) throw new ArgumentException("Invalid or duplicate move id");
                final[move.Id] = (move.X, move.Y);
            }
            var occupied = new HashSet<(int, int)>();
            foreach (var p in final)
                if (p.X < 0 || p.X >= width || p.Y < 0 || p.Y >= height || !occupied.Add(p))
                    throw new InvalidOperationException("Plan has an overlapping or out-of-bounds position");
            // Validate the entire plan before applying its first write.
            foreach (var move in moves)
            {
                RecordWrite(_items[move.Id], move.X, move.Y);
                write(_items[move.Id], move.X, move.Y);
            }
        }

        public void RecordWrite(T item, int x, int y)
        {
            for (int i = 0; i < _items.Length; i++) if (ReferenceEquals(_items[i], item)) { _written[i] = true; _lastWrite[i] = (x, y); return; }
        }

        public void RestoreRemaining(IReadOnlyList<T> live, Action<T, int, int> write)
        {
            var restore = new bool[_items.Length];
            for (int i = 0; i < _items.Length; i++)
                restore[i] = _written[i] && Contains(live, _items[i]) && _position(_items[i]) == _lastWrite[i];
            // A later foreign move/add may occupy an original cell. Never create an overlap while
            // undoing our journal, including chains whose next restore was blocked by that occupant.
            bool changed;
            do
            {
                changed = false;
                for (int i = 0; i < _items.Length; i++)
                {
                    if (!restore[i]) continue;
                    foreach (var occupant in live)
                    {
                        if (ReferenceEquals(occupant, _items[i])) continue;
                        var p = _position(occupant);
                        if (p.X != _positions[i].X || p.Y != _positions[i].Y) continue;
                        int other = -1;
                        for (int j = 0; j < _items.Length; j++) if (ReferenceEquals(_items[j], occupant)) { other = j; break; }
                        if (other >= 0 && restore[other]) continue;
                        restore[i] = false; changed = true; break;
                    }
                }
            } while (changed);
            for (int i = 0; i < _items.Length; i++)
                if (restore[i]) { write(_items[i], _positions[i].X, _positions[i].Y); _written[i] = false; }
        }
    }
}
