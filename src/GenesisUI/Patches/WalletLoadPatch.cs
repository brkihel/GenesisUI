using System;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using HarmonyLib;

namespace GenesisUI.Patches
{
    /// <summary>D-044: preserve saved coin counts even while the wallet drawing is disabled.</summary>
    [HarmonyPatch]
    [GameContract("assembly_valheim", "Inventory", "Load", Parameters = new[] { "ZPackage" })]
    [GameContract("assembly_valheim", "Inventory", "Load", Parameters = new[] { "ZPackage", "System.Boolean" })]
    [GameContract("assembly_valheim", "ObjectDB", "get_instance", Parameters = new string[0])]
    [GameContract("assembly_valheim", "ObjectDB", "GetItemPrefab", Parameters = new[] { "System.String" })]
    [GameContract("assembly_valheim", "ItemDrop", "m_itemData", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "ItemDrop+ItemData")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_shared", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "ItemDrop+ItemData+SharedData")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_maxStackSize", Kind = ContractMemberKind.Field, Static = ContractStatic.Instance, ValueType = "System.Int32")]
    [ContractDependency(typeof(Guard))]
    internal static class WalletLoadPatch
    {
        [HarmonyTargetMethods]
        private static System.Reflection.MethodBase[] Targets() => new System.Reflection.MethodBase[]
        {
            AccessTools.Method(typeof(Inventory), nameof(Inventory.Load), new[] { typeof(ZPackage) }),
            AccessTools.Method(typeof(Inventory), nameof(Inventory.Load), new[] { typeof(ZPackage), typeof(bool) }),
        };
        internal struct State { internal ItemDrop.ItemData.SharedData Shared; internal int Limit; }
        [HarmonyPrefix]
        private static void Before(ref State __state)
        {
            try
            {
                if (ObjectDB.instance == null) return;
                var prefab = ObjectDB.instance.GetItemPrefab("Coins");
                var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (drop == null || drop.m_itemData == null) return;
                __state.Shared = drop.m_itemData.m_shared;
                __state.Limit = __state.Shared.m_maxStackSize;
                __state.Shared.m_maxStackSize = int.MaxValue;
            }
            catch (Exception error) { Guard.Fault("wallet:load", error); }
        }
        [HarmonyFinalizer]
        private static Exception After(Exception __exception, State __state)
        {
            if (__state.Shared != null && __state.Shared.m_maxStackSize == int.MaxValue)
                __state.Shared.m_maxStackSize = __state.Limit;
            return __exception;
        }
    }
}
