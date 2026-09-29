using System.Collections.Generic;
using System.Linq;
using GenesisUI.InventoryModel;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class SlotRulesTests
    {
        [Fact]
        public void Quick_use_takes_only_consumables()
        {
            Assert.True(SlotRules.Accepts(SlotKind.Quick, ItemCategory.Consumable));
            Assert.False(SlotRules.Accepts(SlotKind.Quick, ItemCategory.Weapon));
            Assert.False(SlotRules.Accepts(SlotKind.Quick, ItemCategory.Material));
        }

        [Fact]
        public void Utility_takes_gear_but_never_armour()
        {
            foreach (var c in new[] { ItemCategory.Weapon, ItemCategory.Shield, ItemCategory.Tool, ItemCategory.Ammo })
                Assert.True(SlotRules.Accepts(SlotKind.Utility, c));
            Assert.False(SlotRules.Accepts(SlotKind.Utility, ItemCategory.Armor));
            Assert.False(SlotRules.Accepts(SlotKind.Utility, ItemCategory.Consumable));
        }

        [Fact]
        public void A_swap_must_fit_the_source_slot_too()
        {
            // A sword from utility onto a cell holding food would push the food into utility.
            Assert.False(SlotRules.AllowsMove(SlotKind.Utility, SlotKind.Ordinary, ItemCategory.Weapon, ItemCategory.Consumable));
            Assert.True(SlotRules.AllowsMove(SlotKind.Utility, SlotKind.Ordinary, ItemCategory.Weapon, ItemCategory.Tool));
            Assert.True(SlotRules.AllowsMove(SlotKind.Utility, SlotKind.Ordinary, ItemCategory.Weapon, null));
        }
    }

    public class InventorySortTests
    {
        private static InventorySort.Entry E(int id, int x, int y, ItemCategory c, string name, int stack = 1) =>
            new InventorySort.Entry { Id = id, X = x, Y = y, Category = c, Name = name, Stack = stack };

        [Fact]
        public void Sorting_packs_rows_below_the_hotbar_and_never_moves_special_cells()
        {
            var layout = new SlotLayout(4, 4, 4, new[] { EquipSlot.Head });
            var items = new List<InventorySort.Entry>
            {
                E(0, 2, 0, ItemCategory.Weapon, "Sword"),       // hotbar: stays
                E(1, 7, 3, ItemCategory.Material, "Wood", 50),
                E(2, 0, 2, ItemCategory.Weapon, "Axe"),
                E(3, 1, 4, ItemCategory.Consumable, "Mead"),    // quick row: stays
                E(4, 0, 5, ItemCategory.Armor, "Helmet"),       // equipment: stays
                E(5, 5, 1, ItemCategory.Material, "Stone", 30),
            };
            var moves = InventorySort.Plan(layout, items).ToDictionary(m => m.Id, m => (m.X, m.Y));
            Assert.Equal((0, 1), moves[2]);
            Assert.Equal((1, 1), moves[5]);
            Assert.Equal((2, 1), moves[1]);
            Assert.DoesNotContain(0, moves.Keys);
            Assert.DoesNotContain(3, moves.Keys);
            Assert.DoesNotContain(4, moves.Keys);
        }

        [Fact]
        public void Sorting_a_sorted_inventory_moves_nothing()
        {
            var layout = new SlotLayout(4, 0, 0, null);
            var items = new List<InventorySort.Entry> { E(0, 0, 1, ItemCategory.Weapon, "Axe"), E(1, 1, 1, ItemCategory.Material, "Wood") };
            Assert.Empty(InventorySort.Plan(layout, items));
        }
    }
}
