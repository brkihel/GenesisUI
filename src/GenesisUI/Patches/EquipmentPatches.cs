using System;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Gameplay;
using GenesisUI.InventoryModel;
using HarmonyLib;

namespace GenesisUI.Patches
{
    /// <summary>
    /// F4.2b: vanilla still equips, unequips and drops. These hooks only validate a drop into
    /// one of our reserved cells and arrange the item in that cell after vanilla succeeded.
    /// The OnSelectedItem skipping prefix is approved in D-030 for invalid special-cell drops.
    /// </summary>
    [HarmonyPatch]
    [GameContract("assembly_valheim", "Humanoid", "EquipItem", Parameters = new[] { "ItemDrop+ItemData", "System.Boolean" })]
    [GameContract("assembly_valheim", "Humanoid", "UnequipItem", Parameters = new[] { "ItemDrop+ItemData", "System.Boolean" })]
    [GameContract("assembly_valheim", "InventoryGui", "OnSelectedItem", Parameters = new[]
        { "InventoryGrid", "ItemDrop+ItemData", "Vector2i", "InventoryGrid+Modifier" })]
    [GameContract("assembly_valheim", "InventoryGui", "m_dragItem", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData")]
    [GameContract("assembly_valheim", "InventoryGui", "m_dragInventory", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Inventory")]
    [GameContract("assembly_valheim", "Inventory", "GetItemAt")]
    [GameContract("assembly_valheim", "Inventory", "GetAllItems")]
    [GameContract("assembly_valheim", "Inventory", "ContainsItem")]
    [GameContract("assembly_valheim", "Inventory", "m_onChanged", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Action")]
    [GameContract("assembly_valheim", "InventoryGrid", "GetInventory")]
    [GameContract("assembly_valheim", "Humanoid", "GetInventory")]
    [GameContract("assembly_valheim", "Player", "Message")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_equipped", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_stack", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_gridPos", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Vector2i")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_itemType", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData\u002BItemType")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Player")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "x", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "y", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Character", "Message", Parameters = new string[] { "MessageHud\u002BMessageType", "System.String", "System.Int32", "UnityEngine.Sprite", "System.Boolean" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Void")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Guard), typeof(GenesisUI.InventoryModel.SlotLayout), typeof(GenesisUI.Gameplay.EquipmentRules), typeof(GenesisUI.Gameplay.ItemCategories), typeof(GenesisUI.InventoryModel.SlotRules), typeof(GenesisUI.Foundation.GenesisLog))]
    internal static class EquipmentPatches
    {
        [ThreadStatic] private static int _equipDepth;
        [ThreadStatic] private static int _selectionDepth;

        private struct SelectionState
        {
            public bool Active;
            public ItemDrop.ItemData Drag;
            public ItemDrop.ItemData FormerTarget;
            public Vector2i Source;
            public Vector2i Target;
            public EquipSlot? SourceSlot;
            public EquipSlot? TargetSlot;
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        [HarmonyPrefix]
        private static void EquipPrefix(Humanoid __instance, out bool __state)
        {
            __state = __instance == Player.m_localPlayer && InventoryModule.Current != null;
            if (Guard.IsTripped(InventorySafety.Owner)) __state = false;
            if (__state) _equipDepth++;
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        [HarmonyPostfix]
        private static void EquipPostfix(Humanoid __instance, ItemDrop.ItemData item, bool __result, bool __state)
        {
            if (!__state || !__result || _selectionDepth > 0) return;
            Guard.Run("module:inv.slots", () => EquipmentRules.AfterEquip((Player)__instance, item));
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        [HarmonyFinalizer]
        private static Exception EquipFinalizer(Exception __exception, bool __state)
        {
            if (__state) _equipDepth--;
            return __exception;
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        [HarmonyPostfix]
        private static void UnequipPostfix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (_equipDepth > 0 || _selectionDepth > 0 || __instance != Player.m_localPlayer) return;
            Guard.Run("module:inv.slots", () => EquipmentRules.AfterUnequip((Player)__instance, item));
        }

        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        [HarmonyPrefix]
        private static bool SelectPrefix(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos,
                                         ItemDrop.ItemData ___m_dragItem, Inventory ___m_dragInventory,
                                         out SelectionState __state)
        {
            __state = default;
            if (Guard.IsTripped(InventorySafety.Owner)) return true;
            try
            {
            var player = Player.m_localPlayer;
            var layout = InventoryModule.Current;
            if (player == null || layout == null || grid == null || grid.GetInventory() != player.GetInventory()) return true;
            if (___m_dragItem != null)
            {
                var targetSlot = layout.EquipmentAt(pos.x, pos.y);
                var kind = layout.KindAt(pos.x, pos.y);
                bool allowed;
                if (targetSlot.HasValue)
                    allowed = ___m_dragInventory == player.GetInventory() &&
                              EquipmentRules.SlotFor(___m_dragItem) == targetSlot && ___m_dragItem.m_stack == 1 &&
                              (item == null || EquipmentRules.SlotFor(item) == targetSlot);
                else if (!kind.HasValue) allowed = false;
                else
                {
                    // Quick-use and utility (F4.2c): the dragged item must fit the target and, on a
                    // swap, the target's item must fit where the dragged one came from.
                    var sourceKind = ___m_dragInventory == player.GetInventory()
                        ? layout.KindAt(___m_dragItem.m_gridPos.x, ___m_dragItem.m_gridPos.y) ?? SlotKind.Ordinary
                        : SlotKind.Ordinary;
                    allowed = sourceKind == SlotKind.Equipment && item != null
                        ? false
                        : SlotRules.AllowsMove(sourceKind == SlotKind.Equipment ? SlotKind.Ordinary : sourceKind, kind.Value,
                            ItemCategories.Of(___m_dragItem), item != null ? ItemCategories.Of(item) : (ItemCategory?)null);
                }
                if (!allowed)
                {
                    player.Message(MessageHud.MessageType.Center, "$msg_cantuseitem");
                    GenesisLog.Warn("Equipment", "refused invalid special-cell drop");
                    return false;
                }
                __state.Drag = ___m_dragItem;
                __state.Source = ___m_dragItem.m_gridPos;
                __state.Target = pos;
                __state.SourceSlot = layout.EquipmentAt(__state.Source.x, __state.Source.y);
                __state.TargetSlot = targetSlot;
                __state.FormerTarget = item;
            }
            __state.Active = true;
            _selectionDepth++;
            return true;
            }
            catch (Exception error) { __state = default; Guard.Fault(InventorySafety.Owner, error); return true; }
        }

        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        [HarmonyPostfix]
        private static void SelectPostfix(SelectionState __state)
        {
            if (!__state.Active || __state.Drag == null) return;
            Guard.Run("module:inv.slots", () => ReconcileDrop(__state));
        }

        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        [HarmonyFinalizer]
        private static Exception SelectFinalizer(Exception __exception, SelectionState __state)
        {
            if (__state.Active) _selectionDepth--;
            return __exception;
        }

        private static void ReconcileDrop(SelectionState state)
        {
            var player = Player.m_localPlayer;
            var layout = InventoryModule.Current;
            if (player == null || layout == null) return;
            var inventory = player.GetInventory();
            if (!inventory.ContainsItem(state.Drag)) return;
            var now = state.Drag.m_gridPos;
            if (state.TargetSlot.HasValue && now.x == state.Target.x && now.y == state.Target.y && !state.Drag.m_equipped)
            {
                if (!player.EquipItem(state.Drag, triggerEquipEffects: false))
                {
                    // Vanilla moved the item but could not equip it (e.g. broken or swimming).
                    // Restore the exact two original positions; no item enters the wrong cell.
                    var sourceOccupant = inventory.GetItemAt(state.Source.x, state.Source.y);
                    var targetOccupant = inventory.GetItemAt(state.Target.x, state.Target.y);
                    if (targetOccupant == state.Drag && sourceOccupant == state.FormerTarget)
                    {
                        if (state.FormerTarget != null) state.FormerTarget.m_gridPos = state.Target;
                        state.Drag.m_gridPos = state.Source;
                        inventory.m_onChanged?.Invoke();
                        GenesisLog.Warn("Equipment", "drop into equipment cell rolled back because vanilla refused equip");
                    }
                    else GenesisLog.Warn("Equipment", "vanilla refused equip but positions changed during drop; left inventory untouched");
                }
            }
            else if (state.SourceSlot.HasValue &&
                     (now.x != state.Source.x || now.y != state.Source.y) &&
                     !layout.EquipmentAt(now.x, now.y).HasValue && state.Drag.m_equipped)
                player.UnequipItem(state.Drag, triggerEquipEffects: false);
        }
    }
}
