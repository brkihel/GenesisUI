using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.InventoryModel;
using UnityEngine;

namespace GenesisUI.Gameplay
{
    /// <summary>F4.2b's vanilla worn kinds and the position plans applied after vanilla acts.</summary>
    internal static class EquipmentRules
    {
        internal static EquipSlot? SlotFor(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return null;
            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Helmet: return EquipSlot.Head;
                case ItemDrop.ItemData.ItemType.Chest: return EquipSlot.Chest;
                case ItemDrop.ItemData.ItemType.Legs: return EquipSlot.Legs;
                case ItemDrop.ItemData.ItemType.Shoulder: return EquipSlot.Cape;
                case ItemDrop.ItemData.ItemType.Utility: return EquipSlot.Belt;
                case ItemDrop.ItemData.ItemType.Trinket: return EquipSlot.Trinket;
                default: return null;
            }
        }

        internal static void AfterEquip(Player player, ItemDrop.ItemData item)
        {
            var layout = InventoryModule.Current;
            var slot = SlotFor(item);
            if (layout == null || player == null || player != Player.m_localPlayer ||
                item == null || !item.m_equipped || !slot.HasValue) return;
            var inventory = player.GetInventory();
            var all = inventory.GetAllItems();
            int index = all.IndexOf(item);
            if (index < 0) return;
            var target = layout.EquipmentPosition(slot.Value);
            var occupant = inventory.GetItemAt(target.X, target.Y);
            if (occupant != null && occupant != item && (SlotFor(occupant) != slot || occupant.m_equipped))
            {
                GenesisLog.Warn("Equipment", "refused equip relocation: incompatible or still-equipped occupant in " + slot);
                return;
            }
            var plan = EquipmentMove.Equip(layout, slot.Value, index, Snapshot(all));
            Apply(inventory, all, plan, "equip " + slot);
        }

        /// <summary>Move items already worn when a saved character first enters the new layout.</summary>
        internal static void ReconcileExisting(Player player)
        {
            if (player == null || InventoryModule.Current == null) return;
            var all = player.GetInventory().GetAllItems();
            // AfterEquip changes positions, not list membership. Take a snapshot to keep this
            // traversal stable if inventory change listeners rebuild the UI.
            var worn = new List<ItemDrop.ItemData>();
            foreach (var item in all)
                if (item.m_equipped && SlotFor(item).HasValue) worn.Add(item);
            foreach (var item in worn) AfterEquip(player, item);
        }

        internal static void AfterUnequip(Player player, ItemDrop.ItemData item)
        {
            var layout = InventoryModule.Current;
            if (layout == null || player == null || player != Player.m_localPlayer || item == null || item.m_equipped) return;
            var inventory = player.GetInventory();
            var all = inventory.GetAllItems();
            int index = all.IndexOf(item);
            if (index < 0) return;
            var plan = EquipmentMove.Unequip(layout, index, Snapshot(all));
            if (!plan.Ok && plan.Reason == "ordinary inventory full")
            {
                GenesisLog.Warn("Equipment", "unequipped item remains in its equipment cell: ordinary inventory full");
                player.Message(MessageHud.MessageType.Center, "$msg_inventoryfull");
                return;
            }
            Apply(inventory, all, plan, "unequip");
        }

        private static List<ItemAt> Snapshot(List<ItemDrop.ItemData> all)
        {
            var places = new List<ItemAt>(all.Count);
            for (int i = 0; i < all.Count; i++)
                places.Add(new ItemAt(i, all[i].m_gridPos.x, all[i].m_gridPos.y));
            return places;
        }

        private static void Apply(Inventory inventory, List<ItemDrop.ItemData> all, EquipmentMove.Plan plan, string action)
        {
            if (!plan.Ok)
            {
                GenesisLog.Warn("Equipment", "refused " + action + " relocation: " + plan.Reason);
                return;
            }
            if (plan.Moves.Count == 0) return;
            foreach (var move in plan.Moves) all[move.Id].m_gridPos = new Vector2i(move.X, move.Y);
            inventory.m_onChanged?.Invoke();
            GenesisLog.Info("Equipment", action + ": moved " + plan.Moves.Count + " item(s) without changing their count");
        }
    }
}
