using System.Collections.Generic;
using GenesisUI.InventoryModel;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class EquipmentMoveTests
    {
        private static SlotLayout Layout() => new SlotLayout(4, 4, 4, new[]
            { EquipSlot.Head, EquipSlot.Chest, EquipSlot.Legs, EquipSlot.Cape, EquipSlot.Belt, EquipSlot.Trinket });

        [Fact]
        public void Equipping_swaps_a_previous_worn_item_even_when_the_inventory_is_full()
        {
            var layout = Layout();
            var head = layout.EquipmentPosition(EquipSlot.Head);
            var items = new List<ItemAt>();
            for (int i = 0; i < layout.OrdinarySlots; i++) items.Add(new ItemAt(i, i % 8, i / 8));
            items.Add(new ItemAt(32, head.X, head.Y));
            var plan = EquipmentMove.Equip(layout, EquipSlot.Head, 5, items);
            Assert.True(plan.Ok);
            Assert.Collection(plan.Moves,
                move => Assert.Equal((5, head.X, head.Y), (move.Id, move.X, move.Y)),
                move => Assert.Equal((32, 5, 0), (move.Id, move.X, move.Y)));
        }

        [Fact]
        public void Unequipping_uses_an_ordinary_empty_cell_and_never_the_special_row()
        {
            var layout = Layout();
            var head = layout.EquipmentPosition(EquipSlot.Head);
            var items = new[] { new ItemAt(0, head.X, head.Y), new ItemAt(1, 0, 0) };
            var plan = EquipmentMove.Unequip(layout, 0, items);
            Assert.True(plan.Ok);
            Assert.Single(plan.Moves);
            Assert.Equal((1, 0), (plan.Moves[0].X, plan.Moves[0].Y));
        }

        [Fact]
        public void A_full_inventory_or_overlapping_positions_never_produces_a_partial_move()
        {
            var layout = Layout();
            var head = layout.EquipmentPosition(EquipSlot.Head);
            var items = new List<ItemAt>();
            for (int i = 0; i < layout.OrdinarySlots; i++) items.Add(new ItemAt(i, i % 8, i / 8));
            items.Add(new ItemAt(32, head.X, head.Y));
            var full = EquipmentMove.Unequip(layout, 32, items);
            Assert.False(full.Ok);
            Assert.Empty(full.Moves);
            items.Add(new ItemAt(33, head.X, head.Y));
            var overlap = EquipmentMove.Equip(layout, EquipSlot.Head, 0, items);
            Assert.False(overlap.Ok);
            Assert.Empty(overlap.Moves);
        }
    }
}
