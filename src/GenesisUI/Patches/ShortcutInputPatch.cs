using GenesisUI.Foundation.Contracts;
using GenesisUI.Gameplay;
using HarmonyLib;

namespace GenesisUI.Patches
{
    /// <summary>
    /// A GenesisUI hotkey held with its modifiers takes its key from vanilla (D-037): while Alt+X is
    /// held for an action slot, vanilla's buttons bound to X (Sit) read as not pressed, so the
    /// character does not sit. Postfixes on the result of ZInput's three button reads (PATCH-POLICY
    /// rule 1): vanilla and every other patch run as before; this only turns a true into false for the
    /// buttons of a claimed key. With the slots module off, or no hotkey held, it changes nothing; the
    /// common path is two comparisons, no allocation.
    /// </summary>
    [HarmonyPatch]
    [GameContract("assembly_utils", "ZInput", "GetButtonDown", Parameters = new[] { "System.String" })]
    [GameContract("assembly_utils", "ZInput", "GetButton", Parameters = new[] { "System.String" })]
    [GameContract("assembly_utils", "ZInput", "GetButtonUp", Parameters = new[] { "System.String" })]
    [GameContract("assembly_utils", "ZInput", "m_buttons", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.Dictionary\u00602[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[ZInput\u002BButtonDef, assembly_utils, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_utils", "ZInput+ButtonDef", "get_ButtonAction")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Gameplay.SlotHotkeys))]
    internal static class ShortcutInputPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown), typeof(string))]
        private static void Down(string name, ref bool __result)
        {
            if (__result && SlotHotkeys.Suppresses(name)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButton), typeof(string))]
        private static void Held(string name, ref bool __result)
        {
            if (__result && SlotHotkeys.Suppresses(name)) __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonUp), typeof(string))]
        private static void Up(string name, ref bool __result)
        {
            if (__result && SlotHotkeys.Suppresses(name)) __result = false;
        }
    }
}
