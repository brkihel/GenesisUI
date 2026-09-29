using System.Collections.Generic;

namespace GenesisUI.InventoryModel
{
    /// <summary>
    /// Position-only plans for F4.2b. The caller applies the complete plan after vanilla has
    /// equipped or unequipped the item; a refused plan never partly moves an item.
    /// </summary>
    public static class EquipmentMove
    {
        public sealed class Plan
        {
            public bool Ok;
            public string Reason;
            public readonly List<Move> Moves = new List<Move>(2);
        }

        public static Plan Equip(SlotLayout layout, EquipSlot slot, int itemId, IReadOnlyList<ItemAt> items)
        {
            var plan = new Plan();
            var target = layout.EquipmentPosition(slot);
            if (target.X < 0 || !Unique(items)) return Refuse(plan, "slot absent or positions overlap");
            ItemAt? moving = null;
            ItemAt? occupied = null;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.Id == itemId) moving = item;
                else if (item.X == target.X && item.Y == target.Y) occupied = item;
            }
            if (!moving.HasValue) return Refuse(plan, "item missing");
            var source = moving.Value;
            if (source.X == target.X && source.Y == target.Y) { plan.Ok = true; return plan; }
            if (!layout.IsOrdinary(source.X, source.Y)) return Refuse(plan, "source is not an ordinary cell");
            plan.Moves.Add(new Move { Id = itemId, X = target.X, Y = target.Y });
            if (occupied.HasValue)
                plan.Moves.Add(new Move { Id = occupied.Value.Id, X = source.X, Y = source.Y });
            plan.Ok = true;
            return plan;
        }

        public static Plan Unequip(SlotLayout layout, int itemId, IReadOnlyList<ItemAt> items)
        {
            var plan = new Plan();
            if (!Unique(items)) return Refuse(plan, "positions overlap");
            ItemAt? moving = null;
            for (int i = 0; i < items.Count; i++) if (items[i].Id == itemId) moving = items[i];
            if (!moving.HasValue) return Refuse(plan, "item missing");
            if (!layout.EquipmentAt(moving.Value.X, moving.Value.Y).HasValue)
            {
                plan.Ok = true;
                return plan;
            }
            for (int y = 0; y < layout.Rows; y++)
                for (int x = 0; x < SlotLayout.Width; x++)
                {
                    bool used = false;
                    for (int i = 0; i < items.Count; i++)
                        if (items[i].X == x && items[i].Y == y) { used = true; break; }
                    if (used) continue;
                    plan.Moves.Add(new Move { Id = itemId, X = x, Y = y });
                    plan.Ok = true;
                    return plan;
                }
            return Refuse(plan, "ordinary inventory full");
        }

        private static Plan Refuse(Plan plan, string reason)
        {
            plan.Reason = reason;
            return plan;
        }

        private static bool Unique(IReadOnlyList<ItemAt> items)
        {
            var places = new HashSet<(int, int)>();
            var ids = new HashSet<int>();
            for (int i = 0; i < items.Count; i++)
                if (!places.Add((items[i].X, items[i].Y)) || !ids.Add(items[i].Id)) return false;
            return true;
        }
    }
}
