using System;
using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Gameplay;
using GenesisUI.Host;
using GenesisUI.InventoryModel;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GenesisUI.Modules.Windows
{
    /// <summary>
    /// The Inventory tab, ConceptArt (9) (D-032): GenesisUI's own window — inventory grid with
    /// quick-use and action slots, equipment, item details, and the chest panel — drawn from the
    /// player's inventory, laid out on the concept's design board (<see cref="WindowCanvas.Design"/>).
    /// Vanilla's inventory panels stay alive but hidden; they are the engine: every click, drag, split,
    /// equip and transfer is handed to the vanilla grid's own callbacks, exactly as a click on a vanilla
    /// slot would, so item operations (and other mods' patches on them) stay vanilla's.
    /// </summary>
    [GameContract("assembly_valheim", "InventoryGui", "m_player")]
    [GameContract("assembly_valheim", "InventoryGui", "m_crafting")]
    [GameContract("assembly_valheim", "InventoryGui", "m_info")]
    [GameContract("assembly_valheim", "InventoryGui", "m_container")]
    [GameContract("assembly_valheim", "InventoryGui", "m_playerGrid")]
    [GameContract("assembly_valheim", "InventoryGui", "m_containerGrid")]
    [GameContract("assembly_valheim", "InventoryGui", "m_currentContainer")]
    [GameContract("assembly_valheim", "InventoryGui", "m_dragItem")]
    [GameContract("assembly_valheim", "InventoryGui", "m_dragAmount")]
    [GameContract("assembly_valheim", "InventoryGui", "m_dragGo")]
    [GameContract("assembly_valheim", "InventoryGui", "m_dropButton")]
    [GameContract("assembly_valheim", "InventoryGui", "m_takeAllButton")]
    [GameContract("assembly_valheim", "InventoryGui", "m_stackAllButton")]
    [GameContract("assembly_valheim", "InventoryGui", "m_splitDialog")]
    [GameContract("assembly_valheim", "InventoryGui", "IsContainerOpen")]
    [GameContract("assembly_valheim", "SplitDialog", "get_IsActive")]
    [GameContract("assembly_valheim", "InventoryGrid", "m_onSelected")]
    [GameContract("assembly_valheim", "InventoryGrid", "m_onRightClick")]
    [GameContract("assembly_valheim", "InventoryGrid", "m_onReleased")]
    [GameContract("assembly_valheim", "InventoryGrid", "GetInventory")]
    [GameContract("assembly_valheim", "Container", "GetInventory")]
    [GameContract("assembly_valheim", "Inventory", "GetItemAt")]
    [GameContract("assembly_valheim", "Inventory", "GetWidth")]
    [GameContract("assembly_valheim", "Inventory", "GetHeight")]
    [GameContract("assembly_valheim", "Inventory", "GetName")]
    [GameContract("assembly_valheim", "Inventory", "GetAllItems")]
    [GameContract("assembly_valheim", "Inventory", "GetTotalWeight")]
    [GameContract("assembly_valheim", "Inventory", "m_onChanged")]
    [GameContract("assembly_valheim", "Humanoid", "IsItemEquiped")]
    [GameContract("assembly_valheim", "Player", "GetMaxCarryWeight")]
    [GameContract("assembly_valheim", "Player", "GetBodyArmor")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetIcon")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetWeight")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetMaxDurability", Parameters = new string[0])]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetArmor", Parameters = new string[0])]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetDamage", Parameters = new string[0])]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetBaseBlockPower", Parameters = new string[0])]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_durability")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_quality")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_stack")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_itemType")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_maxStackSize")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_maxQuality")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_useDurability")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_description")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_food")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_foodStamina")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_foodEitr")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_foodBurnTime")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_value")]
    [GameContract("assembly_valheim", "HitData+DamageTypes", "GetTotalDamage")]
    [GameContract("assembly_utils", "ZInput", "get_pointerPosition")]
    internal sealed class InventoryWindowModule : IUiModule
    {
        private const string Owner = "module:win.inventory";
        private static readonly string[] NoRegions = new string[0];
        private const float CloseHoldSeconds = 0.6f;

        // ConceptArt (9), measured in the design board's units (WindowCanvas.Design).
        private const float PanelsTop = 102f, PanelsHeight = 673f;
        private const float InventoryX = 0f, InventoryW = 816f;
        private const float EquipmentX = 820f, EquipmentW = 412f;
        private const float DetailsX = 1236f, DetailsW = 344f;
        private const float GridX = 44f, GridY = 96f, Cell = 82f, GapX = 7f, GapY = 10f;
        private const int VisibleRows = 4;
        private const float SmallCell = 74f, QuickX = 54f, QuickPitch = 85f, UtilityX = 445f, UtilityPitch = 82f, SpecialY = 512f;
        private const float WornCell = 80f;
        private const float DimAlpha = 0.22f;

        internal enum Filter { All, Weapons, Armor, Tools, Consumables, Materials, Ammo, Misc }

        private static readonly string[] FilterTokens =
        {
            "$genesisui_filter_all", "$genesisui_filter_weapons", "$genesisui_filter_armor", "$genesisui_filter_tools",
            "$genesisui_filter_consumables", "$genesisui_filter_materials", "$genesisui_filter_ammo", "$genesisui_filter_misc",
        };

        private static readonly string[] UtilityKeys = { "Z", "X", "C", "V" };

        // Left column like the concept (head, chest, cape, legs), then the right column.
        private static readonly EquipSlot[] WornOrder =
        {
            EquipSlot.Head, EquipSlot.Chest, EquipSlot.Cape, EquipSlot.Legs,
            EquipSlot.Trinket, EquipSlot.Belt, EquipSlot.Amulet, EquipSlot.Ring, EquipSlot.Lantern, EquipSlot.BackpackQuiver,
        };

        private readonly VanillaSkin _skin = new VanillaSkin(Owner);
        private readonly List<ItemCell> _gridCells = new List<ItemCell>(SlotLayout.Width * VisibleRows);
        private readonly List<ItemCell> _quickCells = new List<ItemCell>(SlotLayout.MaxQuick);
        private readonly List<ItemCell> _utilityCells = new List<ItemCell>(SlotLayout.MaxUtility);
        private readonly Dictionary<EquipSlot, ItemCell> _wornCells = new Dictionary<EquipSlot, ItemCell>();
        private readonly List<ItemCell> _containerCells = new List<ItemCell>(32);
        private readonly List<GameObject> _wornLabels = new List<GameObject>();
        private readonly List<InventorySort.Entry> _sortEntries = new List<InventorySort.Entry>(64);

        private ThemeRuntime _theme;
        private readonly BepInEx.Configuration.ConfigEntry<BepInEx.Configuration.KeyboardShortcut> _sortKey;
        private RectTransform _root;
        private CanvasGroup _fade;
        private RectTransform _area;
        private RectTransform _inventoryPanel, _equipmentPanel, _detailsPanel, _containerPanel;
        private RectTransform _filterList;
        private RectTransform _scrollTrack, _scrollThumb;
        private RectTransform _containerGrid;
        private GameObject _quickTitle, _utilityTitle, _specialDivider;
        private TextMeshProUGUI _slotsText, _weightText, _armorText, _filterText, _containerTitle;
        private Image _weightFill;
        private Image _ghostIcon;
        private TextMeshProUGUI _ghostAmount;
        private RectTransform _ghost;
        private Details _details;

        private Filter _filter = Filter.All;
        private bool _applied;
        private float _closedFor;
        private int _scroll;
        private int _builtContainerW = -1, _builtContainerH = -1, _containerScroll;
        private int _containerVisibleRows;
        private bool _containerShown;
        private ItemCell _hovered;
        private ItemDrop.ItemData _shownItem;
        private bool _droppedOnCell;
        private string _layoutKey;
        private int _shownSlots = -1, _shownOrdinary = -1, _shownWeight = -1, _shownMax = -1, _shownArmor = -1;
        private GameObject _hiddenDragGo;
        private Transform _guiAncestor;
        private bool _behind;

        private AccessTools.FieldRef<InventoryGui, InventoryGrid> _containerGridRef;
        private AccessTools.FieldRef<InventoryGui, Container> _containerRef;
        private AccessTools.FieldRef<InventoryGui, ItemDrop.ItemData> _dragItem;
        private AccessTools.FieldRef<InventoryGui, int> _dragAmount;
        private AccessTools.FieldRef<InventoryGui, GameObject> _dragGo;

        public InventoryWindowModule(BepInEx.Configuration.ConfigFile config)
        {
            _sortKey = config.Bind("Windows", "SortKey", new BepInEx.Configuration.KeyboardShortcut(KeyCode.R),
                "Tecla que organiza o inventário (fora da hotbar, do consumo rápido, dos slots de ação e do equipamento).");
        }

        public string Id => "win.inventory";
        public string NameToken => "$genesisui_module_inventory_window";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f;

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _containerGridRef = AccessTools.FieldRefAccess<InventoryGui, InventoryGrid>("m_containerGrid");
            _containerRef = AccessTools.FieldRefAccess<InventoryGui, Container>("m_currentContainer");
            _dragItem = AccessTools.FieldRefAccess<InventoryGui, ItemDrop.ItemData>("m_dragItem");
            _dragAmount = AccessTools.FieldRefAccess<InventoryGui, int>("m_dragAmount");
            _dragGo = AccessTools.FieldRefAccess<InventoryGui, GameObject>("m_dragGo");
            _applied = false;
        }

        public void Refresh(float deltaSeconds)
        {
            var gui = InventoryGui.instance;
            var player = Player.m_localPlayer;
            bool on = gui != null && player != null && WindowShellModule.Showing && WindowShellModule.ActiveTab == WindowShellModule.Tab.Inventory;
            if (!on)
            {
                if (_applied)
                {
                    // Keep vanilla hidden while our window fades out with vanilla's close animation.
                    if (gui == null || player == null || InventoryGui.IsVisible() || WindowShellModule.Showing)
                        Unapply();
                    else
                    {
                        _closedFor += deltaSeconds;
                        SetFade(WindowShellModule.Opacity, false);
                        if (_closedFor >= CloseHoldSeconds && WindowShellModule.Opacity <= 0.001f) Unapply();
                    }
                }
                return;
            }
            _closedFor = 0f;
            if (!EnsureBuilt(gui)) return;
            if (!_applied) Apply(gui);
            WindowCanvas.Fit(_area);
            SetFade(WindowShellModule.Opacity, true);

            var layout = InventoryModule.Current ?? new SlotLayout(SlotLayout.MinRows, 0, 0, null);
            ApplyLayout(layout);
            HideVanillaDrag(gui);
            FollowSplitDialog(gui);

            var inventory = player.GetInventory();
            var dragged = _dragItem(gui);
            bool container = UpdateContainer(gui, player, dragged);
            for (int i = 0; i < _gridCells.Count; i++) UpdateCell(_gridCells[i], inventory, player, dragged);
            for (int i = 0; i < _quickCells.Count; i++) UpdateCell(_quickCells[i], inventory, player, dragged);
            for (int i = 0; i < _utilityCells.Count; i++) UpdateCell(_utilityCells[i], inventory, player, dragged);
            if (!container)
                foreach (var cell in _wornCells.Values) UpdateCell(cell, inventory, player, dragged);
            UpdateGhost(gui, dragged);
            if (!container) UpdateDetails(inventory);
            UpdateStats(player, inventory, layout);

            if (_sortKey.Value.IsDown()) Sort(gui, player, layout);
        }

        public void Teardown()
        {
            if (_applied) Unapply();
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            _root = null;
            _gridCells.Clear();
            _quickCells.Clear();
            _utilityCells.Clear();
            _wornCells.Clear();
            _containerCells.Clear();
            _wornLabels.Clear();
        }

        // ------------------------------------------------------------------ vanilla engine

        private bool EnsureBuilt(InventoryGui gui)
        {
            if (_root != null) return true;
            _root = WindowCanvas.CreateRoot(gui, "GenesisUI.InventoryWindow", behind: false);
            if (_root == null) return false;
            _fade = _root.gameObject.AddComponent<CanvasGroup>();
            _fade.alpha = 0f;
            _area = WindowCanvas.Area(_root, "Board");
            Build();
            _root.gameObject.SetActive(false);
            return true;
        }

        private void Apply(InventoryGui gui)
        {
            _applied = true;
            _root.gameObject.SetActive(true);
            // Vanilla's panels keep running (their grids are the engine) but draw nothing and take no clicks.
            Hide(gui.m_player.gameObject);
            Hide(gui.m_container.gameObject);
            Hide(gui.m_crafting.gameObject);
            Hide(gui.m_info.gameObject);
            // Vanilla's split dialog stays vanilla's and visible even if it lives under a hidden panel;
            // while it is open our window steps behind InventoryGui (FollowSplitDialog).
            if (gui.m_splitDialog != null)
            {
                var group = _skin.Group(gui.m_splitDialog.gameObject);
                group.alpha = 1f;
                group.blocksRaycasts = true;
                group.interactable = true;
                group.ignoreParentGroups = true;
            }
            _guiAncestor = gui.transform;
            while (_guiAncestor.parent != null && _guiAncestor.parent != _root.parent) _guiAncestor = _guiAncestor.parent;
            _behind = true; // forces the first placement in front
            InventoryModule.SetEquipmentPanelVisible(true);
            FollowSplitDialog(gui);
            GenesisLog.Info("Module:win.inventory", "window shown; vanilla panels hidden (" + _skin.Count + " change(s))");
        }

        private void Hide(GameObject go)
        {
            var g = _skin.Group(go);
            g.alpha = 0f;
            g.blocksRaycasts = false;
            g.interactable = false;
        }

        private void Unapply()
        {
            _applied = false;
            _closedFor = 0f;
            InventoryModule.SetEquipmentPanelVisible(false);
            ShowVanillaDrag();
            _skin.Restore();
            _hovered = null;
            _shownItem = null;
            _containerShown = false;
            _droppedOnCell = false;
            if (_filterList != null) _filterList.gameObject.SetActive(false);
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void SetFade(float alpha, bool interactive)
        {
            if (_fade == null) return;
            if (!Mathf.Approximately(_fade.alpha, alpha)) _fade.alpha = alpha;
            _fade.blocksRaycasts = interactive;
            _fade.interactable = interactive;
        }

        /// <summary>
        /// Our window draws in front of InventoryGui; while vanilla's split dialog is open it steps just
        /// behind it, so the dialog (vanilla's, modal) draws and clicks above our panels.
        /// </summary>
        private void FollowSplitDialog(InventoryGui gui)
        {
            bool split = gui.m_splitDialog != null && gui.m_splitDialog.IsActive;
            if (split == _behind || _guiAncestor == null || _guiAncestor.parent != _root.parent) return;
            _behind = split;
            int index = _guiAncestor.GetSiblingIndex();
            int mine = _root.GetSiblingIndex();
            if (split) _root.SetSiblingIndex(mine < index ? index - 1 : index);
            else _root.SetSiblingIndex(mine < index ? index : index + 1);
        }

        /// <summary>Vanilla's drag icon is replaced by ours; its object is vanilla's and dies on drop.</summary>
        private void HideVanillaDrag(InventoryGui gui)
        {
            var go = _dragGo(gui);
            if (go == null || go == _hiddenDragGo) return;
            _hiddenDragGo = go;
            var group = go.GetComponent<CanvasGroup>();
            if (group == null) group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        private void ShowVanillaDrag()
        {
            if (_hiddenDragGo != null)
            {
                var group = _hiddenDragGo.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = 1f;
            }
            _hiddenDragGo = null;
        }

        private InventoryGrid GridOf(InventoryGui gui, ItemCell cell) => cell.Container ? _containerGridRef(gui) : gui.m_playerGrid;

        private Inventory InventoryOf(InventoryGui gui, ItemCell cell)
        {
            if (!cell.Container) return Player.m_localPlayer != null ? Player.m_localPlayer.GetInventory() : null;
            var container = _containerRef(gui);
            return container != null ? container.GetInventory() : null;
        }

        // ------------------------------------------------------------------ input (called by CellInput)

        /// <summary>A press on a cell: the same call a press on vanilla's slot makes (InventoryGrid.OnLeftDown/OnRightDown).</summary>
        internal void OnDown(ItemCell cell, PointerEventData e)
        {
            var gui = InventoryGui.instance;
            if (gui == null || !_applied || !cell.Active) return;
            var grid = GridOf(gui, cell);
            var inventory = InventoryOf(gui, cell);
            if (grid == null || inventory == null) return;
            var item = inventory.GetItemAt(cell.Pos.x, cell.Pos.y);
            if (e.button == PointerEventData.InputButton.Right)
            {
                grid.m_onRightClick?.Invoke(grid, item, cell.Pos);
                return;
            }
            if (e.button != PointerEventData.InputButton.Left) return;
            var input = BepInEx.UnityInput.Current;
            var mod = InventoryGrid.Modifier.Select;
            if (input.GetKey(KeyCode.LeftShift) || input.GetKey(KeyCode.RightShift)) mod = InventoryGrid.Modifier.Split;
            else if (input.GetKey(KeyCode.LeftControl) || input.GetKey(KeyCode.RightControl)) mod = InventoryGrid.Modifier.Move;
            grid.m_onSelected?.Invoke(grid, item, cell.Pos, mod);
            if (_filterList != null) _filterList.gameObject.SetActive(false);
        }

        /// <summary>A drag released on a cell: vanilla's OnReleasedItem, as vanilla's own drag handler calls it.</summary>
        internal void OnDrop(ItemCell cell)
        {
            var gui = InventoryGui.instance;
            if (gui == null || !_applied || !cell.Active || _dragItem(gui) == null) return;
            _droppedOnCell = true;
            var grid = GridOf(gui, cell);
            var inventory = InventoryOf(gui, cell);
            if (grid == null || inventory == null) return;
            grid.m_onReleased?.Invoke(grid, inventory.GetItemAt(cell.Pos.x, cell.Pos.y), cell.Pos);
        }

        /// <summary>A drag released on nothing (the world): vanilla's drop-outside button, which drops the item.</summary>
        internal void OnEndDrag(PointerEventData e)
        {
            var gui = InventoryGui.instance;
            bool onCell = _droppedOnCell;
            _droppedOnCell = false;
            if (gui == null || !_applied || onCell || _dragItem(gui) == null || gui.m_dropButton == null) return;
            var under = e.pointerCurrentRaycast.gameObject;
            if (under == null || under.transform.IsChildOf(gui.m_dropButton.transform))
                gui.m_dropButton.onClick.Invoke();
        }

        internal void OnEnter(ItemCell cell) => _hovered = cell;

        internal void OnExit(ItemCell cell)
        {
            if (_hovered == cell) _hovered = null;
        }

        internal void OnScroll(ItemCell cell, PointerEventData e)
        {
            int step = e.scrollDelta.y > 0f ? -1 : e.scrollDelta.y < 0f ? 1 : 0;
            if (step == 0) return;
            if (cell != null && cell.Container) _containerScroll += step;
            else _scroll += step;
        }

        // ------------------------------------------------------------------ per frame

        /// <summary>Binds cells to inventory positions for the admin's layout; rebuilt only when it changes.</summary>
        private void ApplyLayout(SlotLayout layout)
        {
            int maxScroll = Mathf.Max(0, layout.Rows - VisibleRows);
            _scroll = Mathf.Clamp(_scroll, 0, maxScroll);
            for (int i = 0; i < _gridCells.Count; i++)
            {
                var cell = _gridCells[i];
                int x = i % SlotLayout.Width, y = i / SlotLayout.Width + _scroll;
                cell.Bind(new Vector2i(x, y), y < layout.Rows, y == 0 ? (x + 1).ToString() : null);
            }
            UpdateScrollbar(layout.Rows);

            string key = layout.Rows + "/" + layout.Quick + "/" + layout.Utility + "/" + layout.Equipment.Count;
            if (key == _layoutKey) return;
            _layoutKey = key;
            for (int i = 0; i < _quickCells.Count; i++)
            {
                var p = layout.QuickPosition(i);
                _quickCells[i].Bind(new Vector2i(p.X, p.Y), i < layout.Quick, null);
                _quickCells[i].Root.gameObject.SetActive(i < layout.Quick);
            }
            for (int i = 0; i < _utilityCells.Count; i++)
            {
                var p = layout.UtilityPosition(i);
                _utilityCells[i].Bind(new Vector2i(p.X, p.Y), i < layout.Utility, null);
                _utilityCells[i].Root.gameObject.SetActive(i < layout.Utility);
            }
            _quickTitle.SetActive(layout.Quick > 0);
            _utilityTitle.SetActive(layout.Utility > 0);
            _specialDivider.SetActive(layout.Quick > 0 && layout.Utility > 0);

            // Worn cells: the layout's slots, left column first (4), then the right column.
            int placed = 0;
            foreach (var label in _wornLabels) label.SetActive(false);
            foreach (var slot in WornOrder)
            {
                if (!_wornCells.TryGetValue(slot, out var cell)) continue;
                var p = layout.EquipmentPosition(slot);
                bool present = p.X >= 0 && placed < 8;
                cell.Root.gameObject.SetActive(present);
                if (!present) continue;
                cell.Bind(new Vector2i(p.X, p.Y), true, null);
                PlaceWorn(cell, placed);
                placed++;
            }
            GenesisLog.Info("Module:win.inventory", "layout bound: " + key);
        }

        private void PlaceWorn(ItemCell cell, int index)
        {
            int column = index < 4 ? 0 : 1, row = index % 4;
            float x = column == 0 ? 24f : EquipmentW - 24f - WornCell;
            float y = 96f + row * 120f;
            cell.Root.anchoredPosition = new Vector2(x, -y);
            if (cell.Label != null)
            {
                var rt = (RectTransform)cell.Label.transform;
                rt.anchoredPosition = new Vector2(x - 20f, -(y + WornCell + 4f));
                cell.Label.gameObject.SetActive(true);
            }
        }

        private void UpdateScrollbar(int rows)
        {
            bool show = rows > VisibleRows;
            if (_scrollTrack.gameObject.activeSelf != show) _scrollTrack.gameObject.SetActive(show);
            if (!show) return;
            float track = _scrollTrack.rect.height;
            float thumb = track * VisibleRows / rows;
            float y = (track - thumb) * _scroll / Mathf.Max(1, rows - VisibleRows);
            _scrollThumb.sizeDelta = new Vector2(_scrollThumb.sizeDelta.x, thumb);
            _scrollThumb.anchoredPosition = new Vector2(0f, -y);
        }

        private void UpdateCell(ItemCell cell, Inventory inventory, Player player, ItemDrop.ItemData dragged)
        {
            var item = cell.Active && inventory != null ? inventory.GetItemAt(cell.Pos.x, cell.Pos.y) : null;
            bool dim = item != null && (item == dragged || (_filter != Filter.All && !Matches(item, _filter)));
            cell.Show(item, player != null && item != null && player.IsItemEquiped(item), cell == _hovered, dim ? DimAlpha : 1f);
        }

        private void UpdateGhost(InventoryGui gui, ItemDrop.ItemData dragged)
        {
            bool show = dragged != null;
            if (_ghost.gameObject.activeSelf != show) _ghost.gameObject.SetActive(show);
            if (!show) return;
            var icon = dragged.GetIcon();
            if (_ghostIcon.sprite != icon) _ghostIcon.sprite = icon;
            int amount = _dragAmount(gui);
            if (amount > 1) _ghostAmount.SetText("{0}", amount);
            else if (_ghostAmount.text.Length > 0) _ghostAmount.text = "";
            _ghost.position = ZInput.pointerPosition;
        }

        /// <summary>The chest panel over equipment and details (ConceptArt 9 keeps the inventory on the left).</summary>
        private bool UpdateContainer(InventoryGui gui, Player player, ItemDrop.ItemData dragged)
        {
            var container = _containerRef(gui);
            bool open = gui.IsContainerOpen() && container != null;
            if (open != _containerShown)
            {
                _containerShown = open;
                _equipmentPanel.gameObject.SetActive(!open);
                _detailsPanel.gameObject.SetActive(!open);
                _containerPanel.gameObject.SetActive(open);
                _containerScroll = 0;
            }
            if (!open) return false;
            var inventory = container.GetInventory();
            if (inventory == null) return true;
            int w = inventory.GetWidth(), h = inventory.GetHeight();
            if (w != _builtContainerW || h != _builtContainerH) BuildContainerCells(w, h);
            _containerScroll = Mathf.Clamp(_containerScroll, 0, Mathf.Max(0, h - _containerVisibleRows));
            for (int i = 0; i < _containerCells.Count; i++)
            {
                var cell = _containerCells[i];
                int x = i % w, y = i / w + _containerScroll;
                cell.Bind(new Vector2i(x, y), y < h, null);
                UpdateCell(cell, inventory, null, dragged);
            }
            string name = Localize(inventory.GetName());
            if (_containerTitle.text != name) _containerTitle.text = name.ToUpperInvariant();
            return true;
        }

        private void UpdateDetails(Inventory inventory)
        {
            var item = _hovered != null && _hovered.Active ? InventoryOf(InventoryGui.instance, _hovered)?.GetItemAt(_hovered.Pos.x, _hovered.Pos.y) : null;
            if (item == null || item == _shownItem) return; // the last item stays, like a selection
            _shownItem = item;
            _details.Show(item);
        }

        private void UpdateStats(Player player, Inventory inventory, SlotLayout layout)
        {
            int used = 0;
            foreach (var item in inventory.GetAllItems())
                if (layout.IsOrdinary(item.m_gridPos.x, item.m_gridPos.y)) used++;
            if (used != _shownSlots || layout.OrdinarySlots != _shownOrdinary)
            {
                _shownSlots = used;
                _shownOrdinary = layout.OrdinarySlots;
                _slotsText.text = Localize("$genesisui_slots_in_use").Replace("{0}", used.ToString()).Replace("{1}", layout.OrdinarySlots.ToString()).ToUpperInvariant();
            }

            int weight = Mathf.CeilToInt(inventory.GetTotalWeight());
            int max = Mathf.CeilToInt(player.GetMaxCarryWeight());
            if (weight != _shownWeight || max != _shownMax)
            {
                _shownWeight = weight;
                _shownMax = max;
                _weightText.SetText("{0} / {1}", weight, max);
                _weightFill.fillAmount = max > 0 ? Mathf.Clamp01((float)weight / max) : 0f;
                _weightFill.color = weight > max ? ThemeRuntime.ToUnity(_theme.Tokens.StateDanger) : ThemeRuntime.ToUnity(_theme.Tokens.AccentGold);
            }

            int armor = Mathf.RoundToInt(player.GetBodyArmor());
            if (armor != _shownArmor)
            {
                _shownArmor = armor;
                _armorText.SetText("{0}", armor);
            }
        }

        // ------------------------------------------------------------------ sort and filter

        /// <summary>Organizar: positions only, below the hotbar, planned in Core (InventorySort).</summary>
        private void Sort(InventoryGui gui, Player player, SlotLayout layout)
        {
            if (_dragItem(gui) != null) return; // never while an item is held
            var inventory = player.GetInventory();
            var all = inventory.GetAllItems();
            _sortEntries.Clear();
            for (int i = 0; i < all.Count; i++)
                _sortEntries.Add(new InventorySort.Entry
                {
                    Id = i, X = all[i].m_gridPos.x, Y = all[i].m_gridPos.y,
                    Category = ItemCategories.Of(all[i]), Name = Localize(all[i].m_shared.m_name), Stack = all[i].m_stack,
                });
            var moves = InventorySort.Plan(layout, _sortEntries);
            foreach (var move in moves) all[move.Id].m_gridPos = new Vector2i(move.X, move.Y);
            if (moves.Count > 0) inventory.m_onChanged?.Invoke();
            GenesisLog.Info("Module:win.inventory", "sorted: moved " + moves.Count + " item(s); items " + all.Count + " before and after");
        }

        internal static bool Matches(ItemDrop.ItemData item, Filter filter)
        {
            var c = ItemCategories.Of(item);
            switch (filter)
            {
                case Filter.Weapons: return c == ItemCategory.Weapon;
                case Filter.Armor: return c == ItemCategory.Armor || c == ItemCategory.Shield;
                case Filter.Tools: return c == ItemCategory.Tool;
                case Filter.Consumables: return c == ItemCategory.Consumable;
                case Filter.Materials: return c == ItemCategory.Material;
                case Filter.Ammo: return c == ItemCategory.Ammo;
                case Filter.Misc: return c == ItemCategory.Trophy || c == ItemCategory.Misc;
                default: return true;
            }
        }

        // ------------------------------------------------------------------ building

        private void Build()
        {
            var t = _theme.Tokens;
            var panels = WindowCanvas.At(_area, "Panels", 0f, PanelsTop, WindowCanvas.Design.x, PanelsHeight);
            _inventoryPanel = Panel(panels, "Inventory", InventoryX, InventoryW, "$genesisui_panel_inventory", 88f, 26f, TextAlignmentOptions.Left);
            _equipmentPanel = Panel(panels, "Equipment", EquipmentX, EquipmentW, "$genesisui_panel_equipment", 54f, 23f, TextAlignmentOptions.Left);
            _detailsPanel = Panel(panels, "Details", DetailsX, DetailsW, "$genesisui_panel_details", 0f, 19f, TextAlignmentOptions.Center);
            _containerPanel = Panel(panels, "Container", EquipmentX, WindowCanvas.Design.x - EquipmentX, "$genesisui_panel_container", 54f, 23f, TextAlignmentOptions.Left);
            _containerTitle = _containerPanel.Find("Title").GetComponent<TextMeshProUGUI>();
            _containerPanel.gameObject.SetActive(false);

            BuildInventory(t);
            BuildEquipment(t);
            _details = new Details(this, _detailsPanel);
            BuildContainer(t);

            // The held item follows the pointer above everything in the window.
            _ghost = Ui.Child(_area, "Held");
            _ghost.sizeDelta = new Vector2(Cell, Cell);
            _ghostIcon = Ui.Image(Ui.Fill(Ui.Child(_ghost, "Icon"), 8f, 8f, 8f, 8f), null, Color.white);
            _ghostIcon.preserveAspect = true;
            _ghostAmount = Label(_ghost, "Amount", FontRole.Display, 18f, t.TextTitle, 0f, Cell - 26f, Cell - 6f, 24f, TextAlignmentOptions.Right);
            var ghostGroup = _ghost.gameObject.AddComponent<CanvasGroup>();
            ghostGroup.blocksRaycasts = false;
            ghostGroup.interactable = false;
            _ghost.gameObject.SetActive(false);
        }

        private void BuildInventory(ThemeTokens t)
        {
            var p = _inventoryPanel;
            _slotsText = Label(p, "Slots", FontRole.Label, 15f, t.TextFlavor, 46f, 60f, 360f, 22f, TextAlignmentOptions.Left);
            _slotsText.characterSpacing = 4f;

            // Filter list and sort, top right like the concept.
            var filter = WindowCanvas.At(p, "Filter", 526f, 30f, 140f, 38f);
            Frame.Dress(filter, _theme, "keycap_wide", "Windows", 38f);
            _filterText = Label(filter, "Text", FontRole.Body, 17f, t.TextTitle, 12f, 0f, 116f, 38f, TextAlignmentOptions.Left);
            Clickable(filter, "inventory filter", ToggleFilterList);
            var sort = WindowCanvas.At(p, "Sort", 678f, 30f, 100f, 38f);
            Frame.Dress(sort, _theme, "keycap_wide", "Windows", 38f);
            Label(sort, "Text", FontRole.Body, 16f, t.TextTitle, 0f, 0f, 100f, 38f, TextAlignmentOptions.Center).text = Localize("$genesisui_sort");
            Clickable(sort, "inventory sort", () =>
            {
                var gui = InventoryGui.instance;
                if (gui != null && Player.m_localPlayer != null && InventoryModule.Current != null) Sort(gui, Player.m_localPlayer, InventoryModule.Current);
            });

            // The grid: eight columns, four rows in view, the rest by scrolling.
            var grid = WindowCanvas.At(p, "Grid", GridX, GridY, SlotLayout.Width * Cell + (SlotLayout.Width - 1) * GapX,
                VisibleRows * Cell + (VisibleRows - 1) * GapY);
            var gridHit = Ui.Image(grid, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            gridHit.gameObject.AddComponent<CellInput>().Init(this, null);
            for (int y = 0; y < VisibleRows; y++)
                for (int x = 0; x < SlotLayout.Width; x++)
                    _gridCells.Add(MakeCell(grid, "Cell " + x + "," + y, x * (Cell + GapX), y * (Cell + GapY), Cell, null));

            _scrollTrack = WindowCanvas.At(p, "Scroll", GridX + SlotLayout.Width * Cell + (SlotLayout.Width - 1) * GapX + 12f, GridY, 3f, grid.sizeDelta.y);
            Ui.Image(_scrollTrack, null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.25f));
            _scrollThumb = WindowCanvas.At(_scrollTrack, "Thumb", -1f, 0f, 5f, 40f);
            Ui.Image(_scrollThumb, null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.85f));

            // Quick-use and action slots, smaller, under the grid.
            _quickTitle = Label(p, "QuickTitle", FontRole.Label, 15f, t.AccentGoldBright, QuickX + 2f, SpecialY - 34f, 330f, 22f, TextAlignmentOptions.Left).gameObject;
            _quickTitle.GetComponent<TextMeshProUGUI>().text = Localize("$genesisui_quick_title").ToUpperInvariant();
            _quickTitle.GetComponent<TextMeshProUGUI>().characterSpacing = 5f;
            for (int i = 0; i < SlotLayout.MaxQuick; i++)
            {
                var cell = MakeCell(p, "Quick " + i, QuickX + i * QuickPitch, SpecialY, SmallCell, null);
                cell.ShowMax = true;
                _quickCells.Add(cell);
            }
            _specialDivider = WindowCanvas.At(p, "Divider", 422f, SpecialY - 30f, 1f, SmallCell + 34f).gameObject;
            Ui.Image((RectTransform)_specialDivider.transform, null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.35f));
            _utilityTitle = Label(p, "UtilityTitle", FontRole.Label, 15f, t.AccentGoldBright, UtilityX, SpecialY - 34f, 330f, 22f, TextAlignmentOptions.Left).gameObject;
            _utilityTitle.GetComponent<TextMeshProUGUI>().text = Localize("$genesisui_utility_title").ToUpperInvariant();
            _utilityTitle.GetComponent<TextMeshProUGUI>().characterSpacing = 5f;
            for (int i = 0; i < SlotLayout.MaxUtility; i++)
                _utilityCells.Add(MakeCell(p, "Utility " + i, UtilityX + i * UtilityPitch, SpecialY, SmallCell, UtilityKeys[i]));

            // Weight, at the bottom like the concept.
            Label(p, "WeightLabel", FontRole.Label, 16f, t.TextFlavor, 56f, 626f, 90f, 26f, TextAlignmentOptions.Left).text =
                Localize("$genesisui_weight").ToUpperInvariant();
            var track = WindowCanvas.At(p, "WeightTrack", 130f, 636f, 450f, 6f);
            Ui.Image(track, null, new Color(0f, 0f, 0f, 0.6f));
            _weightFill = Ui.Image(Ui.Fill(Ui.Child(track, "Fill")), _theme.Sprite("bar_fill"), ThemeRuntime.ToUnity(t.AccentGold));
            _weightFill.type = Image.Type.Filled;
            _weightFill.fillMethod = Image.FillMethod.Horizontal;
            _weightText = Label(p, "WeightValue", FontRole.Label, 17f, t.TextTitle, 600f, 626f, 170f, 26f, TextAlignmentOptions.Left);

            BuildFilterList(p, filter);
            ShowFilter();
        }

        private void BuildEquipment(ThemeTokens t)
        {
            var p = _equipmentPanel;
            string[] tokens =
            {
                "$genesisui_slot_head", "$genesisui_slot_chest", "$genesisui_slot_cape", "$genesisui_slot_legs",
                "$genesisui_slot_trinket", "$genesisui_slot_belt", "$genesisui_slot_amulet", "$genesisui_slot_ring",
                "$genesisui_slot_lantern", "$genesisui_slot_backpack",
            };
            for (int i = 0; i < WornOrder.Length; i++)
            {
                var cell = MakeCell(p, "Worn " + WornOrder[i], 0f, 0f, WornCell, null);
                cell.Label = Label(p, "Label " + WornOrder[i], FontRole.Body, 16f, t.TextTitle, 0f, 0f, WornCell + 40f, 22f, TextAlignmentOptions.Center);
                cell.Label.text = Localize(tokens[i]);
                cell.Label.gameObject.SetActive(false);
                _wornLabels.Add(cell.Label.gameObject);
                cell.Root.gameObject.SetActive(false);
                _wornCells[WornOrder[i]] = cell;
            }
            var armorLabel = Label(p, "ArmorLabel", FontRole.Label, 14f, t.TextFlavor, 0f, 586f, EquipmentW, 20f, TextAlignmentOptions.Center);
            armorLabel.text = Localize("$genesisui_armor_total").ToUpperInvariant();
            armorLabel.characterSpacing = 5f;
            _armorText = Label(p, "Armor", FontRole.Display, 34f, t.TextTitle, 0f, 610f, EquipmentW, 42f, TextAlignmentOptions.Center);
        }

        private void BuildContainer(ThemeTokens t)
        {
            var p = _containerPanel;
            float w = WindowCanvas.Design.x - EquipmentX;
            var take = WindowCanvas.At(p, "TakeAll", w - 24f - 150f, 30f, 150f, 38f);
            Frame.Dress(take, _theme, "keycap_wide", "Windows", 38f);
            Label(take, "Text", FontRole.Body, 16f, t.TextTitle, 0f, 0f, 150f, 38f, TextAlignmentOptions.Center).text = Localize("$genesisui_take_all");
            Clickable(take, "container take all", () => { var gui = InventoryGui.instance; if (gui != null) gui.m_takeAllButton.onClick.Invoke(); });
            var stack = WindowCanvas.At(p, "StackAll", w - 24f - 150f - 12f - 150f, 30f, 150f, 38f);
            Frame.Dress(stack, _theme, "keycap_wide", "Windows", 38f);
            Label(stack, "Text", FontRole.Body, 16f, t.TextTitle, 0f, 0f, 150f, 38f, TextAlignmentOptions.Center).text = Localize("$genesisui_stack_all");
            Clickable(stack, "container stack all", () => { var gui = InventoryGui.instance; if (gui != null) gui.m_stackAllButton.onClick.Invoke(); });
            _containerGrid = WindowCanvas.At(p, "Grid", 0f, GridY, w, PanelsHeight - GridY - 40f);
            var hit = Ui.Image(_containerGrid, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            hit.gameObject.AddComponent<CellInput>().Init(this, null).ContainerScroll = true;
        }

        /// <summary>Chest cells for its size: vanilla chests are up to 8 x 4; bigger ones scroll.</summary>
        private void BuildContainerCells(int width, int height)
        {
            foreach (var cell in _containerCells) UnityEngine.Object.Destroy(cell.Root.gameObject);
            _containerCells.Clear();
            _builtContainerW = width;
            _builtContainerH = height;
            var area = _containerGrid.sizeDelta;
            float size = Mathf.Floor(Mathf.Min(Cell, (area.x - 80f - (width - 1) * GapX) / Mathf.Max(1, width)));
            _containerVisibleRows = Mathf.Max(1, Mathf.Min(height, Mathf.FloorToInt((area.y + GapY) / (size + GapY))));
            float gridW = width * size + (width - 1) * GapX;
            float x0 = (area.x - gridW) / 2f;
            for (int y = 0; y < _containerVisibleRows; y++)
                for (int x = 0; x < width; x++)
                {
                    var cell = MakeCell(_containerGrid, "Cell " + x + "," + y, x0 + x * (size + GapX), y * (size + GapY), size, null);
                    cell.Container = true;
                    _containerCells.Add(cell);
                }
            GenesisLog.Info("Module:win.inventory", "chest panel " + width + "x" + height + ", " + _containerVisibleRows + " row(s) in view, cell " + size);
        }

        /// <summary>
        /// A panel of the concept: Diego's finest frame, the title on the header rule (left like
        /// Inventário/Equipamento, centred for the details) and the whole tab marker as the rule.
        /// </summary>
        private RectTransform Panel(RectTransform parent, string name, float x, float width, string titleToken, float titleX, float titleSize,
                                    TextAlignmentOptions align)
        {
            var rt = WindowCanvas.At(parent, name, x, 0f, width, PanelsHeight);
            // The panel takes the pointer: a click on its empty parts must not reach vanilla's
            // drop-outside button behind the window (that would throw a held item on the ground).
            Ui.Image(rt, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(rt, _theme, "window_panel", "Windows");
            bool centred = align == TextAlignmentOptions.Center;
            var title = Label(rt, "Title", FontRole.Display, titleSize, _theme.Tokens.AccentGoldBright,
                centred ? 60f : titleX, 12f, centred ? width - 120f : width - titleX - 70f, 36f, align);
            title.characterSpacing = centred ? 5f : 8f;
            title.text = Localize(titleToken).ToUpperInvariant();

            var marker = _theme.Sprite("tab_marker");
            if (marker != null)
            {
                float mw = centred ? width * 0.6f : Mathf.Min(300f, width * 0.5f);
                var mrt = WindowCanvas.At(rt, "Rule", centred ? (width - mw) / 2f : titleX - 6f, 48f, mw, 8f);
                var img = Ui.Image(mrt, marker, Color.white);
                img.pixelsPerUnitMultiplier = _theme.Size("tab_marker").y / 8f * Frame.CanvasScale(mrt);
            }
            return rt;
        }

        private ItemCell MakeCell(RectTransform parent, string name, float x, float y, float size, string key)
        {
            var t = _theme.Tokens;
            var cell = new ItemCell { Root = WindowCanvas.At(parent, name, x, y, size, size) };
            var hit = Ui.Image(cell.Root, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(cell.Root, _theme, "hotslot", "Windows", size);
            cell.Icon = Ui.Image(Ui.Fill(Ui.Child(cell.Root, "Icon"), size * 0.13f, size * 0.13f, size * 0.13f, size * 0.13f), null, Color.white);
            cell.Icon.preserveAspect = true;
            cell.Icon.enabled = false;
            cell.Hover = Overlay(cell.Root, "hotslot_selected", size);
            cell.Equipped = Overlay(cell.Root, "hotslot_equipped", size);

            cell.DurBack = Ui.Image(WindowCanvas.At(cell.Root, "Durability", size * 0.14f, size * 0.86f, size * 0.72f, Mathf.Max(3f, size * 0.045f)),
                null, new Color(0f, 0f, 0f, 0.6f));
            cell.Dur = Ui.Image(Ui.Fill(Ui.Child((RectTransform)cell.DurBack.transform, "Fill")), _theme.Sprite("bar_fill"), ThemeRuntime.ToUnity(t.StatePositive));
            cell.Dur.type = Image.Type.Filled;
            cell.Dur.fillMethod = Image.FillMethod.Horizontal;
            cell.DurBack.gameObject.SetActive(false);
            cell.Good = ThemeRuntime.ToUnity(t.StatePositive);
            cell.Bad = ThemeRuntime.ToUnity(t.StateDanger);

            cell.Amount = Label(cell.Root, "Amount", FontRole.Display, size * 0.22f, t.TextTitle, size * 0.08f, size * 0.62f, size * 0.84f, size * 0.3f, TextAlignmentOptions.BottomRight);
            cell.Quality = Label(cell.Root, "Quality", FontRole.Display, size * 0.2f, t.AccentGoldBright, size * 0.5f, size * 0.06f, size * 0.42f, size * 0.26f, TextAlignmentOptions.TopRight);
            cell.Index = Label(cell.Root, "Index", FontRole.Label, size * 0.2f, t.TextTitle, size * 0.1f, size * 0.05f, size * 0.4f, size * 0.26f, TextAlignmentOptions.TopLeft);
            if (key != null)
            {
                var cap = WindowCanvas.At(cell.Root, "Key", 4f, 4f, 20f, 20f);
                Frame.Dress(cap, _theme, "keycap", "Windows", 20f);
                Label(cap, "Text", FontRole.Label, 12f, t.TextTitle, 0f, 0f, 20f, 20f, TextAlignmentOptions.Center).text = key;
            }
            hit.gameObject.AddComponent<CellInput>().Init(this, cell);
            return cell;
        }

        /// <summary>A state frame from Diego's sheet laid over the cell, its window matching the cell.</summary>
        private Image Overlay(RectTransform cell, string sprite, float size)
        {
            var s = _theme.Sprite(sprite);
            if (s == null) return null;
            var drawn = _theme.Size(sprite);
            var c = _theme.Content(sprite, Vector4.zero);
            float k = size / Mathf.Max(1f, drawn.x - c.x - c.z);
            var rt = Ui.Place(Ui.Child(cell, sprite), new Vector2(0.5f, 0.5f), Vector2.zero, drawn * k);
            var img = Ui.Image(rt, s, Color.white);
            img.type = Image.Type.Simple;
            img.enabled = false;
            return img;
        }

        private void BuildFilterList(RectTransform panel, RectTransform field)
        {
            var t = _theme.Tokens;
            const float row = 28f;
            _filterList = WindowCanvas.At(panel, "FilterList", field.anchoredPosition.x, -field.anchoredPosition.y + field.sizeDelta.y + 2f,
                170f, row * FilterTokens.Length + 16f);
            Frame.Dress(_filterList, _theme, "card", "Windows");
            for (int i = 0; i < FilterTokens.Length; i++)
            {
                var choice = (Filter)i;
                var rt = WindowCanvas.At(_filterList, "Option " + choice, 12f, 8f + i * row, 146f, row);
                Label(rt, "Text", FontRole.Body, 17f, t.TextTitle, 6f, 0f, 140f, row, TextAlignmentOptions.Left).text = Localize(FilterTokens[i]);
                Clickable(rt, "filter option", () => SelectFilter(choice));
            }
            _filterList.gameObject.SetActive(false);
        }

        private static void Clickable(RectTransform rt, string what, Action onClick)
        {
            var hit = Ui.Image(Ui.Fill(Ui.Child(rt, "Hit")), null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Guard.Try(what, onClick));
        }

        private void ToggleFilterList()
        {
            bool show = !_filterList.gameObject.activeSelf;
            _filterList.gameObject.SetActive(show);
            if (show) _filterList.SetAsLastSibling();
        }

        private void SelectFilter(Filter filter)
        {
            _filter = filter;
            ShowFilter();
            _filterList.gameObject.SetActive(false);
            GenesisLog.Info("Module:win.inventory", "filter selected: " + filter);
        }

        private void ShowFilter() => _filterText.text = Localize(FilterTokens[(int)_filter]) + "  ◆";

        /// <summary>A text placed by its top-left corner in design units.</summary>
        private TextMeshProUGUI Label(RectTransform parent, string name, FontRole role, float size, ColorRgba color,
                                     float x, float y, float width, float height, TextAlignmentOptions alignment)
        {
            var text = Ui.Fit(Ui.Text(parent, name, _theme, role, size, ThemeRuntime.ToUnity(color), alignment, outlined: true), Mathf.Min(10f, size));
            var rt = (RectTransform)text.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
            return text;
        }

        private static string Localize(string text) => Localization.instance != null ? Localization.instance.Localize(text) : text;

        // ------------------------------------------------------------------ views

        /// <summary>One item cell of our window, bound to an inventory position.</summary>
        internal sealed class ItemCell
        {
            public RectTransform Root;
            public Image Icon, Hover, Equipped, DurBack, Dur;
            public TextMeshProUGUI Amount, Quality, Index, Label;
            public Color Good, Bad;
            public bool Container, ShowMax;
            public Vector2i Pos;
            public bool Active;

            private ItemDrop.ItemData _item;
            private int _stack = -1, _quality = -1;
            private float _durability = -2f, _alpha = -1f;
            private bool _equipped, _hover;
            private string _index;

            public void Bind(Vector2i pos, bool active, string index)
            {
                Pos = pos;
                Active = active;
                if (index != _index)
                {
                    _index = index;
                    Index.text = index ?? "";
                }
            }

            public void Show(ItemDrop.ItemData item, bool equipped, bool hover, float alpha)
            {
                if (item != _item)
                {
                    _item = item;
                    Icon.sprite = item != null ? item.GetIcon() : null;
                    Icon.enabled = item != null;
                    _stack = _quality = -1;
                    _durability = -2f;
                }
                if (item != null && !Mathf.Approximately(alpha, _alpha))
                {
                    _alpha = alpha;
                    Icon.color = new Color(1f, 1f, 1f, alpha);
                }

                int stack = item != null && item.m_shared.m_maxStackSize > 1 ? item.m_stack : 0;
                if (stack != _stack)
                {
                    _stack = stack;
                    if (stack <= 0) Amount.text = "";
                    else if (ShowMax) Amount.SetText("{0}/{1}", stack, item.m_shared.m_maxStackSize);
                    else Amount.SetText("{0}", stack);
                }
                int quality = item != null && item.m_shared.m_maxQuality > 1 ? item.m_quality : 0;
                if (quality != _quality)
                {
                    _quality = quality;
                    if (quality <= 0) Quality.text = "";
                    else Quality.SetText("{0}", quality);
                }
                float durability = item != null && item.m_shared.m_useDurability
                    ? Mathf.Clamp01(item.m_durability / Mathf.Max(1f, item.GetMaxDurability())) : -1f;
                if (!Mathf.Approximately(durability, _durability))
                {
                    _durability = durability;
                    bool bar = durability >= 0f && durability < 0.999f;
                    if (DurBack.gameObject.activeSelf != bar) DurBack.gameObject.SetActive(bar);
                    if (bar)
                    {
                        Dur.fillAmount = durability;
                        Dur.color = durability < 0.25f ? Bad : Good;
                    }
                }
                if (equipped != _equipped)
                {
                    _equipped = equipped;
                    if (Equipped != null) Equipped.enabled = equipped;
                }
                if (hover != _hover)
                {
                    _hover = hover;
                    if (Hover != null) Hover.enabled = hover;
                }
            }
        }

        /// <summary>Pointer events of one cell, handed to the module (and from there to vanilla).</summary>
        internal sealed class CellInput : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler,
            IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IScrollHandler
        {
            private InventoryWindowModule _owner;
            private ItemCell _cell;
            internal bool ContainerScroll;

            internal CellInput Init(InventoryWindowModule owner, ItemCell cell)
            {
                _owner = owner;
                _cell = cell;
                return this;
            }

            public void OnPointerDown(PointerEventData e) { if (_cell != null) Guard.Try("inventory cell press", () => _owner.OnDown(_cell, e)); }
            public void OnPointerEnter(PointerEventData e) { if (_cell != null) _owner.OnEnter(_cell); }
            public void OnPointerExit(PointerEventData e) { if (_cell != null) _owner.OnExit(_cell); }
            public void OnBeginDrag(PointerEventData e) { }
            public void OnDrag(PointerEventData e) { }
            public void OnEndDrag(PointerEventData e) => Guard.Try("inventory drag end", () => _owner.OnEndDrag(e));
            public void OnDrop(PointerEventData e) { if (_cell != null) Guard.Try("inventory drop", () => _owner.OnDrop(_cell)); }

            public void OnScroll(PointerEventData e)
            {
                if (_cell != null) _owner.OnScroll(_cell, e);
                else if (ContainerScroll) _owner._containerScroll += e.scrollDelta.y > 0f ? -1 : e.scrollDelta.y < 0f ? 1 : 0;
                else _owner.OnScroll(null, e);
            }
        }

        /// <summary>The details panel (replaces vanilla's tooltip): icon, name, type, description, stats.</summary>
        private sealed class Details
        {
            private const int MaxRows = 8;
            private readonly InventoryWindowModule _m;
            private readonly Image _icon;
            private readonly TextMeshProUGUI _name, _type, _description, _empty;
            private readonly TextMeshProUGUI[] _labels = new TextMeshProUGUI[MaxRows];
            private readonly TextMeshProUGUI[] _values = new TextMeshProUGUI[MaxRows];
            private readonly GameObject[] _rows = new GameObject[MaxRows];
            private readonly Image _durBack, _dur;
            private readonly GameObject _body;

            internal Details(InventoryWindowModule m, RectTransform p)
            {
                _m = m;
                var t = m._theme.Tokens;
                const float pad = 24f, w = DetailsW - 2f * pad;
                _body = WindowCanvas.At(p, "Body", 0f, 0f, DetailsW, PanelsHeight).gameObject;
                var body = (RectTransform)_body.transform;
                _icon = Ui.Image(WindowCanvas.At(body, "Icon", pad, 74f, w, 150f), null, Color.white);
                _icon.preserveAspect = true;
                _name = m.Label(body, "Name", FontRole.Display, 24f, t.AccentGoldBright, pad, 236f, w, 32f, TextAlignmentOptions.Left);
                _type = m.Label(body, "Type", FontRole.Body, 17f, t.TextFlavor, pad, 268f, w, 22f, TextAlignmentOptions.Left);
                _description = Ui.Text(body, "Description", m._theme, FontRole.Body, 16f, ThemeRuntime.ToUnity(t.TextBody), TextAlignmentOptions.TopLeft);
                var drt = (RectTransform)_description.transform;
                drt.anchorMin = drt.anchorMax = drt.pivot = new Vector2(0f, 1f);
                drt.anchoredPosition = new Vector2(pad, -298f);
                drt.sizeDelta = new Vector2(w, 70f);
                _description.textWrappingMode = TextWrappingModes.Normal;
                _description.overflowMode = TextOverflowModes.Ellipsis;
                Ui.Image(WindowCanvas.At(body, "Rule", pad, 376f, w, 1f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.35f));
                for (int i = 0; i < MaxRows; i++)
                {
                    var row = WindowCanvas.At(body, "Row " + i, pad, 386f + i * 34f, w, 32f);
                    _rows[i] = row.gameObject;
                    _labels[i] = m.Label(row, "Label", FontRole.Body, 17f, t.TextBody, 0f, 0f, w * 0.55f, 26f, TextAlignmentOptions.Left);
                    _values[i] = m.Label(row, "Value", FontRole.Body, 17f, t.TextTitle, w * 0.45f, 0f, w * 0.55f, 26f, TextAlignmentOptions.Right);
                    Ui.Image(WindowCanvas.At(row, "Line", 0f, 31f, w, 1f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.12f));
                    row.gameObject.SetActive(false);
                }
                _durBack = Ui.Image(WindowCanvas.At(body, "DurabilityBar", pad + w * 0.45f, 0f, w * 0.55f, 4f), null, new Color(0f, 0f, 0f, 0.6f));
                _dur = Ui.Image(Ui.Fill(Ui.Child((RectTransform)_durBack.transform, "Fill")), m._theme.Sprite("bar_fill"), ThemeRuntime.ToUnity(t.AccentGold));
                _dur.type = Image.Type.Filled;
                _dur.fillMethod = Image.FillMethod.Horizontal;
                _durBack.gameObject.SetActive(false);
                _body.SetActive(false);

                _empty = m.Label(p, "Empty", FontRole.Body, 17f, t.TextFlavor, pad, 300f, w, 60f, TextAlignmentOptions.Center);
                _empty.textWrappingMode = TextWrappingModes.Normal;
                _empty.text = Localize("$genesisui_details_empty");
            }

            internal void Show(ItemDrop.ItemData item)
            {
                _empty.gameObject.SetActive(false);
                _body.SetActive(true);
                var s = item.m_shared;
                _icon.sprite = item.GetIcon();
                _name.text = Localize(s.m_name).ToUpperInvariant();
                _type.text = Localize(TypeToken(ItemCategories.Of(item)));
                _description.text = Localize(s.m_description);

                int n = 0;
                _durBack.gameObject.SetActive(false);
                Row(ref n, "$genesisui_stat_weight", item.GetWeight(-1).ToString("0.0"));
                if (s.m_useDurability)
                {
                    float max = item.GetMaxDurability();
                    Row(ref n, "$genesisui_stat_durability", Mathf.CeilToInt(item.m_durability) + " / " + Mathf.CeilToInt(max));
                    var bar = (RectTransform)_durBack.transform;
                    bar.anchoredPosition = new Vector2(bar.anchoredPosition.x, -(386f + (n - 1) * 34f + 26f));
                    _dur.fillAmount = max > 0f ? Mathf.Clamp01(item.m_durability / max) : 0f;
                    _durBack.gameObject.SetActive(true);
                }
                if (s.m_maxQuality > 1) Row(ref n, "$genesisui_stat_quality", item.m_quality + " / " + s.m_maxQuality);
                float armor = item.GetArmor();
                if (armor > 0f) Row(ref n, "$genesisui_stat_armor", armor.ToString("0"));
                float damage = item.GetDamage().GetTotalDamage();
                if (damage > 0f) Row(ref n, "$genesisui_stat_damage", damage.ToString("0"));
                float block = item.GetBaseBlockPower();
                if (block > 0f) Row(ref n, "$genesisui_stat_block", block.ToString("0"));
                if (s.m_food > 0f) Row(ref n, "$genesisui_stat_food_health", s.m_food.ToString("0"));
                if (s.m_foodStamina > 0f) Row(ref n, "$genesisui_stat_food_stamina", s.m_foodStamina.ToString("0"));
                if (s.m_foodEitr > 0f) Row(ref n, "$genesisui_stat_food_eitr", s.m_foodEitr.ToString("0"));
                if (s.m_foodBurnTime > 0f) Row(ref n, "$genesisui_stat_duration", Mathf.RoundToInt(s.m_foodBurnTime / 60f) + " min");
                if (s.m_value > 0) Row(ref n, "$genesisui_stat_value", s.m_value.ToString());
                for (int i = n; i < MaxRows; i++) if (_rows[i].activeSelf) _rows[i].SetActive(false);
            }

            private void Row(ref int n, string token, string value)
            {
                if (n >= MaxRows) return;
                _labels[n].text = Localize(token);
                _values[n].text = value;
                if (!_rows[n].activeSelf) _rows[n].SetActive(true);
                n++;
            }

            private static string TypeToken(ItemCategory c)
            {
                switch (c)
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
        }
    }
}
