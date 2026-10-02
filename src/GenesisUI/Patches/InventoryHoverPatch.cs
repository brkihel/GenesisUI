using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Modules.Windows;
using HarmonyLib;

namespace GenesisUI.Patches
{
    /// <summary>Project a GenesisUI cell onto the live native element for tooltip/touch consumers.</summary>
    [HarmonyPatch(typeof(InventoryGrid), "GetHoveredElement")]
    [GameContract("assembly_valheim", "InventoryGrid", "GetHoveredElement", Parameters = new string[0])]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Guard))]
    internal static class InventoryHoverPatch
    {
        private static readonly System.Action<InventoryGrid> Read = grid => _projected = InventoryWindowModule.ProjectHover(grid);
        private static InventoryElement _projected;
        [HarmonyPostfix]
        private static void Postfix(InventoryGrid __instance, ref InventoryElement __result)
        {
            _projected = null;
            if (Guard.Run("module:win.inventory", Read, __instance) && _projected != null) __result = _projected;
        }
    }
}
