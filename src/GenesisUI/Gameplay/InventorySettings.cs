using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.InventoryModel;
using ServerSync;

namespace GenesisUI.Gameplay
{
    /// <summary>
    /// The admin's inventory settings (docs/GAMEPLAY.md §3.4). On a server that runs GenesisUI they
    /// come from the server through ServerSync (D-031) and clients cannot change them while the
    /// server locks the configuration; in single player and local worlds the player's own config
    /// is the admin's. Hotkeys are always the player's.
    /// </summary>
    internal static class InventorySettings
    {
        private static ConfigSync _sync;

        internal static ConfigEntry<bool> Lock;
        internal static ConfigEntry<int> Rows;
        internal static ConfigEntry<int> QuickSlots;
        internal static ConfigEntry<int> UtilitySlots;

        /// <summary>Raised when any synced value changes (locally or from the server).</summary>
        internal static event Action Changed;

        internal static void Bind(ConfigFile config)
        {
            _sync = new ConfigSync(PluginInfo.Guid)
            {
                DisplayName = PluginInfo.Name,
                CurrentVersion = PluginInfo.Version,
                // Clients with or without GenesisUI can join; the settings only reach those who have it.
                ModRequired = false,
            };

            Lock = config.Bind("Inventory", "LockConfiguration", true,
                "Com o GenesisUI no servidor, as opções de inventário marcadas [Servidor] vêm do servidor e o jogador não as muda. " +
                "Em jogo local ou sem o mod no servidor, valem as suas.");
            _sync.AddLockingConfigEntry(Lock);
            Rows = Synced(config.Bind("Inventory", "Rows", 4, new ConfigDescription(
                "[Servidor] Linhas do inventário (8 espaços cada, a primeira é a barra de itens): 4 = 32, 5 = 40, 6 = 48 espaços.",
                new AcceptableValueRange<int>(SlotLayout.MinRows, SlotLayout.MaxRows))));
            QuickSlots = Synced(config.Bind("Inventory", "QuickSlots", 4, new ConfigDescription(
                "[Servidor] Espaços de consumo rápido (comidas, hidromeles, poções): 0 desliga, até 4.",
                new AcceptableValueRange<int>(0, SlotLayout.MaxQuick))));
            UtilitySlots = Synced(config.Bind("Inventory", "UtilitySlots", 4, new ConfigDescription(
                "[Servidor] Espaços utilitários (munição, magias, escudos, ferramentas, armas; anéis e colares para troca rápida): 0 desliga, até 4.",
                new AcceptableValueRange<int>(0, SlotLayout.MaxUtility))));
        }

        /// <summary>
        /// True when these values come from a server that locks them and this player is not its admin:
        /// the settings window shows them read-only.
        /// </summary>
        internal static bool LockedHere => _sync != null && _sync.IsLocked && !_sync.IsSourceOfTruth && !_sync.IsAdmin;

        /// <summary>Whether an entry is one of the server-synced inventory settings.</summary>
        internal static bool IsSynced(ConfigEntryBase entry) => entry == Rows || entry == QuickSlots || entry == UtilitySlots;

        /// <summary>The layout the admin's settings describe right now.</summary>
        internal static SlotLayout Layout() =>
            new SlotLayout(Rows.Value, QuickSlots.Value, UtilitySlots.Value, EquipmentSlots());

        /// <summary>Vanilla's worn slots; modded ones join as each is enabled (GAMEPLAY §4, step F4.2d).</summary>
        internal static IEnumerable<EquipSlot> EquipmentSlots()
        {
            yield return EquipSlot.Head;
            yield return EquipSlot.Chest;
            yield return EquipSlot.Legs;
            yield return EquipSlot.Cape;
            yield return EquipSlot.Belt;
            yield return EquipSlot.Trinket;
        }

        private static ConfigEntry<T> Synced<T>(ConfigEntry<T> entry)
        {
            _sync.AddConfigEntry(entry);
            entry.SettingChanged += (_, __) => Changed?.Invoke();
            return entry;
        }
    }
}
