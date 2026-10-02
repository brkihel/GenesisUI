using GenesisUI.InventoryModel;

namespace GenesisUI.Gameplay
{
    /// <summary>The game's item type mapped to GenesisUI's categories (slot rules, sorting, filter).</summary>
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_shared", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData\u002BSharedData")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData\u002BSharedData", "m_itemType", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData\u002BItemType")]
    internal static class ItemCategories
    {
        internal static ItemCategory Of(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return ItemCategory.Misc;
            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Torch:
                    return ItemCategory.Weapon;
                case ItemDrop.ItemData.ItemType.Shield: return ItemCategory.Shield;
                case ItemDrop.ItemData.ItemType.Tool: return ItemCategory.Tool;
                case ItemDrop.ItemData.ItemType.Ammo:
                case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                    return ItemCategory.Ammo;
                case ItemDrop.ItemData.ItemType.Consumable: return ItemCategory.Consumable;
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Hands:
                case ItemDrop.ItemData.ItemType.Utility:
                case ItemDrop.ItemData.ItemType.Trinket:
                    return ItemCategory.Armor;
                case ItemDrop.ItemData.ItemType.Material: return ItemCategory.Material;
                case ItemDrop.ItemData.ItemType.Trophy: return ItemCategory.Trophy;
                default: return ItemCategory.Misc;
            }
        }
    }
}
