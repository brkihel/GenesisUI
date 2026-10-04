using System;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Gameplay;
using GenesisUI.InventoryModel;
using HarmonyLib;

namespace GenesisUI.Patches
{
    /// <summary>
    /// D-030 (1): vanilla's own resize holds GenesisUI's special rows. Whatever rows are asked for
    /// (the admin's setting, or vanilla re-applying the stored "invrows" on spawn), the height becomes
    /// the whole layout, so DropInvalidItems never finds a special item outside the grid. The
    /// postfix sizes vanilla's player panel to the ordinary rows only.
    /// No-op while the inventory module is not active (Current == null).
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetInventorySize))]
    [GameContract("assembly_valheim", "Player", "SetInventorySize")]
    [GameContract("assembly_valheim", "InventoryGui", "SetInventorySize")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Player")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "InventoryGui", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "InventoryGui")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.InventoryModel.SlotLayout), typeof(GenesisUI.Gameplay.InventorySafety), typeof(GenesisUI.Foundation.Guard))]
    internal static class InventorySizePatch
    {
        [HarmonyPrefix]
        private static void Prefix(Player __instance, ref int rows)
        {
            if (Guard.IsTripped(InventorySafety.Owner)) return;
            var original = rows;
            try
            {
            var layout = InventoryModule.Current;
            if (layout == null || __instance != Player.m_localPlayer) return;
            rows = System.Math.Max(layout.TotalRows, InventorySafety.RequiredRows);
                    }
            catch (System.Exception error) { rows = original; Guard.Fault(InventorySafety.Owner, error); }
        }

        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            if (Guard.IsTripped(InventorySafety.Owner)) return;

            try
            {
            var layout = InventoryModule.Current;
            if (layout == null || __instance != Player.m_localPlayer || InventoryGui.instance == null) return;
            Guard.Run("module:inv.slots", () => InventoryGui.instance.SetInventorySize(layout.Rows));
                    }
            catch (System.Exception error) {  Guard.Fault(InventorySafety.Owner, error); }
        }
    }

    /// <summary>
    /// D-030 (2): automatic placement (pickups, crafting results, take all overflow) and free-space
    /// counts only see the ordinary rows of the local player's inventory. Postfixes on the result:
    /// vanilla and every other patch run first and unchanged; other inventories are never touched.
    /// </summary>
    [HarmonyPatch]
    [GameContract("assembly_valheim", "Inventory", "FindEmptySlot")]
    [GameContract("assembly_valheim", "Inventory", "GetEmptySlots")]
    [GameContract("assembly_valheim", "Inventory", "HaveEmptySlot")]
    [GameContract("assembly_valheim", "Inventory", "CanAddItem", Parameters = new[] { "ItemDrop+ItemData", "System.Int32" })]
    [GameContract("assembly_valheim", "Inventory", "FindFreeStackSpace")]
    [GameContract("assembly_valheim", "Inventory", "GetItemAt")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Player")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Humanoid", "GetInventory", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Inventory")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Inventory", "GetAllItems", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.List\u00601[[ItemDrop\u002BItemData, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_gridPos", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Vector2i")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "x", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "y", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", ".ctor", Parameters = new string[] { "System.Int32", "System.Int32" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance)]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_stack", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_shared", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData\u002BSharedData")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData\u002BSharedData", "m_name", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_worldLevel", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData\u002BSharedData", "m_maxStackSize", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.InventoryModel.SlotLayout))]
    [GameContract("assembly_valheim", "Inventory", "GetWidth", Parameters = new string[0])]
    [GameContract("assembly_valheim", "Inventory", "GetHeight", Parameters = new string[0])]
    internal static class InventoryPlacementPatches
    {
        private static SlotLayout LayoutFor(Inventory inventory)
        {
            var layout = InventoryModule.Current;
            if (layout == null) return null;
            var player = Player.m_localPlayer;
            return player != null && player.GetInventory() == inventory ? layout : null;
        }

        private static int OrdinaryFree(Inventory inventory, SlotLayout layout)
        {
            int used = 0;
            foreach (var item in inventory.GetAllItems())
                if (layout.IsOrdinary(item.m_gridPos.x, item.m_gridPos.y)) used++;
            return layout.OrdinarySlots - used;
        }

        [HarmonyPatch(typeof(Inventory), "FindEmptySlot")]
        [HarmonyPostfix]
        private static void FindEmptySlot(Inventory __instance, bool topFirst, ref Vector2i __result)
        {
            if (Guard.IsTripped(InventorySafety.Owner)) return;
            var original = __result;
            try
            {
            var layout = LayoutFor(__instance);
            if (layout == null || (__result.x >= 0 && layout.IsOrdinary(__result.x, __result.y))) return;
            // Vanilla found no slot, or one in a special row: look again in the ordinary rows only,
            // in vanilla's order (top first for weapons and tools, bottom first for the rest).
            __result = new Vector2i(-1, -1);
            for (int i = 0; i < layout.Rows; i++)
            {
                int y = topFirst ? i : layout.Rows - 1 - i;
                for (int x = 0; x < SlotLayout.Width; x++)
                {
                    if (__instance.GetItemAt(x, y) != null) continue;
                    __result = new Vector2i(x, y);
                    return;
                }
            }
                    }
            catch (System.Exception error) { __result = original; Guard.Fault(InventorySafety.Owner, error); }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetEmptySlots))]
        [HarmonyPostfix]
        private static void GetEmptySlots(Inventory __instance, ref int __result)
        {
            if (Guard.IsTripped(InventorySafety.Owner)) return;
            var original = __result;
            try
            {
            var layout = LayoutFor(__instance);
            if (layout != null) __result = OrdinaryFree(__instance, layout);
                    }
            catch (System.Exception error) { __result = original; Guard.Fault(InventorySafety.Owner, error); }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveEmptySlot))]
        [HarmonyPostfix]
        private static void HaveEmptySlot(Inventory __instance, ref bool __result)
        {
            if (Guard.IsTripped(InventorySafety.Owner)) return;
            var original = __result;
            try
            {
            var layout = LayoutFor(__instance);
            if (layout != null) __result = OrdinaryFree(__instance, layout) > 0;
                    }
            catch (System.Exception error) { __result = original; Guard.Fault(InventorySafety.Owner, error); }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        private static void CanAddItem(Inventory __instance, ItemDrop.ItemData item, int stack, bool __runOriginal, ref bool __result)
        {
            if (Guard.IsTripped(InventorySafety.Owner)) return;
            var original = __result;
            try
            {
            var layout = LayoutFor(__instance);
            if (item == null) return;
            if (stack <= 0) stack = item.m_stack;
            if (__runOriginal && InventoryModule.WalletItem(item))
            {
                // Correct the proven native Int32 overflow before other mods' normal-priority
                // postfixes can apply their own restrictions. Never create or merge an item.
                long free = 0;
                var items = __instance.GetAllItems();
                foreach (var held in items)
                    if (held.m_shared.m_name == item.m_shared.m_name && held.m_worldLevel == item.m_worldLevel)
                        free += Math.Max(0, held.m_shared.m_maxStackSize - held.m_stack);
                int cells = __instance.GetWidth() * __instance.GetHeight() - items.Count;
                bool raw = unchecked((int)free + cells * item.m_shared.m_maxStackSize) >= stack;
                if (__result != raw) return;
                if (free + (long)cells * item.m_shared.m_maxStackSize > int.MaxValue)
                    __result = PocketRules.HasStackCapacity(free, layout != null ? OrdinaryFree(__instance, layout) : cells, item.m_shared.m_maxStackSize, stack);
                if (layout != null && __result)
                    __result = PocketRules.HasStackCapacity(free, OrdinaryFree(__instance, layout), item.m_shared.m_maxStackSize, stack);
                return;
            }
            if (layout == null || !__result) return;
            // Vanilla counted every empty position; only ordinary ones take new stacks.
            __result = PocketRules.HasStackCapacity(__instance.FindFreeStackSpace(item.m_shared.m_name, item.m_worldLevel),
                       OrdinaryFree(__instance, layout), item.m_shared.m_maxStackSize, stack);
                    }
            catch (System.Exception error) { __result = original; Guard.Fault(InventorySafety.Owner, error); }
        }
    }
}
