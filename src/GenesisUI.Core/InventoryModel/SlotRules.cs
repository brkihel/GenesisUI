namespace GenesisUI.InventoryModel
{
    /// <summary>What an item is, for GenesisUI's slot rules and sorting (mapped from the game's item type).</summary>
    public enum ItemCategory
    {
        Weapon,
        Shield,
        Tool,
        Ammo,
        Consumable,
        Armor,
        Material,
        Trophy,
        Misc,
    }

    /// <summary>
    /// Which items each kind of slot accepts (docs/GAMEPLAY.md §1): quick-use holds food, meads and
    /// potions; utility holds ammo, magic, shields, tools and weapons, never armour, capes or bags
    /// (they have their own equipment cells). Ordinary cells hold anything.
    /// </summary>
    public static class SlotRules
    {
        public static bool Accepts(SlotKind kind, ItemCategory item)
        {
            switch (kind)
            {
                case SlotKind.Ordinary: return true;
                case SlotKind.Quick: return item == ItemCategory.Consumable;
                case SlotKind.Utility:
                    return item == ItemCategory.Weapon || item == ItemCategory.Shield ||
                           item == ItemCategory.Tool || item == ItemCategory.Ammo;
                default: return false; // equipment cells follow EquipSlot, not categories
            }
        }

        /// <summary>
        /// A move of <paramref name="dragged"/> from a slot of kind <paramref name="source"/> onto a slot of
        /// kind <paramref name="target"/> that holds <paramref name="occupant"/> (null when empty). A swap sends
        /// the occupant back to the source slot, so it must be accepted there too.
        /// </summary>
        public static bool AllowsMove(SlotKind source, SlotKind target, ItemCategory dragged, ItemCategory? occupant)
        {
            if (!Accepts(target, dragged)) return false;
            return !occupant.HasValue || Accepts(source, occupant.Value);
        }
    }
}
