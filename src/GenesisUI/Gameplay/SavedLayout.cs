using System.Collections.Generic;
using System.Globalization;
using GenesisUI.InventoryModel;

namespace GenesisUI.Gameplay
{
    /// <summary>
    /// The layout a character was last saved with, kept in the character itself through vanilla's
    /// unique key/value store ("genesisui_layout" = "rows,quick,utility,equipmentMask"), so a change
    /// the admin made while the player was away is planned from the right starting point.
    /// </summary>
    internal static class SavedLayout
    {
        internal const string Key = "genesisui_layout";

        internal static SlotLayout Read(Player player)
        {
            if (!player.TryGetUniqueKeyValue(Key, out var value) || string.IsNullOrEmpty(value)) return null;
            var parts = value.Split(',');
            if (parts.Length != 4) return null;
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rows) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int quick) ||
                !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int utility) ||
                !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mask)) return null;
            var equipment = new List<EquipSlot>();
            foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
                if ((mask & (1 << (int)slot)) != 0) equipment.Add(slot);
            try { return new SlotLayout(rows, quick, utility, equipment); }
            catch (System.ArgumentException) { return null; }
        }

        internal static void Write(Player player, SlotLayout layout)
        {
            int mask = 0;
            foreach (var slot in layout.Equipment) mask |= 1 << (int)slot;
            player.AddUniqueKeyValue(Key, string.Join(",", new[]
            {
                layout.Rows.ToString(CultureInfo.InvariantCulture), layout.Quick.ToString(CultureInfo.InvariantCulture),
                layout.Utility.ToString(CultureInfo.InvariantCulture), mask.ToString(CultureInfo.InvariantCulture),
            }));
        }
    }
}
