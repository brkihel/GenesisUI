using System;
using System.Collections.Generic;

namespace GenesisUI.InventoryModel
{
    /// <summary>
    /// "Organizar" (R): packs the ordinary rows below the hotbar in a stable order — category, then
    /// name, then larger stacks first. The hotbar row, quick-use, utility and equipment never move.
    /// Only positions change: no stack is merged or split, so item count and identity stay the same.
    /// </summary>
    public static class InventorySort
    {
        public struct Entry
        {
            public int Id;
            public int X;
            public int Y;
            public ItemCategory Category;
            public string Name;
            public int Stack;
        }

        public static List<Move> Plan(SlotLayout layout, IReadOnlyList<Entry> items)
        {
            var moving = new List<Entry>();
            for (int i = 0; i < items.Count; i++)
                if (items[i].Y >= 1 && layout.IsOrdinary(items[i].X, items[i].Y)) moving.Add(items[i]);
            moving.Sort(Compare);

            var moves = new List<Move>();
            int cell = SlotLayout.Width; // first cell of row 1
            for (int i = 0; i < moving.Count; i++, cell++)
            {
                int x = cell % SlotLayout.Width, y = cell / SlotLayout.Width;
                if (moving[i].X != x || moving[i].Y != y) moves.Add(new Move { Id = moving[i].Id, X = x, Y = y });
            }
            return moves;
        }

        private static int Compare(Entry a, Entry b)
        {
            int c = a.Category.CompareTo(b.Category);
            if (c != 0) return c;
            c = string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase);
            if (c != 0) return c;
            c = b.Stack.CompareTo(a.Stack);
            if (c != 0) return c;
            c = a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X); // stable: current order
            return c != 0 ? c : a.Id.CompareTo(b.Id);
        }
    }
}
