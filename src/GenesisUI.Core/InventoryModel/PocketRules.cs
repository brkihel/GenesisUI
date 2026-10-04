using System;

namespace GenesisUI.InventoryModel
{
    /// <summary>Native item identities accepted by the three compact non-equipment pockets.</summary>
    public static class PocketRules
    {
        public static bool IsPocket(EquipSlot slot) => slot == EquipSlot.Wallet || slot == EquipSlot.KeyOne || slot == EquipSlot.KeyTwo;
        public static bool Accepts(EquipSlot slot, string prefab) =>
            slot == EquipSlot.Wallet ? string.Equals(prefab, "Coins", StringComparison.Ordinal) :
            (slot == EquipSlot.KeyOne || slot == EquipSlot.KeyTwo) &&
            (string.Equals(prefab, "CryptKey", StringComparison.Ordinal) || string.Equals(prefab, "DvergrKey", StringComparison.Ordinal));
        public static bool HasStackCapacity(long freeStackSpace, int freeCells, int stackLimit, int requested) =>
            requested >= 0 && freeStackSpace >= 0 && freeCells >= 0 && stackLimit > 0 &&
            (long)freeStackSpace + (long)freeCells * stackLimit >= requested;
    }
}
