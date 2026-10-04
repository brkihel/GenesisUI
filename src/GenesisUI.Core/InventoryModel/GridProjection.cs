using System;

namespace GenesisUI.InventoryModel
{
    /// <summary>Wrap a native grid into a compact view without changing native cell addresses.</summary>
    public readonly struct GridProjection
    {
        public int Width { get; }
        public int Count { get; }
        public int Columns { get; }
        public int Rows => Columns > 0 ? (Count + Columns - 1) / Columns : 0;
        public GridProjection(int width, int height, int maximumColumns)
        {
            if (width <= 0 || height <= 0 || maximumColumns <= 0 || (long)width * height > 512)
                throw new ArgumentOutOfRangeException(nameof(width), "Native grid exceeds the projection budget");
            Width = width; Count = width * height; Columns = Math.Min(maximumColumns, width);
        }
        public (int X, int Y) Native(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            return (index % Width, index / Width);
        }
    }
}
