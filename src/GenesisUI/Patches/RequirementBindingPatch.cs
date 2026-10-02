using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Modules.Windows;
using HarmonyLib;
using UnityEngine;

namespace GenesisUI.Patches
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    [GameContract("assembly_valheim", "InventoryGui", "SetupRequirement", Parameters = new[] { "UnityEngine.Transform", "Piece+Requirement", "Player", "System.Boolean", "System.Int32", "System.Int32" })]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Guard), typeof(GenesisUI.Modules.Windows.RequirementBindings))]
    internal static class RequirementBindingPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Transform elementRoot, Piece.Requirement req)
        {
            if (Guard.IsTripped("module:win.crafting")) return;
            try { RequirementBindings.Bind(elementRoot, req); }
            catch (System.Exception error) { Guard.Fault("module:win.crafting", error); }
        }
    }
}
