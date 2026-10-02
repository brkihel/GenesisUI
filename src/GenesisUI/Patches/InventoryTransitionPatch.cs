using GenesisUI.Foundation.Contracts;
using GenesisUI.Gameplay;
using HarmonyLib;

namespace GenesisUI.Patches
{
    /// <summary>D-040: no destructive invalid-item cleanup inside our verified resize transaction.</summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DropInvalidItems))]
    [GameContract("assembly_valheim", "Humanoid", "DropInvalidItems")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Gameplay.InventorySafety))]
    internal static class InventoryTransitionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Humanoid __instance) => !InventorySafety.Transitioning(__instance);
    }
}
