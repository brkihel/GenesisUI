using System;
namespace GenesisUI.Foundation.Logging
{
    /// <summary>Bounded samples; recording allocates nothing, sorting happens on report.</summary>
    public sealed class RecentSamples
    {
        private readonly double[] _values;
        private int _next;
        public int Count { get; private set; }
        public RecentSamples(int capacity = 256) { if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity)); _values = new double[capacity]; }
        public void Add(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) return;
            _values[_next] = value; _next = (_next + 1) % _values.Length; Count = Math.Min(Count + 1, _values.Length);
        }
        public double Percentile(double fraction)
        {
            if (Count == 0) return 0;
            var copy = new double[Count]; Array.Copy(_values, copy, Count); Array.Sort(copy);
            return copy[Math.Max(0, Math.Min(Count - 1, (int)Math.Ceiling(fraction * Count) - 1))];
        }
        public void Clear() { _next = 0; Count = 0; }
    }
}
