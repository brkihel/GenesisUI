using System;
using GenesisUI.InventoryModel;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class PocketRulesTests
    {
        [Theory]
        [InlineData(EquipSlot.Wallet, "Coins", true)]
        [InlineData(EquipSlot.Wallet, "CryptKey", false)]
        [InlineData(EquipSlot.KeyOne, "CryptKey", true)]
        [InlineData(EquipSlot.KeyTwo, "DvergrKey", true)]
        [InlineData(EquipSlot.KeyOne, "Coins", false)]
        [InlineData(EquipSlot.Ring, "Coins", false)]
        [InlineData(EquipSlot.Wallet, "Coins(Clone)", false)]
        [InlineData(EquipSlot.KeyOne, null, false)]
        public void Reserved_pockets_accept_only_the_native_identity(EquipSlot slot, string prefab, bool expected) =>
            Assert.Equal(expected, PocketRules.Accepts(slot, prefab));

        [Fact]
        public void Large_wallet_capacity_never_overflows_to_a_refusal()
        {
            Assert.True(PocketRules.HasStackCapacity(int.MaxValue - 200, 31, int.MaxValue, 500));
            Assert.True(PocketRules.HasStackCapacity(0, 1, int.MaxValue, int.MaxValue));
            Assert.True(PocketRules.HasStackCapacity((long)int.MaxValue * 3, 0, int.MaxValue, int.MaxValue));
            Assert.False(PocketRules.HasStackCapacity(499, 0, int.MaxValue, 500));
            Assert.False(PocketRules.HasStackCapacity(-1, 1, int.MaxValue, 1));
        }

        [Fact]
        public void All_modular_slots_remain_unique_outside_ordinary_inventory()
        {
            var slots = (EquipSlot[])Enum.GetValues(typeof(EquipSlot));
            var layout = new SlotLayout(6, 4, 3, slots);
            Assert.Equal(13, layout.Equipment.Count);
            var positions = new System.Collections.Generic.HashSet<(int, int)>();
            foreach (var slot in slots)
            {
                var pos = layout.EquipmentPosition(slot);
                Assert.True(pos.X >= 0);
                Assert.False(layout.IsOrdinary(pos.X, pos.Y));
                Assert.True(positions.Add((pos.X, pos.Y)));
                Assert.Equal(slot, layout.EquipmentAt(pos.X, pos.Y));
            }
        }

        [Fact]
        public void Existing_equipment_mask_values_stay_compatible()
        {
            Assert.Equal(9, (int)EquipSlot.Ring);
            Assert.Equal(10, (int)EquipSlot.Wallet);
            Assert.Equal(12, (int)EquipSlot.KeyTwo);
        }
    }
}
