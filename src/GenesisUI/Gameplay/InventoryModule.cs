using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.InventoryModel;
using HarmonyLib;
using UnityEngine;

namespace GenesisUI.Gameplay
{
    /// <summary>
    /// GenesisUI's inventory slots (docs/GAMEPLAY.md, step F4.2a): keeps the player's inventory in
    /// the layout the admin set — ordinary rows, then quick-use/utility, then equipment — moving
    /// items when that layout changes, and keeps quick/utility rows out of the custom window until
    /// their panels arrive (F4.2c). Equipment cells appear in F4.2b. The placement patches read <see cref="Current"/>; while this
    /// module is not active it is null and they do nothing.
    /// </summary>
    [GameContract("assembly_valheim", "Player", "m_localPlayer")]
    [GameContract("assembly_valheim", "Player", "SetInventorySize")]
    [GameContract("assembly_valheim", "Player", "TryGetUniqueKeyValue")]
    [GameContract("assembly_valheim", "Player", "AddUniqueKeyValue")]
    [GameContract("assembly_valheim", "Humanoid", "GetInventory")]
    [GameContract("assembly_valheim", "Inventory", "GetAllItems")]
    [GameContract("assembly_valheim", "Inventory", "GetHeight")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_gridPos")]
    [GameContract("assembly_valheim", "InventoryGui", "m_playerGrid")]
    [GameContract("assembly_valheim", "InventoryGui", "SetInventorySize")]
    [GameContract("assembly_valheim", "InventoryGrid", "m_elements")]
    [GameContract("assembly_valheim", "InventoryElement", "get_Position")]
    internal sealed class InventoryModule : IUiModule
    {
        private static readonly string[] NoRegions = new string[0];

        /// <summary>The layout in force for the local player, or null (module off, faulted or no player).</summary>
        internal static SlotLayout Current { get; private set; }

        private Player _appliedTo;
        private SlotLayout _wanted;
        private bool _dirty = true;
        private string _lastRefusal;
        private readonly List<ItemAt> _items = new List<ItemAt>(64);
        private readonly VanillaSkin _specialSkin = new VanillaSkin("module:inv.slots");
        private InventoryElement _firstSpecialElement;
        private static InventoryModule _active;
        private static bool _equipmentPanelVisible;
        private AccessTools.FieldRef<InventoryGrid, List<InventoryElement>> _elements;

        public string Id => "inv.slots";
        public string NameToken => "$genesisui_module_inventory";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 10f;

        public void Build(ModuleContext context)
        {
            // Resolved here, after the host checked the contracts (AGENTS.md §2a).
            _elements = AccessTools.FieldRefAccess<InventoryGrid, List<InventoryElement>>("m_elements");
            InventorySettings.Changed += OnSettingsChanged;
            _appliedTo = null;
            _dirty = true;
            _active = this;
            _equipmentPanelVisible = false;
        }

        public void Refresh(float deltaSeconds)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                Current = null;
                _appliedTo = null;
                return;
            }
            if (_dirty || _appliedTo != player) Apply(player);
            if (Current != null) HideSpecialRows();
        }

        public void Teardown()
        {
            InventorySettings.Changed -= OnSettingsChanged;
            Current = null;
            _appliedTo = null;
            _equipmentPanelVisible = false;
            _active = null;
            _specialSkin.Restore();
            _firstSpecialElement = null;
        }

        private void OnSettingsChanged() => _dirty = true;

        /// <summary>
        /// Brings the player's inventory into the admin's layout. Plans first (LayoutChange): if the
        /// items do not fit the new layout, nothing moves, the previous layout stays and the player
        /// is told once.
        /// </summary>
        private void Apply(Player player)
        {
            _dirty = false;
            _wanted = InventorySettings.Layout();
            var inventory = player.GetInventory();
            // A character that never ran GenesisUI (or ran it with other settings) is planned from what
            // it saved; one with no saved layout is plain vanilla: 4 ordinary rows, nothing special.
            var from = _appliedTo == player && Current != null
                ? Current
                : SavedLayout.Read(player) ?? new SlotLayout(SlotLayout.MinRows, 0, 0, null);

            var all = inventory.GetAllItems();
            _items.Clear();
            for (int i = 0; i < all.Count; i++) _items.Add(new ItemAt(i, all[i].m_gridPos.x, all[i].m_gridPos.y));
            var plan = LayoutChange.Compute(from, _wanted, _items);

            SlotLayout layout;
            if (plan.Ok)
            {
                foreach (var move in plan.Moves) all[move.Id].m_gridPos = new Vector2i(move.X, move.Y);
                layout = _wanted;
                if (plan.Moves.Count > 0)
                    GenesisLog.Info("Module:inv.slots", "layout " + Describe(from) + " -> " + Describe(layout) + ": moved " + plan.Moves.Count +
                        " item(s), " + plan.Displaced + " to the inventory; items " + all.Count + " before and after");
                _lastRefusal = null;
            }
            else
            {
                // Keep what the character has; say why, once per refusal.
                layout = from;
                string why = "layout " + Describe(_wanted) + " refused: " + plan.Overflow + " item(s) would not fit; keeping " + Describe(from);
                if (why != _lastRefusal)
                {
                    _lastRefusal = why;
                    GenesisLog.Warn("Module:inv.slots", why);
                    player.Message(MessageHud.MessageType.Center,
                        Localize("$genesisui_inventory_refused").Replace("{0}", plan.Overflow.ToString()));
                }
            }

            Current = layout;
            _appliedTo = player;
            // Vanilla's own resize: the prefix raises the rows to hold the special rows too, so
            // vanilla stores that height in the character and never drops a special item.
            player.SetInventorySize(layout.Rows);
            SavedLayout.Write(player, layout);
            EquipmentRules.ReconcileExisting(player);
            _specialSkin.Restore();
            _firstSpecialElement = null;
        }

        /// <summary>The custom equipment panel shows the real special-row cells. If it is not
        /// available, vanilla shows every row so no stored item becomes inaccessible.</summary>
        internal static void SetEquipmentPanelVisible(bool visible)
        {
            if (_equipmentPanelVisible == visible) return;
            _equipmentPanelVisible = visible;
            if (_active != null && Current != null) _active.HideSpecialRows();
        }

        /// <summary>
        /// The custom window shows equipment cells and hides quick/utility cells until F4.2c.
        /// When the custom window is off or faulted, show the whole vanilla grid as a safe fallback.
        /// </summary>
        private void HideSpecialRows()
        {
            var gui = InventoryGui.instance;
            if (gui == null || !InventoryGui.IsVisible() || gui.m_playerGrid == null) return;
            var elements = _elements(gui.m_playerGrid);
            if (elements == null) return;
            var first = elements.Count > 0 ? elements[0] : null;
            if (first != _firstSpecialElement)
            {
                _specialSkin.Restore();
                _firstSpecialElement = first;
            }
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null || element.Position.y < Current.Rows) continue;
                var pos = element.Position;
                bool show = !_equipmentPanelVisible || Current.EquipmentAt(pos.x, pos.y).HasValue;
                var group = _specialSkin.Group(element.gameObject);
                if (group.alpha != (show ? 1f : 0f)) group.alpha = show ? 1f : 0f;
                if (group.blocksRaycasts != show) group.blocksRaycasts = show;
                if (group.interactable != show) group.interactable = show;
            }
        }

        private static string Describe(SlotLayout l) => l.Rows + " rows, " + l.Quick + " quick, " + l.Utility + " utility, " + l.Equipment.Count + " worn";

        private static string Localize(string token) => Localization.instance != null ? Localization.instance.Localize(token) : token;
    }
}
