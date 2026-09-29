using System.Collections.Generic;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Gameplay;
using GenesisUI.InventoryModel;
using UnityEngine;

namespace GenesisUI.Widgets
{
    /// <summary>One line of an item's stat table: a translation token, its value, and a bar (0..1) or -1.</summary>
    internal struct StatRow
    {
        public string Token;
        public string Value;
        public float Bar;
    }

    /// <summary>
    /// The stat table shared by the inventory's item details and the crafting window (ConceptArt 9
    /// and 12): weight, durability, quality, armour, damage, block, food and value, read from the
    /// game's own item getters at the given quality.
    /// </summary>
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetWeight")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetMaxDurability", Parameters = new[] { "System.Int32" })]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetArmor", Parameters = new[] { "System.Int32", "System.Single" })]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetDamage", Parameters = new[] { "System.Int32", "System.Single" })]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetBaseBlockPower", Parameters = new[] { "System.Int32" })]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_worldLevel")]
    [GameContract("assembly_valheim", "Game", "m_worldLevel")]
    internal static class ItemStats
    {
        /// <param name="crafting">A recipe's result (the prefab's data): durability is its maximum, no bar.</param>
        public static void Collect(ItemDrop.ItemData item, int quality, bool crafting, List<StatRow> rows)
        {
            rows.Clear();
            var s = item.m_shared;
            float world = crafting ? Game.m_worldLevel : item.m_worldLevel;
            Add(rows, "$genesisui_stat_weight", item.GetWeight(-1).ToString("0.0"));
            if (s.m_useDurability)
            {
                float max = item.GetMaxDurability(quality);
                if (crafting) Add(rows, "$genesisui_stat_durability", Mathf.CeilToInt(max).ToString());
                else Add(rows, "$genesisui_stat_durability", Mathf.CeilToInt(item.m_durability) + " / " + Mathf.CeilToInt(max),
                    max > 0f ? Mathf.Clamp01(item.m_durability / max) : 0f);
            }
            if (s.m_maxQuality > 1) Add(rows, "$genesisui_stat_quality", quality + " / " + s.m_maxQuality);
            float armor = item.GetArmor(quality, world);
            if (armor > 0f) Add(rows, "$genesisui_stat_armor", armor.ToString("0"));
            float damage = item.GetDamage(quality, world).GetTotalDamage();
            if (damage > 0f) Add(rows, "$genesisui_stat_damage", damage.ToString("0"));
            float block = item.GetBaseBlockPower(quality);
            if (block > 0f) Add(rows, "$genesisui_stat_block", block.ToString("0"));
            if (s.m_food > 0f) Add(rows, "$genesisui_stat_food_health", s.m_food.ToString("0"));
            if (s.m_foodStamina > 0f) Add(rows, "$genesisui_stat_food_stamina", s.m_foodStamina.ToString("0"));
            if (s.m_foodEitr > 0f) Add(rows, "$genesisui_stat_food_eitr", s.m_foodEitr.ToString("0"));
            if (s.m_foodBurnTime > 0f) Add(rows, "$genesisui_stat_duration", Mathf.RoundToInt(s.m_foodBurnTime / 60f) + " min");
            if (s.m_value > 0) Add(rows, "$genesisui_stat_value", s.m_value.ToString());
        }

        public static string TypeToken(ItemDrop.ItemData item)
        {
            switch (ItemCategories.Of(item))
            {
                case ItemCategory.Weapon: return "$genesisui_type_weapon";
                case ItemCategory.Shield: return "$genesisui_type_shield";
                case ItemCategory.Tool: return "$genesisui_type_tool";
                case ItemCategory.Ammo: return "$genesisui_type_ammo";
                case ItemCategory.Consumable: return "$genesisui_type_consumable";
                case ItemCategory.Armor: return "$genesisui_type_armor";
                case ItemCategory.Material: return "$genesisui_type_material";
                case ItemCategory.Trophy: return "$genesisui_type_trophy";
                default: return "$genesisui_type_misc";
            }
        }

        private static void Add(List<StatRow> rows, string token, string value, float bar = -1f) =>
            rows.Add(new StatRow { Token = token, Value = value, Bar = bar });
    }
}
