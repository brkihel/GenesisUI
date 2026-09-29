using System.Collections.Generic;
using GenesisUI.Foundation.Contracts;
using HarmonyLib;

namespace GenesisUI.Patches
{
    /// <summary>
    /// The crafting window (D-032) mirrors vanilla's recipe list, which vanilla rebuilds on opening,
    /// on a tab switch and after crafting. This postfix only counts those rebuilds so the window
    /// re-reads the list then, and never per frame. It changes nothing; other mods' patches on the
    /// method run as before.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipeList")]
    [GameContract("assembly_valheim", "InventoryGui", "UpdateRecipeList")]
    internal static class CraftingListPatch
    {
        /// <summary>Incremented after every vanilla rebuild of the recipe list.</summary>
        internal static int Version { get; private set; }

        [HarmonyPostfix]
        private static void Postfix() => Version++;
    }

    /// <summary>Set while one of GenesisUI's text fields has the keyboard (the recipe search).</summary>
    internal static class TextInputFocus
    {
        internal static bool Active;
    }
}
