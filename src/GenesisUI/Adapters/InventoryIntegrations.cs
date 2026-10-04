using System;
using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.InventoryModel;

namespace GenesisUI.Adapters
{
    /// <summary>Internal F7 equipment/container capabilities, with owning-mod calls isolated by owner.</summary>
    internal static class InventoryIntegrations
    {
        internal sealed class Entry : IDisposable
        {
            internal string Owner;
            internal Func<ItemDrop.ItemData, EquipSlot?> Classify;
            internal Func<EquipSlot, bool> Enabled;
            internal Func<ItemDrop.ItemData, bool> CanOpen;
            internal Action<ItemDrop.ItemData> Open;
            internal Inventory Container;
            internal bool BelowInventory;
            private ItemDrop.ItemData _item;
            private EquipSlot _slot;
            private EquipSlot? _classified;
            private bool _allowed;
            private Action _classify, _enabled, _canOpen, _open;

            internal void Prepare()
            {
                _classify = () => _classified = Classify(_item);
                _enabled = () => _allowed = Enabled(_slot);
                _canOpen = () => _allowed = CanOpen != null && CanOpen(_item);
                _open = () => Open(_item);
            }
            internal EquipSlot? SlotFor(ItemDrop.ItemData item)
            {
                _item = item; _classified = null;
                return Guard.Run(Owner, _classify) ? _classified : null;
            }
            internal bool Has(EquipSlot slot)
            {
                _slot = slot; _allowed = false;
                return Guard.Run(Owner, _enabled) && _allowed;
            }
            internal bool CanUse(ItemDrop.ItemData item)
            {
                _item = item; _allowed = false;
                return Guard.Run(Owner, _canOpen) && _allowed;
            }
            internal bool Use(ItemDrop.ItemData item)
            {
                _item = item;
                return Guard.Run(Owner, _open);
            }
            public void Dispose() { Entries.Remove(this); Container = null; }
        }
        private static readonly List<Entry> Entries = new List<Entry>(3);
        internal static Entry Register(Entry entry) { entry.Prepare(); Entries.Add(entry); return entry; }
        internal static bool Enabled(EquipSlot slot)
        {
            for (int i = 0; i < Entries.Count; i++) if (Entries[i].Has(slot)) return true;
            return false;
        }
        internal static EquipSlot? SlotFor(ItemDrop.ItemData item)
        {
            if (item == null) return null;
            for (int i = 0; i < Entries.Count; i++)
            {
                var slot = Entries[i].SlotFor(item);
                if (slot.HasValue) return slot;
            }
            return null;
        }
        internal static Inventory Container(out bool below)
        {
            for (int i = 0; i < Entries.Count; i++)
                if (Entries[i].Container != null && !Guard.IsTripped(Entries[i].Owner))
                { below = Entries[i].BelowInventory; return Entries[i].Container; }
            below = false; return null;
        }
        internal static bool CanUse(ItemDrop.ItemData item)
        {
            for (int i = 0; i < Entries.Count; i++) if (Entries[i].CanUse(item)) return true;
            return false;
        }
        internal static bool Use(ItemDrop.ItemData item)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                if (entry.CanUse(item)) return entry.Use(item);
            }
            return false;
        }
    }
}
