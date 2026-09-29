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
    [GameContract("assembly_valheim", "InventoryGui", "m_dragItem")]
    [GameContract("assembly_valheim", "InventoryGui", "m_dragInventory")]
    [GameContract("assembly_valheim", "Inventory", "GetItemAt")]
    [GameContract("assembly_valheim", "Inventory", "GetAllItems")]
    [GameContract("assembly_valheim", "Inventory", "ContainsItem")]
    [GameContract("assembly_valheim", "Inventory", "m_onChanged")]
    [GameContract("assembly_valheim", "InventoryGrid", "GetInventory")]
    [GameContract("assembly_valheim", "Humanoid", "GetInventory")]
    [GameContract("assembly_valheim", "Player", "Message")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_equipped")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_stack")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_gridPos")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_itemType")]
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
            if (__state) _equipDepth++;
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        [HarmonyPostfix]
        private static void EquipPostfix(Humanoid __instance, ItemDrop.ItemData item, bool __result, bool __state)
        {
            if (!__state || !__result || _selectionDepth > 0) return;
            Guard.Try("equipment equip relocation", () => EquipmentRules.AfterEquip((Player)__instance, item));
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
            Guard.Try("equipment unequip relocation", () => EquipmentRules.AfterUnequip((Player)__instance, item));
        }

        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        [HarmonyPrefix]
        private static bool SelectPrefix(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos,
                                         ItemDrop.ItemData ___m_dragItem, Inventory ___m_dragInventory,
                                         out SelectionState __state)
        {
            __state = default;
            var player = Player.m_localPlayer;
            var layout = InventoryModule.Current;
            if (player == null || layout == null || grid == null || grid.GetInventory() != player.GetInventory()) return true;
            if (___m_dragItem != null)
            {
                var targetSlot = layout.EquipmentAt(pos.x, pos.y);
                var kind = layout.KindAt(pos.x, pos.y);
                bool allowed = targetSlot.HasValue
                    ? ___m_dragInventory == player.GetInventory() &&
                      EquipmentRules.SlotFor(___m_dragItem) == targetSlot && ___m_dragItem.m_stack == 1 &&
                      (item == null || EquipmentRules.SlotFor(item) == targetSlot)
                    : kind == SlotKind.Ordinary;
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

        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        [HarmonyPostfix]
        private static void SelectPostfix(SelectionState __state)
        {
            if (!__state.Active || __state.Drag == null) return;
            Guard.Try("equipment drop reconciliation", () => ReconcileDrop(__state));
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
