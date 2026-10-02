using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using UnityEngine;

namespace GenesisUI.Modules.Windows
{
    /// <summary>
    /// Vanilla's four inventory panels (player grid, container, crafting, item info), hidden while any
    /// GenesisUI window is shown. Every window hides the same four. When each window kept its own
    /// hiding, a tab switch had the new window find the old window's group and keep it, and the old
    /// window then removed that group: vanilla showed through (R-059). Now one owner hides them, held
    /// by every shown window; they come back when the last window lets go.
    /// </summary>
    [GameContract("assembly_valheim", "InventoryGui", "m_player", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "InventoryGui", "m_container", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "InventoryGui", "m_crafting", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "InventoryGui", "m_info", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.VanillaSkin))]
    internal static class VanillaPanels
    {
        private static readonly VanillaSkin Skin = new VanillaSkin("windows:vanilla-panels");
        private static readonly HashSet<string> Holders = new HashSet<string>();

        /// <summary>The window of <paramref name="owner"/> is shown: vanilla's panels stay hidden.</summary>
        internal static void Hold(string owner, InventoryGui gui)
        {
            if (!Holders.Add(owner)) return;
            if (Skin.Count > 0) return; // already hidden for another window
            Hide(gui.m_player.gameObject);
            Hide(gui.m_container.gameObject);
            Hide(gui.m_crafting.gameObject);
            Hide(gui.m_info.gameObject);
        }

        /// <summary>The window closed (or faulted): the last one out gives vanilla its panels back.</summary>
        internal static void Release(string owner)
        {
            // Host cleanup also covers partial Build/Teardown and shell subclaims.
            string id = owner.StartsWith("module:", System.StringComparison.Ordinal) ? owner.Substring(7) : owner;
            Holders.RemoveWhere(holder => holder == id || holder.StartsWith(id + ":", System.StringComparison.Ordinal));
            if (Holders.Count > 0) return;
            Skin.Restore();
        }

        internal static int HolderCount => Holders.Count;
        internal static void Shutdown() { Holders.Clear(); Skin.Restore(); }

        private static void Hide(GameObject go)
        {
            Skin.Hidden(go);
        }
    }
}
