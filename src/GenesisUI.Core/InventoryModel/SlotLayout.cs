using System;
using System.Collections.Generic;

namespace GenesisUI.InventoryModel
{
    /// <summary>Kinds of GenesisUI's own slots (docs/GAMEPLAY.md §1).</summary>
    public enum SlotKind
    {
        Ordinary,
        Quick,
        Utility,
        Equipment,
    }

    /// <summary>Worn equipment slots, in panel order. Modded ones only exist when enabled.</summary>
    public enum EquipSlot
    {
        Head,
        Chest,
        Legs,
        Cape,
        Belt,
        Trinket,
        BackpackQuiver,
        Lantern,
        Amulet,
        Ring,
        Wallet,
        KeyOne,
        KeyTwo,
    }

    /// <summary>
    /// Where every slot lives in the player's inventory (docs/GAMEPLAY.md §3.1): width 8, ordinary
    /// rows 0..N-1 (row 0 = hotbar), then one row with quick-use (x 0-3) and utility (x 4-7), then the
    /// equipment rows. Pure data: the admin's settings in, positions out.
    /// </summary>
    public sealed class SlotLayout
    {
        public const int Width = 8;
        public const int MinRows = 4;
        public const int MaxRows = 6;
        public const int MaxQuick = 4;
        public const int MaxUtility = 4;
        /// <summary>Vanilla's clamp in Player.SetInventorySize.</summary>
        public const int VanillaMaxRows = 9;

        private readonly EquipSlot[] _equipment;

        public int Rows { get; }
        public int Quick { get; }
        public int Utility { get; }
        public IReadOnlyList<EquipSlot> Equipment => _equipment;

        /// <summary>First row after the ordinary ones: quick-use and utility.</summary>
        public int SpecialRow => Rows;

        public int EquipmentRows => (_equipment.Length + Width - 1) / Width;

        /// <summary>The inventory height the game must hold: ordinary + quick/utility + equipment.</summary>
        public int TotalRows => Rows + 1 + EquipmentRows;

        public int OrdinarySlots => Rows * Width;

        public SlotLayout(int rows, int quick, int utility, IEnumerable<EquipSlot> equipment)
        {
            Rows = Clamp(rows, MinRows, MaxRows);
            Quick = Clamp(quick, 0, MaxQuick);
            Utility = Clamp(utility, 0, MaxUtility);
            var list = new List<EquipSlot>();
            foreach (var e in equipment ?? Array.Empty<EquipSlot>())
                if (!list.Contains(e)) list.Add(e);
            list.Sort();
            _equipment = list.ToArray();
            if (TotalRows > VanillaMaxRows)
                throw new ArgumentException("layout needs " + TotalRows + " rows; the game holds " + VanillaMaxRows);
        }

        public bool IsOrdinary(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Rows;

        /// <summary>The kind of the slot at (x, y), or null when that position is not a slot in this layout.</summary>
        public SlotKind? KindAt(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0) return null;
            if (y < Rows) return SlotKind.Ordinary;
            if (y == SpecialRow)
            {
                if (x < MaxQuick) return x < Quick ? SlotKind.Quick : (SlotKind?)null;
                int u = x - MaxQuick;
                return u < Utility ? SlotKind.Utility : (SlotKind?)null;
            }
            int index = (y - Rows - 1) * Width + x;
            return index >= 0 && index < _equipment.Length ? SlotKind.Equipment : (SlotKind?)null;
        }

        public (int X, int Y) QuickPosition(int index) => (index, SpecialRow);

        public (int X, int Y) UtilityPosition(int index) => (MaxQuick + index, SpecialRow);

        /// <summary>The cell of an equipment slot, or (-1, -1) when the slot is not in this layout.</summary>
        public (int X, int Y) EquipmentPosition(EquipSlot slot)
        {
            int i = Array.IndexOf(_equipment, slot);
            return i < 0 ? (-1, -1) : (i % Width, Rows + 1 + i / Width);
        }

        public EquipSlot? EquipmentAt(int x, int y)
        {
            if (y <= Rows || x < 0 || x >= Width) return null;
            int i = (y - Rows - 1) * Width + x;
            return i >= 0 && i < _equipment.Length ? _equipment[i] : (EquipSlot?)null;
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
