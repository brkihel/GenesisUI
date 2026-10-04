using System;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.InventoryModel;
using HarmonyLib;

namespace GenesisUI.Gameplay
{
    /// <summary>D-040: one verified patch capability and a position-only, rollback-capable resize.</summary>
    [GameContract("assembly_valheim", "Inventory", "m_height", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "Inventory", "GetHeight")]
    [GameContract("assembly_valheim", "Inventory", "GetWidth")]
    [GameContract("assembly_valheim", "Inventory", "GetAllItems")]
    [GameContract("assembly_valheim", "Player", "TryGetUniqueKeyValue")]
    [GameContract("assembly_valheim", "Player", "AddUniqueKeyValue")]
    [GameContract("assembly_valheim", "Player", "RemoveUniqueKeyValue")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Humanoid", "GetInventory", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Inventory")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Player", "SetInventorySize", Parameters = new string[] { "System.Int32" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Void")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_gridPos", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Vector2i")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "x", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", "y", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_utils", "Vector2i", ".ctor", Parameters = new string[] { "System.Int32", "System.Int32" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance)]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.GuardedPatcher), typeof(GenesisUI.Foundation.ContractResolver), typeof(GenesisUI.InventoryModel.SlotLayout), typeof(GenesisUI.Gameplay.SavedLayout), typeof(GenesisUI.Foundation.Guard), typeof(GenesisUI.Foundation.GenesisLog))]
    internal static class InventorySafety
    {
        internal const string Owner = "module:inv.slots";
        private static AccessTools.FieldRef<Inventory, int> _height;
        private static Player _player;
        private static PositionSnapshot<ItemDrop.ItemData> _snapshot;
        internal static int RequiredRows { get; private set; }

        internal static bool Transitioning(Humanoid player) => _player != null && player == _player;
        internal static string MissingPatches => GuardedPatcher.MissingCapability(
            typeof(Patches.InventorySizePatch), typeof(Patches.InventoryPlacementPatches), typeof(Patches.WalletLoadPatch),
            typeof(Patches.EquipmentPatches), typeof(Patches.InventoryTransitionPatch));

        internal static void Resolve()
        {
            var missing = ContractResolver.Missing(typeof(InventorySafety));
            if (missing.Count > 0) throw new NotSupportedException(string.Join("; ", missing));
            if (MissingPatches != null) throw new NotSupportedException(MissingPatches);
            _height = AccessTools.FieldRefAccess<Inventory, int>("m_height");
        }

        internal static void Resize(Player player, SlotLayout layout, PositionSnapshot<ItemDrop.ItemData> snapshot,
                                    System.Collections.Generic.IReadOnlyList<Move> moves, Action reconcile)
        {
            if (MissingPatches != null || _height == null || _player != null) throw new InvalidOperationException("Inventory capability is unavailable or transition is nested");
            var inventory = player.GetInventory();
            if (inventory.GetWidth() != SlotLayout.Width || !snapshot.Matches(inventory.GetAllItems()))
                throw new InvalidOperationException("Inventory width or snapshot is incompatible");
            int oldHeight = inventory.GetHeight();
            bool hadRows = player.TryGetUniqueKeyValue("invrows", out var oldRows);
            bool hadLayout = player.TryGetUniqueKeyValue(SavedLayout.Key, out var oldLayout);
            _player = player;
            _snapshot = snapshot;
            try
            {
                RequiredRows = Math.Max(oldHeight, layout.TotalRows);
                if (RequiredRows > SlotLayout.VanillaMaxRows) throw new InvalidOperationException("Inventory exceeds the verified height limit");
                player.SetInventorySize(RequiredRows);
                if (inventory.GetHeight() < RequiredRows) throw new InvalidOperationException("Another resize hook reduced the required height");
                snapshot.Apply(inventory.GetAllItems(), moves, inventory.GetWidth(), inventory.GetHeight(), WritePosition);
                RequiredRows = layout.TotalRows;
                player.SetInventorySize(RequiredRows);
                if (inventory.GetHeight() < RequiredRows) throw new InvalidOperationException("Final inventory height cannot hold the layout");
                reconcile();
                if (!snapshot.Matches(inventory.GetAllItems())) throw new InvalidOperationException("A callback changed item membership/count during relocation");
                foreach (var item in inventory.GetAllItems())
                    if (item.m_gridPos.x < 0 || item.m_gridPos.x >= inventory.GetWidth() || item.m_gridPos.y < 0 || item.m_gridPos.y >= inventory.GetHeight())
                        throw new InvalidOperationException("A callback placed an item outside the inventory");
                SavedLayout.Write(player, layout);
            }
            catch
            {
                snapshot.RestoreRemaining(inventory.GetAllItems(), WritePosition);
                // Restore our original native height without invoking another destructive resize hook.
                int safeHeight = oldHeight;
                foreach (var item in inventory.GetAllItems()) safeHeight = Math.Max(safeHeight, item.m_gridPos.y + 1);
                _height(inventory) = safeHeight;
                Guard.Try("restore inventory row metadata", () => { if (hadRows) player.AddUniqueKeyValue("invrows", oldRows); else player.RemoveUniqueKeyValue("invrows"); });
                Guard.Try("restore layout metadata", () => { if (hadLayout) player.AddUniqueKeyValue(SavedLayout.Key, oldLayout); else player.RemoveUniqueKeyValue(SavedLayout.Key); });
                GenesisLog.Error("Inventory", "layout transition rolled back for remaining original items; foreign membership/count changes were preserved");
                throw;
            }
            finally { RequiredRows = 0; _player = null; _snapshot = null; }
        }

        internal static void WritePosition(ItemDrop.ItemData item, int x, int y)
        {
            item.m_gridPos = new Vector2i(x, y);
            if (_snapshot != null) _snapshot.RecordWrite(item, x, y);
        }
    }
}
