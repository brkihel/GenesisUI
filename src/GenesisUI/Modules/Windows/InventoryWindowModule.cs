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
    /// The Inventory tab in ConceptArt (9)'s layout (F4.2a, D-025): our panels — inventory,
    /// equipment, item details — drawn on vanilla's inventory canvas right behind InventoryGui, and
    /// vanilla's own slots moved onto our grid and dressed with <see cref="VanillaSkin"/>. Every click,
    /// drag, split and equip stays vanilla's; vanilla's panel frame, texts and tooltips are hidden
    /// (never destroyed) and everything returns exactly on another tab, on close or on a fault.
    /// </summary>
    [GameContract("assembly_valheim", "InventoryGui", "m_player")]
    [GameContract("assembly_valheim", "InventoryGui", "m_crafting")]
    [GameContract("assembly_valheim", "InventoryGui", "m_info")]
    [GameContract("assembly_valheim", "InventoryGui", "m_container")]
    [GameContract("assembly_valheim", "InventoryGui", "m_playerGrid")]
    [GameContract("assembly_valheim", "InventoryGui", "IsContainerOpen")]
    [GameContract("assembly_valheim", "InventoryGrid", "m_elements")]
    [GameContract("assembly_valheim", "InventoryGrid", "GetHoveredElement")]
    [GameContract("assembly_valheim", "InventoryGrid", "GetInventory")]
    [GameContract("assembly_valheim", "InventoryElement", "m_icon")]
    [GameContract("assembly_valheim", "InventoryElement", "m_equiped")]
    [GameContract("assembly_valheim", "InventoryElement", "m_button")]
    [GameContract("assembly_valheim", "InventoryElement", "m_tooltip")]
    [GameContract("assembly_valheim", "InventoryElement", "get_Position")]
    [GameContract("assembly_valheim", "Inventory", "GetItemAt")]
    [GameContract("assembly_valheim", "Inventory", "GetTotalWeight")]
    [GameContract("assembly_valheim", "Player", "GetMaxCarryWeight")]
    [GameContract("assembly_valheim", "Player", "GetBodyArmor")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetTooltip", Parameters = new[]
        { "ItemDrop+ItemData", "System.Int32", "System.Boolean", "System.Single", "System.Int32", "System.Boolean" })]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_itemType")]
    internal sealed class InventoryWindowModule : IUiModule
    {
        private const string Owner = "module:win.inventory";
        private static readonly string[] NoRegions = new string[0];
        private static readonly Action<InventoryWindowModule> AlignForRender = module => module.AlignVisibleSlots();

        // Inside the shell's window area (WindowCanvas): below the tab bar, above the hint bar.
        private const float BarsTop = 72f, BarsBottom = 54f, Gap = 8f;
        private const float InventoryShare = 0.47f, EquipmentShare = 0.25f;
        private const float CellGap = 6f, HeaderRule = 40f;

        internal enum Filter { All, Weapons, Armor, Tools, Consumables, Materials, Ammo, Misc }

        private static readonly string[] FilterTokens =
        {
            "$genesisui_filter_all", "$genesisui_filter_weapons", "$genesisui_filter_armor", "$genesisui_filter_tools",
            "$genesisui_filter_consumables", "$genesisui_filter_materials", "$genesisui_filter_ammo", "$genesisui_filter_misc",
        };

        private readonly VanillaSkin _skin = new VanillaSkin("module:win.inventory");
        private readonly Vector3[] _corners = new Vector3[4];
        private ThemeRuntime _theme;
        private RectTransform _root;
        private RectTransform _area;
        private RectTransform _controlsRoot;
        private RectTransform _controlsArea;
        private RectTransform _inventoryPanel;
        private RectTransform _equipmentPanel;
        private RectTransform _detailsPanel;
        private RectTransform _gridArea;
        private RectTransform _filterList;
        private float _cell;
        private TextMeshProUGUI _slotsText;
        private TextMeshProUGUI _weightText;
        private Image _weightFill;
        private TextMeshProUGUI _armorText;
        private TextMeshProUGUI _filterText;
        private Image _detailIcon;
        private TextMeshProUGUI _detailName;
        private TextMeshProUGUI _detailBody;
        private Filter _filter = Filter.All;
        private bool _applied;
        private bool _containerShown;
        private InventoryElement _firstElement;
        private int _elementCount;
        private int _dressedRows = -1;
        private ItemDrop.ItemData _shownItem;
        private int _shownSlots = -1, _shownWeight = -1, _shownMax = -1, _shownArmor = -1;
        private bool _loggedElement;
        private bool _reportedDrift;

        private AccessTools.FieldRef<InventoryGrid, List<InventoryElement>> _elements;
        private Func<InventoryGrid, InventoryElement> _hovered;

        public string Id => "win.inventory";
        public string NameToken => "$genesisui_module_inventory_window";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f;

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _elements = AccessTools.FieldRefAccess<InventoryGrid, List<InventoryElement>>("m_elements");
            _hovered = AccessTools.MethodDelegate<Func<InventoryGrid, InventoryElement>>(
                AccessTools.Method(typeof(InventoryGrid), "GetHoveredElement"));
            _applied = false;
            Canvas.willRenderCanvases += OnWillRenderCanvases;
            // Panels are built on vanilla's canvas when it exists (EnsureBuilt).
        }

        public void Refresh(float deltaSeconds)
        {
            var gui = InventoryGui.instance;
            var player = Player.m_localPlayer;
            bool on = gui != null && player != null && WindowShellModule.Showing && WindowShellModule.ActiveTab == WindowShellModule.Tab.Inventory;
            if (!on)
            {
                if (_applied) Unapply();
                return;
            }
            if (!EnsureBuilt(gui)) return;
            if (!_applied) Apply(gui);

            var grid = gui.m_playerGrid;
            var elements = _elements(grid);
            int rows = InventoryModule.Current != null ? InventoryModule.Current.Rows : SlotLayout.MinRows;
            if (elements.Count != _elementCount || rows != _dressedRows || (elements.Count > 0 && elements[0] != _firstElement))
                DressElements(elements, rows); // vanilla rebuilds its elements when the size changes
            AlignVisibleSlots(); // also correct before input; the canvas callback corrects after layout
            ApplyFilter(grid, elements);
            PlaceContainer(gui);
            if (!_containerShown) UpdateDetails(grid);
            UpdateStats(player);
        }

        public void Teardown()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            if (_applied) Unapply();
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            if (_controlsRoot != null) UnityEngine.Object.Destroy(_controlsRoot.gameObject);
            _root = null;
            _controlsRoot = null;
        }

        private bool EnsureBuilt(InventoryGui gui)
        {
            if (_root != null) return true;
            _root = WindowCanvas.CreateRoot(gui, "GenesisUI.InventoryWindow", behind: true);
            if (_root == null) return false;
            _area = WindowCanvas.Area(_root, "Area");
            // Only interactive controls are drawn in front of InventoryGui. The panel art remains
            // behind vanilla's item cells, while the filter can receive pointer events reliably.
            _controlsRoot = WindowCanvas.CreateRoot(gui, "GenesisUI.InventoryControls", behind: false);
            if (_controlsRoot == null)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
                _root = null;
                return false;
            }
            _controlsArea = WindowCanvas.Area(_controlsRoot, "Area");
            BuildPanels();
            _root.gameObject.SetActive(false);
            _controlsRoot.gameObject.SetActive(false);
            return true;
        }

        // ------------------------------------------------------------------ vanilla

        private void Apply(InventoryGui gui)
        {
            _applied = true;
            _root.gameObject.SetActive(true);
            _controlsRoot.gameObject.SetActive(true);

            // Hide only vanilla panel graphics outside the grid. A CanvasGroup with alpha=0
            // on m_player also suppresses its child grid on Unity 6 despite
            // ignoreParentGroups, and breaks the slots' raycasts (R-049).
            foreach (var graphic in gui.m_player.GetComponentsInChildren<Graphic>(true))
                if (!graphic.transform.IsChildOf(gui.m_playerGrid.transform))
                    _skin.Enabled(graphic).enabled = false;

            // Crafting and the character info belong to other tabs.
            Hide(gui.m_crafting.gameObject);
            Hide(gui.m_info.gameObject);
            _dressedRows = -1;
            _reportedDrift = false;
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
            // A moved button may never receive the pointer-exit event that clears Unity's hover
            // transition. Clear its state before restoring its original sprite and colours.
            var gui = InventoryGui.instance;
            if (gui != null && gui.m_playerGrid != null && EventSystem.current != null)
            {
                var eventSystem = EventSystem.current;
                var selected = eventSystem.currentSelectedGameObject;
                if (selected != null && selected.transform.IsChildOf(gui.m_playerGrid.transform))
                    eventSystem.SetSelectedGameObject(null);
                var pointer = new PointerEventData(eventSystem);
                foreach (var element in _elements(gui.m_playerGrid))
                    if (element != null && element.m_button != null)
                        element.m_button.OnPointerExit(pointer);
            }
            _skin.Restore();
            _firstElement = null;
            _elementCount = 0;
            _dressedRows = -1;
            _shownItem = null;
            _containerShown = false;
            if (_filterList != null) _filterList.gameObject.SetActive(false);
            if (_equipmentPanel != null) _equipmentPanel.gameObject.SetActive(true);
            if (_detailsPanel != null) _detailsPanel.gameObject.SetActive(true);
            if (_root != null) _root.gameObject.SetActive(false);
            if (_controlsRoot != null) _controlsRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// Each vanilla slot of the ordinary rows goes to its cell on our grid (same canvas: world
        /// positions line up), wears Diego's slot under its icon, and loses its tooltip (the details
        /// panel shows the item). Vanilla's own slot background is hidden; its "equipped" mark wears
        /// the equipped slot. Special rows stay hidden by the inventory module.
        /// </summary>
        private void DressElements(List<InventoryElement> elements, int rows)
        {
            _elementCount = elements.Count;
            _firstElement = elements.Count > 0 ? elements[0] : null;
            _dressedRows = rows;
            var slot = _theme.Sprite("hotslot");
            var equipped = _theme.Sprite("hotslot_equipped");

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null) continue;
                var pos = element.Position;
                if (pos.y >= rows) continue;

                var rt = _skin.Rect((RectTransform)element.transform);
                var target = CellWorld(pos.x, pos.y);
                float scale = rt.parent != null ? Mathf.Max(0.0001f, rt.parent.lossyScale.x) : 1f;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(target.width, target.height) / scale;
                rt.position = target.center;

                if (!_loggedElement)
                {
                    _loggedElement = true;
                    LogElement(element);
                }

                // Dress the actual button graphic. It remains the raycast target and keeps
                // vanilla's click, drag, split and gamepad handling on the same object.
                var rootBackground = element.GetComponent<Image>();
                var buttonBackground = element.m_button != null ? element.m_button.targetGraphic as Image : null;
                var background = buttonBackground != null ? buttonBackground : rootBackground;
                if (slot != null && background != null)
                {
                    _skin.Image(background);
                    background.sprite = slot;
                    background.type = Image.Type.Sliced;
                    background.color = Color.white;
                    background.pixelsPerUnitMultiplier = Frame.CanvasScale(background.transform) * _theme.Size("hotslot").y / _cell;
                    if (rootBackground != null && rootBackground != background)
                        _skin.Enabled(rootBackground).enabled = false;
                    if (element.m_button != null)
                    {
                        _skin.Button(element.m_button);
                        var colors = element.m_button.colors;
                        colors.normalColor = Color.white;
                        colors.highlightedColor = new Color(1f, 0.91f, 0.70f, 1f);
                        colors.pressedColor = new Color(1f, 0.78f, 0.46f, 1f);
                        colors.selectedColor = colors.highlightedColor;
                        element.m_button.colors = colors;
                    }
                }

                // The equipped mark: Diego's equipped slot over the cell instead of vanilla's blue square.
                if (element.m_equiped != null && equipped != null)
                {
                    _skin.Image(element.m_equiped);
                    element.m_equiped.sprite = equipped;
                    element.m_equiped.type = Image.Type.Simple;
                    element.m_equiped.color = Color.white;
                    var ert = _skin.Rect(element.m_equiped.rectTransform);
                    var drawn = _theme.Size("hotslot_equipped");
                    var c = _theme.Content("hotslot_equipped", Vector4.zero);
                    float k = _cell / Mathf.Max(1f, drawn.x - c.x - c.z);
                    ert.anchorMin = ert.anchorMax = ert.pivot = new Vector2(0.5f, 0.5f);
                    ert.anchoredPosition = Vector2.zero;
                    ert.sizeDelta = drawn * k;
                }
                if (element.m_tooltip != null) _skin.Enabled(element.m_tooltip).enabled = false;
            }
        }

        /// <summary>Logs one element's objects once, so a styling problem can be read in the log.</summary>
        private static void LogElement(InventoryElement element)
        {
            var sb = new System.Text.StringBuilder("inventory slot parts:");
            foreach (var image in element.GetComponentsInChildren<Image>(true))
                sb.Append(' ').Append(image.gameObject.name).Append('(').Append(image.sprite != null ? image.sprite.name : "none").Append(')');
            GenesisLog.Info("Module:win.inventory", sb.ToString());
        }

        /// <summary>A grid cell in world space.</summary>
        private Rect CellWorld(int x, int y)
        {
            _gridArea.GetWorldCorners(_corners);
            float unit = (_corners[2].x - _corners[0].x) / Mathf.Max(1f, _gridArea.rect.width);
            float cx = _corners[1].x + (x * (_cell + CellGap) + _cell / 2f) * unit;
            float cy = _corners[1].y - (y * (_cell + CellGap) + _cell / 2f) * unit;
            float size = _cell * unit;
            return new Rect(cx - size / 2f, cy - size / 2f, size, size);
        }

        private void OnWillRenderCanvases()
        {
            if (!_applied) return;
            if (!Guard.Run(Owner, AlignForRender, this) && Guard.IsTripped(Owner)) Unapply();
        }

        /// <summary>Vanilla moves its grid after opening and when changing tabs. Keep the original
        /// slot objects aligned with our panel after that movement and after UI layout.</summary>
        private void AlignVisibleSlots()
        {
            if (!_applied || _gridArea == null) return;
            var gui = InventoryGui.instance;
            if (gui == null || gui.m_playerGrid == null) return;
            var elements = _elements(gui.m_playerGrid);
            if (elements == null || elements.Count != _elementCount ||
                (elements.Count > 0 && elements[0] != _firstElement)) return;
            int rows = InventoryModule.Current != null ? InventoryModule.Current.Rows : SlotLayout.MinRows;
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null) continue;
                var pos = element.Position;
                if (pos.y >= rows) continue;
                var rt = (RectTransform)element.transform;
                var target = CellWorld(pos.x, pos.y);
                var world = target.center;
                if (((Vector2)rt.position - world).sqrMagnitude < 0.0625f) continue;
                if (!_reportedDrift && ((Vector2)rt.position - world).sqrMagnitude > 25f)
                {
                    _reportedDrift = true;
                    GenesisLog.Info("Module:win.inventory", "vanilla grid moved after dressing; aligning cells on each canvas render");
                }
                rt.position = world;
            }
        }

        private void PlaceContainer(InventoryGui gui)
        {
            bool open = gui.IsContainerOpen() && gui.m_container.gameObject.activeInHierarchy;
            if (open != _containerShown)
            {
                _containerShown = open;
                _equipmentPanel.gameObject.SetActive(!open);
                _detailsPanel.gameObject.SetActive(!open);
            }
            if (!open) return;
            var rt = _skin.Rect(gui.m_container);
            _equipmentPanel.GetWorldCorners(_corners);
            var left = _corners[1];
            _detailsPanel.GetWorldCorners(_corners);
            var right = _corners[2];
            var target = new Vector3((left.x + right.x) / 2f, left.y, 0f);
            if (rt.pivot != new Vector2(0.5f, 1f)) rt.pivot = new Vector2(0.5f, 1f);
            if ((rt.position - target).sqrMagnitude > 0.25f) rt.position = target;
        }

        // ------------------------------------------------------------------ filter, details, stats

        private void ApplyFilter(InventoryGrid grid, List<InventoryElement> elements)
        {
            var inventory = grid.GetInventory();
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null || element.m_icon == null) continue;
                var p = element.Position;
                var item = inventory.GetItemAt(p.x, p.y);
                float alpha = _filter == Filter.All || item == null || Matches(item, _filter) ? 1f : 0.22f;
                var renderer = element.m_icon.canvasRenderer;
                if (!Mathf.Approximately(renderer.GetAlpha(), alpha))
                {
                    _skin.RendererAlpha(renderer);
                    renderer.SetAlpha(alpha);
                }
            }
        }

        internal static bool Matches(ItemDrop.ItemData item, Filter filter)
        {
            var t = item.m_shared.m_itemType;
            switch (filter)
            {
                case Filter.Weapons:
                    return t == ItemDrop.ItemData.ItemType.OneHandedWeapon || t == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                           t == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft || t == ItemDrop.ItemData.ItemType.Bow ||
                           t == ItemDrop.ItemData.ItemType.Torch;
                case Filter.Armor:
                    return t == ItemDrop.ItemData.ItemType.Helmet || t == ItemDrop.ItemData.ItemType.Chest ||
                           t == ItemDrop.ItemData.ItemType.Legs || t == ItemDrop.ItemData.ItemType.Shoulder ||
                           t == ItemDrop.ItemData.ItemType.Hands || t == ItemDrop.ItemData.ItemType.Utility ||
                           t == ItemDrop.ItemData.ItemType.Trinket || t == ItemDrop.ItemData.ItemType.Shield;
                case Filter.Tools: return t == ItemDrop.ItemData.ItemType.Tool;
                case Filter.Consumables: return t == ItemDrop.ItemData.ItemType.Consumable;
                case Filter.Materials: return t == ItemDrop.ItemData.ItemType.Material;
                case Filter.Ammo: return t == ItemDrop.ItemData.ItemType.Ammo || t == ItemDrop.ItemData.ItemType.AmmoNonEquipable;
                case Filter.Misc:
                    return !Matches(item, Filter.Weapons) && !Matches(item, Filter.Armor) && !Matches(item, Filter.Tools) &&
                           !Matches(item, Filter.Consumables) && !Matches(item, Filter.Materials) && !Matches(item, Filter.Ammo);
                default: return true;
            }
        }

        private void UpdateDetails(InventoryGrid grid)
        {
            var element = _hovered(grid);
            ItemDrop.ItemData item = null;
            if (element != null)
            {
                var p = element.Position;
                item = grid.GetInventory().GetItemAt(p.x, p.y);
            }
            if (item == null || item == _shownItem) return; // the last item stays, like a selection
            _shownItem = item;
            _detailIcon.sprite = item.GetIcon();
            _detailIcon.enabled = true;
            _detailName.text = Localize(item.m_shared.m_name).ToUpperInvariant();
            _detailBody.text = Localize(ItemDrop.ItemData.GetTooltip(item, item.m_quality, false, item.m_worldLevel, -1, false));
        }

        private void UpdateStats(Player player)
        {
            var inventory = player.GetInventory();
            var layout = InventoryModule.Current;
            int ordinary = layout != null ? layout.OrdinarySlots : inventory.GetWidth() * inventory.GetHeight();
            int used = 0;
            foreach (var item in inventory.GetAllItems())
                if (layout == null || layout.IsOrdinary(item.m_gridPos.x, item.m_gridPos.y)) used++;
            if (used != _shownSlots)
            {
                _shownSlots = used;
                _slotsText.SetText("{0}/{1}", used, ordinary);
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

        // ------------------------------------------------------------------ our panels

        private void BuildPanels()
        {
            var t = _theme.Tokens;
            var content = Ui.Child(_area, "Panels");
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(0f, BarsBottom);
            content.offsetMax = new Vector2(0f, -BarsTop);

            _inventoryPanel = Panel(content, "Inventory", 0f, InventoryShare, "$genesisui_panel_inventory");
            _equipmentPanel = Panel(content, "Equipment", InventoryShare, InventoryShare + EquipmentShare, "$genesisui_panel_equipment");
            _detailsPanel = Panel(content, "Details", InventoryShare + EquipmentShare, 1f, "$genesisui_panel_details");

            // The grid fits both axes, including six rows on shorter screens.
            float panelWidth = _area.rect.width * InventoryShare - Gap;
            float panelHeight = _area.rect.height - BarsTop - BarsBottom;
            float byWidth = (panelWidth - 60f - (SlotLayout.Width - 1) * CellGap) / SlotLayout.Width;
            float byHeight = (panelHeight - 80f - 70f - (SlotLayout.MaxRows - 1) * CellGap) / SlotLayout.MaxRows;
            _cell = Mathf.Floor(Mathf.Max(16f, Mathf.Min(64f, Mathf.Min(byWidth, byHeight))));
            float gridWidth = SlotLayout.Width * _cell + (SlotLayout.Width - 1) * CellGap;
            float gridHeight = SlotLayout.MaxRows * _cell + (SlotLayout.MaxRows - 1) * CellGap;
            _gridArea = Ui.Place(Ui.Child(_inventoryPanel, "Grid"), new Vector2(0.5f, 1f), new Vector2(0f, -(HeaderRule + 40f)), new Vector2(gridWidth, gridHeight));
            _gridArea.pivot = new Vector2(0.5f, 1f);

            _slotsText = Label(_inventoryPanel, "Slots", FontRole.Label, 13f, t.TextFlavor, new Vector2(28f, -(HeaderRule + 10f)), new Vector2(200f, 20f), TextAlignmentOptions.Left);

            // The filter: a small field that opens a list of categories.
            var controls = Ui.Child(_controlsArea, "PanelControls");
            controls.anchorMin = Vector2.zero;
            controls.anchorMax = Vector2.one;
            controls.offsetMin = new Vector2(0f, BarsBottom);
            controls.offsetMax = new Vector2(0f, -BarsTop);
            var filter = Ui.Place(Ui.Child(controls, "Filter"), new Vector2(InventoryShare, 1f),
                new Vector2(-24f - Gap / 2f, -(HeaderRule + 6f)), new Vector2(150f, 26f));
            filter.pivot = new Vector2(1f, 1f);
            Frame.Dress(filter, _theme, "keycap_wide", "Windows", 26f);
            _filterText = Ui.Text(filter, "Text", _theme, FontRole.Body, 15f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.Center);
            Ui.Fill((RectTransform)_filterText.transform, 8f, 0f, 8f, 0f);
            Clickable(filter, "inventory filter", ToggleFilterList);
            BuildFilterList(filter);
            ShowFilter();

            var weight = Ui.Child(_inventoryPanel, "Weight");
            weight.anchorMin = new Vector2(0f, 0f);
            weight.anchorMax = new Vector2(1f, 0f);
            weight.pivot = new Vector2(0.5f, 0f);
            weight.offsetMin = new Vector2(30f, 26f);
            weight.offsetMax = new Vector2(-30f, 50f);
            Label(weight, "Label", FontRole.Label, 13f, t.TextFlavor, Vector2.zero, new Vector2(60f, 24f), TextAlignmentOptions.Left, anchor: new Vector2(0f, 0.5f)).text =
                Localize("$genesisui_weight").ToUpperInvariant();
            var track = Ui.Child(weight, "Track");
            track.anchorMin = new Vector2(0f, 0.5f);
            track.anchorMax = new Vector2(1f, 0.5f);
            track.offsetMin = new Vector2(64f, -2f);
            track.offsetMax = new Vector2(-104f, 2f);
            Ui.Image(track, null, new Color(0f, 0f, 0f, 0.55f));
            _weightFill = Ui.Image(Ui.Fill(Ui.Child(track, "Fill")), _theme.Sprite("bar_fill"), ThemeRuntime.ToUnity(t.AccentGold));
            _weightFill.type = Image.Type.Filled;
            _weightFill.fillMethod = Image.FillMethod.Horizontal;
            _weightText = Label(weight, "Value", FontRole.Label, 14f, t.TextTitle, Vector2.zero, new Vector2(100f, 24f), TextAlignmentOptions.Right, anchor: new Vector2(1f, 0.5f));

            // Equipment: total protection (the slots arrive in F4.2b).
            Label(_equipmentPanel, "ArmorLabel", FontRole.Label, 12f, t.TextFlavor, new Vector2(0f, 70f), new Vector2(240f, 18f), TextAlignmentOptions.Center, anchor: new Vector2(0.5f, 0f)).text =
                Localize("$genesisui_armor_total").ToUpperInvariant();
            _armorText = Label(_equipmentPanel, "Armor", FontRole.Display, 26f, t.TextTitle, new Vector2(0f, 34f), new Vector2(160f, 34f), TextAlignmentOptions.Center, anchor: new Vector2(0.5f, 0f));

            // Details: icon, name, and vanilla's own text for the hovered item.
            var iconRt = Ui.Place(Ui.Child(_detailsPanel, "Icon"), new Vector2(0.5f, 1f), new Vector2(0f, -(HeaderRule + 20f)), new Vector2(96f, 96f));
            iconRt.pivot = new Vector2(0.5f, 1f);
            _detailIcon = Ui.Image(iconRt, null, Color.white);
            _detailIcon.preserveAspect = true;
            _detailIcon.enabled = false;
            _detailName = Label(_detailsPanel, "Name", FontRole.Display, 17f, t.AccentGoldBright, new Vector2(0f, -(HeaderRule + 124f)), new Vector2(260f, 24f), TextAlignmentOptions.Center, anchor: new Vector2(0.5f, 1f));
            _detailBody = Ui.Text(_detailsPanel, "Body", _theme, FontRole.Body, 15f, ThemeRuntime.ToUnity(t.TextBody), TextAlignmentOptions.TopLeft);
            _detailBody.textWrappingMode = TextWrappingModes.Normal;
            _detailBody.overflowMode = TextOverflowModes.Ellipsis;
            var body = (RectTransform)_detailBody.transform;
            body.anchorMin = new Vector2(0f, 0f);
            body.anchorMax = new Vector2(1f, 1f);
            body.offsetMin = new Vector2(26f, 30f);
            body.offsetMax = new Vector2(-26f, -(HeaderRule + 156f));
        }

        /// <summary>
        /// A panel over a share of the width: Diego's finest frame without its mid-edge diamonds
        /// (R-048: unnecessary and misplaced), the title above the header rule and the whole tab marker
        /// centred on that rule as its divider.
        /// </summary>
        private RectTransform Panel(RectTransform parent, string name, float from, float to, string titleToken)
        {
            var rt = Ui.Child(parent, name);
            rt.anchorMin = new Vector2(from, 0f);
            rt.anchorMax = new Vector2(to, 1f);
            rt.offsetMin = new Vector2(from > 0f ? Gap / 2f : 0f, 0f);
            rt.offsetMax = new Vector2(to < 1f ? -Gap / 2f : 0f, 0f);
            Frame.Dress(rt, _theme, "window_panel", "Windows");

            float rule = _theme.Inset("window_panel_rule_knot");
            if (rule <= 0f) rule = 38f;
            var title = Label(rt, "Title", FontRole.Display, 16f, _theme.Tokens.AccentGoldBright, new Vector2(0f, -(rule - 26f)), new Vector2(300f, 20f), TextAlignmentOptions.Center, anchor: new Vector2(0.5f, 1f));
            title.characterSpacing = 8f;
            title.text = Localize(titleToken).ToUpperInvariant();

            var marker = _theme.Sprite("tab_marker");
            if (marker != null)
            {
                var mrt = Ui.Child(rt, "Divider");
                mrt.anchorMin = new Vector2(0.2f, 1f);
                mrt.anchorMax = new Vector2(0.8f, 1f);
                mrt.pivot = new Vector2(0.5f, 0.5f);
                mrt.anchoredPosition = new Vector2(0f, -rule);
                mrt.sizeDelta = new Vector2(0f, 8f);
                var img = Ui.Image(mrt, marker, Color.white);
                img.pixelsPerUnitMultiplier = _theme.Size("tab_marker").y / 8f * Frame.CanvasScale(mrt);
            }
            return rt;
        }

        private void BuildFilterList(RectTransform field)
        {
            var t = _theme.Tokens;
            const float row = 24f;
            _filterList = Ui.Place(Ui.Child(field, "List"), new Vector2(0f, 0f), new Vector2(0f, -2f), new Vector2(150f, row * FilterTokens.Length + 12f));
            _filterList.pivot = new Vector2(0f, 1f);
            Frame.Dress(_filterList, _theme, "card", "Windows");
            for (int i = 0; i < FilterTokens.Length; i++)
            {
                var choice = (Filter)i;
                var rt = Ui.Place(Ui.Child(_filterList, "Option " + choice), new Vector2(0f, 1f), new Vector2(10f, -6f - i * row), new Vector2(130f, row));
                rt.pivot = new Vector2(0f, 1f);
                var text = Ui.Text(rt, "Text", _theme, FontRole.Body, 15f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.Left);
                Ui.Fill((RectTransform)text.transform, 6f, 0f, 0f, 0f);
                text.text = Localize(FilterTokens[i]);
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
            GenesisLog.Info("Module:win.inventory", "filter list " + (show ? "opened" : "closed"));
        }

        private void SelectFilter(Filter filter)
        {
            _filter = filter;
            ShowFilter();
            _filterList.gameObject.SetActive(false);
            GenesisLog.Info("Module:win.inventory", "filter selected: " + filter);
        }

        private void ShowFilter() => _filterText.text = Localize(FilterTokens[(int)_filter]) + "  ◆";

        private TextMeshProUGUI Label(RectTransform parent, string name, FontRole role, float size, ColorRgba color, Vector2 position, Vector2 box,
                                     TextAlignmentOptions alignment, Vector2? anchor = null)
        {
            var text = Ui.Fit(Ui.Text(parent, name, _theme, role, size, ThemeRuntime.ToUnity(color), alignment, outlined: true), Mathf.Min(10f, size));
            var a = anchor ?? new Vector2(0f, 1f);
            var rt = Ui.Place((RectTransform)text.transform, a, position, box);
            rt.pivot = a;
            return text;
        }

        private static string Localize(string text) => Localization.instance != null ? Localization.instance.Localize(text) : text;
    }
}
