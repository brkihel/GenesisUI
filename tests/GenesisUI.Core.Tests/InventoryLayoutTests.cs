using System.Collections.Generic;
using System.Linq;
using GenesisUI.InventoryModel;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class InventoryLayoutTests
    {
        private static readonly EquipSlot[] Vanilla = { EquipSlot.Head, EquipSlot.Chest, EquipSlot.Legs, EquipSlot.Cape, EquipSlot.Belt, EquipSlot.Trinket };
        private static readonly EquipSlot[] All = (EquipSlot[])System.Enum.GetValues(typeof(EquipSlot));

        [Fact]
        public void Rows_quick_utility_and_equipment_sit_after_the_ordinary_rows()
        {
            var l = new SlotLayout(4, 4, 4, Vanilla);
            Assert.Equal(6, l.TotalRows);
            Assert.Equal(SlotKind.Ordinary, l.KindAt(7, 3));
            Assert.Equal(SlotKind.Quick, l.KindAt(0, 4));
            Assert.Equal(SlotKind.Utility, l.KindAt(4, 4));
            Assert.Equal((3, 5), l.EquipmentPosition(EquipSlot.Cape));
            Assert.Equal(EquipSlot.Trinket, l.EquipmentAt(5, 5));
            Assert.Null(l.KindAt(6, 5));
        }

        [Fact]
        public void The_largest_layout_fits_the_games_nine_rows()
        {
            var l = new SlotLayout(6, 4, 4, All);
            Assert.Equal(9, l.TotalRows);
        }

        [Fact]
        public void Switched_off_quick_and_utility_slots_are_not_slots()
        {
            var l = new SlotLayout(5, 2, 0, Vanilla);
            Assert.Equal(SlotKind.Quick, l.KindAt(1, 5));
            Assert.Null(l.KindAt(2, 5));
            Assert.Null(l.KindAt(4, 5));
        }

        [Fact]
        public void Settings_are_clamped()
        {
            var l = new SlotLayout(12, 9, -3, null);
            Assert.Equal(6, l.Rows);
            Assert.Equal(4, l.Quick);
            Assert.Equal(0, l.Utility);
        }

        [Fact]
        public void Growing_moves_special_items_down_and_keeps_every_item()
        {
            var from = new SlotLayout(4, 4, 4, Vanilla);
            var to = new SlotLayout(6, 4, 4, Vanilla);
            var items = new List<ItemAt> { new ItemAt(0, 2, 1), new ItemAt(1, 1, 4), new ItemAt(2, 5, 4), new ItemAt(3, 3, 5) };
            var plan = LayoutChange.Compute(from, to, items);
            Assert.True(plan.Ok);
            Assert.Equal(0, plan.Displaced);
            var final = Apply(items, plan);
            Assert.Equal((2, 1), final[0]);
            Assert.Equal(to.QuickPosition(1), final[1]);
            Assert.Equal(to.UtilityPosition(1), final[2]);
            Assert.Equal(to.EquipmentPosition(EquipSlot.Cape), final[3]);
            AssertNoSharedPositions(final);
        }

        [Fact]
        public void Shrinking_rehouses_the_lost_rows_and_never_shares_a_position()
        {
            var from = new SlotLayout(6, 4, 4, Vanilla);
            var to = new SlotLayout(4, 4, 4, Vanilla);
            var items = new List<ItemAt>
            {
                new ItemAt(0, 0, 0), new ItemAt(1, 0, 4), new ItemAt(2, 7, 5),   // rows 4-5 vanish
                new ItemAt(3, 0, 6),                                              // quick 0: 6 -> 4
                new ItemAt(4, 0, 7),                                              // head: 7 -> 5
            };
            var plan = LayoutChange.Compute(from, to, items);
            Assert.True(plan.Ok);
            Assert.Equal(2, plan.Displaced);
            var final = Apply(items, plan);
            Assert.Equal(to.QuickPosition(0), final[3]);
            Assert.Equal(to.EquipmentPosition(EquipSlot.Head), final[4]);
            Assert.True(to.IsOrdinary(final[1].X, final[1].Y));
            Assert.True(to.IsOrdinary(final[2].X, final[2].Y));
            AssertNoSharedPositions(final);
        }

        [Fact]
        public void Shrinking_a_full_inventory_fails_and_moves_nothing()
        {
            var from = new SlotLayout(6, 0, 0, null);
            var to = new SlotLayout(4, 0, 0, null);
            var items = Enumerable.Range(0, 48).Select(i => new ItemAt(i, i % 8, i / 8)).ToList();
            var plan = LayoutChange.Compute(from, to, items);
            Assert.False(plan.Ok);
            Assert.Equal(16, plan.Overflow);
            Assert.Empty(plan.Moves);
        }

        [Fact]
        public void A_switched_off_slot_sends_its_item_to_the_inventory()
        {
            var from = new SlotLayout(4, 4, 4, All);
            var to = new SlotLayout(4, 4, 2, Vanilla);
            var items = new List<ItemAt> { new ItemAt(0, 7, 4), new ItemAt(1, from.EquipmentPosition(EquipSlot.Lantern).X, from.EquipmentPosition(EquipSlot.Lantern).Y) };
            var plan = LayoutChange.Compute(from, to, items);
            Assert.True(plan.Ok);
            Assert.Equal(2, plan.Displaced);
            var final = Apply(items, plan);
            Assert.True(to.IsOrdinary(final[0].X, final[0].Y));
            Assert.True(to.IsOrdinary(final[1].X, final[1].Y));
        }

        [Fact]
        public void Items_outside_any_slot_are_rehoused()
        {
            var layout = new SlotLayout(4, 1, 0, Vanilla);
            var items = new List<ItemAt> { new ItemAt(0, 3, 4), new ItemAt(1, 0, 8) };
            var plan = LayoutChange.Compute(layout, layout, items);
            Assert.True(plan.Ok);
            Assert.Equal(2, plan.Displaced);
        }

        private static Dictionary<int, (int X, int Y)> Apply(List<ItemAt> items, LayoutChange.Plan plan)
        {
            var d = items.ToDictionary(i => i.Id, i => (i.X, i.Y));
            foreach (var m in plan.Moves) d[m.Id] = (m.X, m.Y);
            return d;
        }

        private static void AssertNoSharedPositions(Dictionary<int, (int X, int Y)> final) =>
            Assert.Equal(final.Count, final.Values.Distinct().Count());
    }
}
