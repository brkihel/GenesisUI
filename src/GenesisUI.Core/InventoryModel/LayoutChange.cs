using System.Collections.Generic;

namespace GenesisUI.InventoryModel
{
    /// <summary>An item's place, by an id the caller chooses (its index in the inventory list).</summary>
    public struct ItemAt
    {
        public int Id;
        public int X;
        public int Y;

        public ItemAt(int id, int x, int y)
        {
            Id = id;
            X = x;
            Y = y;
        }
    }

    public struct Move
    {
        public int Id;
        public int X;
        public int Y;
    }

    /// <summary>
    /// Plans how items move when the layout changes (the admin changes rows, quick/utility counts,
    /// or switches an equipment slot off) — or when a character is loaded under a new layout.
    /// Every item keeps its logical place when the new layout still has it (quick 2 stays quick 2,
    /// the cape stays in the cape slot); ordinary items stay where they are; anything whose place
    /// is gone goes to a free ordinary slot, top-left first. If that does not fit, nothing moves
    /// and the plan fails: the caller keeps the old layout. Two items never end on one position
    /// (vanilla would silently lose the second at the next load).
    /// </summary>
    public static class LayoutChange
    {
        public sealed class Plan
        {
            public bool Ok;
            public List<Move> Moves = new List<Move>();
            /// <summary>Items that had to leave their place for an ordinary slot.</summary>
            public int Displaced;
            /// <summary>When not Ok: how many items did not fit.</summary>
            public int Overflow;
            public string Reason;
        }

        public static Plan Compute(SlotLayout from, SlotLayout to, IReadOnlyList<ItemAt> items)
        {
            var plan = new Plan();
            if (from == null || to == null || items == null) { plan.Reason = "missing snapshot or layout"; return plan; }
            var ids = new HashSet<int>();
            foreach (var item in items)
                if (item.Id < 0 || !ids.Add(item.Id)) { plan.Reason = "invalid or duplicate item id"; return plan; }
            var taken = new HashSet<(int, int)>();
            var final = new Dictionary<int, (int X, int Y)>();
            var homeless = new List<ItemAt>();

            // 1. Items that keep a place: ordinary items still inside the ordinary rows, and special
            //    items whose logical slot exists in the new layout.
            foreach (var it in items)
            {
                var target = Target(from, to, it);
                if (target.HasValue && taken.Add(target.Value)) final[it.Id] = target.Value;
                else homeless.Add(it);
            }

            // 2. Everyone else goes to a free ordinary slot, in reading order.
            foreach (var it in homeless)
            {
                var slot = FreeOrdinary(to, taken);
                if (!slot.HasValue) { plan.Overflow++; continue; }
                taken.Add(slot.Value);
                final[it.Id] = slot.Value;
                plan.Displaced++;
            }
            plan.Ok = plan.Overflow == 0;
            if (!plan.Ok) return plan;

            foreach (var it in items)
            {
                var p = final[it.Id];
                if (p.X != it.X || p.Y != it.Y) plan.Moves.Add(new Move { Id = it.Id, X = p.X, Y = p.Y });
            }
            return plan;
        }

        private static (int, int)? Target(SlotLayout from, SlotLayout to, ItemAt it)
        {
            var kind = from.KindAt(it.X, it.Y);
            switch (kind)
            {
                case SlotKind.Ordinary:
                    return to.IsOrdinary(it.X, it.Y) ? (it.X, it.Y) : ((int, int)?)null;
                case SlotKind.Quick:
                {
                    int i = it.X;
                    return i < to.Quick ? to.QuickPosition(i) : ((int, int)?)null;
                }
                case SlotKind.Utility:
                {
                    int i = it.X - SlotLayout.MaxQuick;
                    return i < to.Utility ? to.UtilityPosition(i) : ((int, int)?)null;
                }
                case SlotKind.Equipment:
                {
                    var slot = from.EquipmentAt(it.X, it.Y);
                    if (!slot.HasValue) return null;
                    var p = to.EquipmentPosition(slot.Value);
                    return p.X < 0 ? ((int, int)?)null : p;
                }
                default:
                    return null; // not a slot in the old layout: find it a home
            }
        }

        private static (int, int)? FreeOrdinary(SlotLayout layout, HashSet<(int, int)> taken)
        {
            for (int y = 0; y < layout.Rows; y++)
                for (int x = 0; x < SlotLayout.Width; x++)
                    if (!taken.Contains((x, y))) return (x, y);
            return null;
        }
    }
}
