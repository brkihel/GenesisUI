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
    internal static class InventorySizePatch
    {
        [HarmonyPrefix]
        private static void Prefix(Player __instance, ref int rows)
        {
            var layout = InventoryModule.Current;
            if (layout == null || __instance != Player.m_localPlayer) return;
            rows = layout.TotalRows;
        }

        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            var layout = InventoryModule.Current;
            if (layout == null || __instance != Player.m_localPlayer || InventoryGui.instance == null) return;
            Guard.Try("inventory panel size", () => InventoryGui.instance.SetInventorySize(layout.Rows));
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

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetEmptySlots))]
        [HarmonyPostfix]
        private static void GetEmptySlots(Inventory __instance, ref int __result)
        {
            var layout = LayoutFor(__instance);
            if (layout != null) __result = OrdinaryFree(__instance, layout);
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveEmptySlot))]
        [HarmonyPostfix]
        private static void HaveEmptySlot(Inventory __instance, ref bool __result)
        {
            var layout = LayoutFor(__instance);
            if (layout != null) __result = OrdinaryFree(__instance, layout) > 0;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
        [HarmonyPostfix]
        private static void CanAddItem(Inventory __instance, ItemDrop.ItemData item, int stack, ref bool __result)
        {
            var layout = LayoutFor(__instance);
            if (layout == null || !__result || item == null) return;
            if (stack <= 0) stack = item.m_stack;
            // Vanilla counted every empty position; only ordinary ones take new stacks.
            __result = __instance.FindFreeStackSpace(item.m_shared.m_name, item.m_worldLevel) +
                       OrdinaryFree(__instance, layout) * item.m_shared.m_maxStackSize >= stack;
        }
    }
}
