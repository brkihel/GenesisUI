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
    [GameContract("assembly_utils", "ZInput", "GetButtonDown", Parameters = new[] { "System.String" })]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Guard), typeof(GenesisUI.Patches.TextInputFocus))]
    internal static class InventoryTabKeyPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            if (GenesisUI.Foundation.Guard.IsTripped("module:win.shell")) return;
            try { Apply(); }
            catch (System.Exception error) { GenesisUI.Foundation.Guard.Fault("module:win.shell", error); }
        }
        private static void Apply()
        {
            // Typing in a GenesisUI text field (recipe search): Tab and E must not close the inventory.
            if (TextInputFocus.Active)
            {
                ZInput.ResetButtonStatus("Inventory");
                ZInput.ResetButtonStatus("Use");
                return;
            }
            var key = WindowShellModule.ActiveNextKey;
            if (WindowShellModule.Showing && ZInput.GetButtonDown("Use") && InventoryWindowModule.UseHovered())
            {
                WindowShellModule.ItemUseFrame = Time.frameCount;
                ZInput.ResetButtonStatus("Use");
                return;
            }
            if (key != KeyCode.None && BepInEx.UnityInput.Current.GetKeyDown(key)) ZInput.ResetButtonStatus("Use");
        }
    }
}
