using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.InventoryModel;
using UnityEngine;

namespace GenesisUI.Gameplay
{
    /// <summary>F4.2b's vanilla worn kinds and the position plans applied after vanilla acts.</summary>
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_shared", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData\u002BSharedData")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData\u002BSharedData", "m_itemType", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData\u002BItemType")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Player")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_equipped", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Humanoid", "GetInventory", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Inventory")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Inventory", "GetAllItems", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.List\u00601[[ItemDrop\u002BItemData, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Inventory", "GetItemAt", Parameters = new string[] { "System.Int32", "System.Int32" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Character", "Message", Parameters = new string[] { "MessageHud\u002BMessageType", "System.String", "System.Int32", "UnityEngine.Sprite", "System.Boolean" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Void")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_gridPos", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Vector2i")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "x", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "y", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Inventory", "GetWidth", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Inventory", "GetHeight", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Inventory", "m_onChanged", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Action")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_stack", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", ".ctor", Parameters = new string[] { "System.Int32", "System.Int32" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance)]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.InventoryModel.SlotLayout), typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.InventoryModel.EquipmentMove))]
    internal static class EquipmentRules
    {
        private static bool _reconciling;
        private static readonly List<ItemDrop.ItemData> Worn = new List<ItemDrop.ItemData>(16);
        internal static EquipSlot? SlotFor(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return null;
            var foreign = Adapters.InventoryIntegrations.SlotFor(item);
            if (foreign.HasValue) return foreign;
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
            var target = layout.EquipmentPosition(slot.Value);
            if (target.X < 0 || (item.m_gridPos.x == target.X && item.m_gridPos.y == target.Y)) return;
            var all = inventory.GetAllItems();
            var snapshot = new PositionSnapshot<ItemDrop.ItemData>(all, entry => entry.m_stack, entry => (entry.m_gridPos.x, entry.m_gridPos.y));
            int index = all.IndexOf(item);
            if (index < 0) return;
            var occupant = inventory.GetItemAt(target.X, target.Y);
            if (occupant != null && occupant != item && (SlotFor(occupant) != slot || occupant.m_equipped))
            {
                GenesisLog.Warn("Equipment", "refused equip relocation: incompatible or still-equipped occupant in " + slot);
                return;
            }
            var plan = EquipmentMove.Equip(layout, slot.Value, index, snapshot.Positions);
            Apply(inventory, snapshot, plan, "equip " + slot);
        }

        /// <summary>Move items already worn when a saved character first enters the new layout.</summary>
        internal static void ReconcileExisting(Player player)
        {
            if (player == null || InventoryModule.Current == null) return;
            if (_reconciling) return;
            _reconciling = true;
            try
            {
            var all = player.GetInventory().GetAllItems();
            // AfterEquip changes positions, not list membership. Take a snapshot to keep this
            // traversal stable if inventory change listeners rebuild the UI.
            var worn = Worn;
            worn.Clear();
            foreach (var item in all)
                if (item.m_equipped && SlotFor(item).HasValue) worn.Add(item);
            foreach (var item in worn) AfterEquip(player, item);
            }
            finally { _reconciling = false; }
        }

        internal static void AfterUnequip(Player player, ItemDrop.ItemData item)
        {
            var layout = InventoryModule.Current;
            if (layout == null || player == null || player != Player.m_localPlayer || item == null || item.m_equipped) return;
            var inventory = player.GetInventory();
            var all = inventory.GetAllItems();
            var snapshot = new PositionSnapshot<ItemDrop.ItemData>(all, entry => entry.m_stack, entry => (entry.m_gridPos.x, entry.m_gridPos.y));
            int index = all.IndexOf(item);
            if (index < 0) return;
            var plan = EquipmentMove.Unequip(layout, index, snapshot.Positions);
            if (!plan.Ok && plan.Reason == "ordinary inventory full")
            {
                GenesisLog.Warn("Equipment", "unequipped item remains in its equipment cell: ordinary inventory full");
                player.Message(MessageHud.MessageType.Center, "$msg_inventoryfull");
                return;
            }
            Apply(inventory, snapshot, plan, "unequip");
        }

        private static void Apply(Inventory inventory, PositionSnapshot<ItemDrop.ItemData> snapshot, EquipmentMove.Plan plan, string action)
        {
            if (!plan.Ok)
            {
                GenesisLog.Warn("Equipment", "refused " + action + " relocation: " + plan.Reason);
                return;
            }
            if (plan.Moves.Count == 0) return;
            try
            {
                snapshot.Apply(inventory.GetAllItems(), plan.Moves, inventory.GetWidth(), inventory.GetHeight(), InventorySafety.WritePosition);
                inventory.m_onChanged?.Invoke();
                if (!snapshot.Matches(inventory.GetAllItems())) throw new System.InvalidOperationException("Inventory callback changed the relocation snapshot");
            }
            catch { snapshot.RestoreRemaining(inventory.GetAllItems(), InventorySafety.WritePosition); throw; }
            GenesisLog.Info("Equipment", action + ": moved " + plan.Moves.Count + " item(s) without changing their count");
        }
    }
}
