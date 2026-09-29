using GenesisUI.Foundation.Contracts;
using GenesisUI.Modules.Windows;
using HarmonyLib;
using UnityEngine;

namespace GenesisUI.Patches
{
    /// <summary>
    /// Vanilla closes the inventory when "Use" (E) is pressed. When the window shell is showing and
    /// its next-tab key (E by default, Diego's concept) is pressed, that press switches the tab
    /// instead: this void prefix clears "Use" for that frame before vanilla reads it.
    /// PATCH-POLICY rule 2 (a guarding void prefix): vanilla's Update and every other patch on it
    /// still run in full; with the shell hidden or faulted (ActiveNextKey = None) it does nothing.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "Update")]
    [GameContract("assembly_valheim", "InventoryGui", "Update")]
    [GameContract("assembly_utils", "ZInput", "ResetButtonStatus", Parameters = new[] { "System.String" })]
    internal static class InventoryTabKeyPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            var key = WindowShellModule.ActiveNextKey;
            if (key != KeyCode.None && BepInEx.UnityInput.Current.GetKeyDown(key)) ZInput.ResetButtonStatus("Use");
        }
    }
}
