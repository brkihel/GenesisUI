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
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Windows
{
    /// <summary>
    /// The Inventory tab in ConceptArt (9)'s layout (F4.2a, D-025): our panels — inventory,
    /// equipment, item details — drawn behind vanilla's window on Jötunn's back canvas, and vanilla's
    /// own grids moved and dressed on top of them with <see cref="VanillaSkin"/>. Every click, drag,
    /// split and equip stays vanilla's; vanilla's panel backgrounds, texts and tooltips are hidden
    /// (never destroyed) and everything returns exactly on teardown, fault or another tab.
    /// </summary>
    [GameContract("assembly_valheim", "InventoryGui", "m_player")]
    [GameContract("assembly_valheim", "InventoryGui", "m_crafting")]
    [GameContract("assembly_valheim", "InventoryGui", "m_info")]
    [GameContract("assembly_valheim", "InventoryGui", "m_container")]
    [GameContract("assembly_valheim", "InventoryGui", "m_playerGrid")]
    [GameContract("assembly_valheim", "InventoryGui", "IsContainerOpen")]
    [GameContract("assembly_valheim", "InventoryGui", "m_container")]
    [GameContract("assembly_valheim", "InventoryGrid", "m_elements")]
    [GameContract("assembly_valheim", "InventoryGrid", "GetHoveredElement")]
    [GameContract("assembly_valheim", "InventoryGrid", "GetInventory")]
    [GameContract("assembly_valheim", "InventoryElement", "m_icon")]
    [GameContract("assembly_valheim", "InventoryElement", "m_tooltip")]
    [GameContract("assembly_valheim", "InventoryElement", "get_Position")]
    [GameContract("assembly_valheim", "Inventory", "GetItemAt")]
    [GameContract("assembly_valheim", "Inventory", "GetTotalWeight")]
    [GameContract("assembly_valheim", "Player", "GetMaxCarryWeight")]
    [GameContract("assembly_valheim", "Player", "GetBodyArmor")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "GetTooltip", Parameters = new[]
        { "ItemDrop+ItemData", "System.Int32", "System.Boolean", "System.Single", "System.Int32", "System.Boolean" })]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_itemType")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData+SharedData", "m_description")]
    internal sealed class InventoryWindowModule : IUiModule
    {
        private static readonly string[] NoRegions = new string[0];

        // Layout on the 1920x1080 design canvas, between the shell's bars.
        private const float Top = 100f, Bottom = 76f, Side = 36f, Gap = 10f;
        private const float InventoryWidth = 900f, EquipmentWidth = 470f;
        private const float Cell = 74f, CellGap = 8f, HeaderHeight = 64f;

        internal enum Filter { All, Weapons, Armor, Tools, Consumables, Materials, Ammo, Misc }

        private static readonly string[] FilterTokens =
        {
            "$genesisui_filter_all", "$genesisui_filter_weapons", "$genesisui_filter_armor", "$genesisui_filter_tools",
            "$genesisui_filter_consumables", "$genesisui_filter_materials", "$genesisui_filter_ammo", "$genesisui_filter_misc",
        };

        private readonly VanillaSkin _skin = new VanillaSkin("module:win.inventory");
        private ThemeRuntime _theme;
        private RectTransform _root;
        private RectTransform _inventoryPanel;
        private RectTransform _gridArea;
        private RectTransform _detailsPanel;
        private TextMeshProUGUI _slotsText;
        private TextMeshProUGUI _weightText;
        private Image _weightFill;
        private TextMeshProUGUI _armorText;
        private TextMeshProUGUI _filterText;
        private Image _detailIcon;
        private TextMeshProUGUI _detailName;
        private TextMeshProUGUI _detailBody;
        private Filter _filter = Filter.All;
        private RectTransform _equipmentPanel;
        private bool _containerShown;
        private bool _applied;
        private InventoryElement _firstElement;
        private int _elementCount;
        private ItemDrop.ItemData _shownItem;
        private int _shownSlots = -1, _shownWeight = -1, _shownMax = -1, _shownArmor = -1;

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

            // Behind vanilla's window: Jötunn's back canvas, so vanilla's slots draw over our panels.
            var back = GUIManager.CustomGUIBack;
            if (back == null) throw new InvalidOperationException("Jötunn's CustomGUIBack is not ready");
            _root = Ui.Fill(Ui.Child(back.transform, "GenesisUI.InventoryWindow"));
            BuildPanels();
            _root.gameObject.SetActive(false);
            _applied = false;
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
            if (!_applied) Apply(gui);

            var grid = gui.m_playerGrid;
            var elements = _elements(grid);
            if (elements.Count != _elementCount || (elements.Count > 0 && elements[0] != _firstElement))
                DressElements(grid, elements); // vanilla rebuilds its elements when the size changes
            ApplyFilter(grid, elements);
            PlaceContainer(gui);
            if (!_containerShown) UpdateDetails(grid);
            UpdateStats(player);
        }

        public void Teardown()
        {
            if (_applied) Unapply();
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            _root = null;
        }

        // ------------------------------------------------------------------ vanilla

        private void Apply(InventoryGui gui)
        {
            _applied = true;
            _root.gameObject.SetActive(true);

            // Vanilla's player panel: its own frame, name, armour and weight texts fade out; the
            // grid stays fully visible through its own group (ignoreParentGroups).
            var panel = _skin.Group(gui.m_player.gameObject);
            panel.alpha = 0f;
            panel.blocksRaycasts = false; // nothing invisible stays clickable
            var gridGroup = _skin.Group(gui.m_playerGrid.gameObject);
            gridGroup.ignoreParentGroups = true;
            gridGroup.alpha = 1f;
            gridGroup.blocksRaycasts = true;
            gridGroup.interactable = true;

            // Crafting and the character info belong to other tabs: hidden and not clickable here.
            Hide(gui.m_crafting.gameObject);
            Hide(gui.m_info.gameObject);

            DressElements(gui.m_playerGrid, _elements(gui.m_playerGrid));
            GenesisLog.Info("Module:win.inventory", "dressed vanilla's inventory (" + _skin.Count + " recorded change(s))");
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
            _skin.Restore();
            _firstElement = null;
            _elementCount = 0;
            _shownItem = null;
            _containerShown = false;
            if (_equipmentPanel != null) _equipmentPanel.gameObject.SetActive(true);
            if (_detailsPanel != null) _detailsPanel.gameObject.SetActive(true);
            if (_root != null) _root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Each vanilla slot of the ordinary rows goes to its cell in our grid area, wears the thin
        /// slot, and loses its tooltip (the details panel shows the item instead). Special rows stay
        /// hidden by the inventory module until their panels exist.
        /// </summary>
        private void DressElements(InventoryGrid grid, List<InventoryElement> elements)
        {
            _elementCount = elements.Count;
            _firstElement = elements.Count > 0 ? elements[0] : null;
            var layout = InventoryModule.Current;
            int rows = layout != null ? layout.Rows : SlotLayout.MinRows;
            var slot = _theme.Sprite("hotslot");

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null) continue;
                var pos = element.Position;
                if (pos.y >= rows) continue;

                var rt = _skin.Rect((RectTransform)element.transform);
                var target = CellWorld(pos.x, pos.y);
                float scale = rt.parent != null ? rt.parent.lossyScale.x : 1f;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(target.width, target.height) / Mathf.Max(0.0001f, scale);
                rt.position = target.center;

                var bg = element.GetComponent<Image>();
                if (bg != null && slot != null)
                {
                    _skin.Image(bg);
                    bg.sprite = slot;
                    bg.type = Image.Type.Sliced;
                    bg.color = Color.white;
                    bg.pixelsPerUnitMultiplier = Frame.CanvasScale(bg.transform) * _theme.Size("hotslot").y / Cell;
                }
                if (element.m_tooltip != null) _skin.Enabled(element.m_tooltip).enabled = false;
            }
        }

        /// <summary>A grid cell in world space (both canvases are screen-space overlays).</summary>
        private Rect CellWorld(int x, int y)
        {
            var corners = new Vector3[4];
            _gridArea.GetWorldCorners(corners);
            float unit = (corners[2].x - corners[0].x) / Mathf.Max(1f, _gridArea.rect.width);
            float cx = corners[1].x + (x * (Cell + CellGap) + Cell / 2f) * unit;
            float cy = corners[1].y - (y * (Cell + CellGap) + Cell / 2f) * unit;
            float size = Cell * unit;
            return new Rect(cx - size / 2f, cy - size / 2f, size, size);
        }

        /// <summary>
        /// A chest, cart or ship: vanilla's container panel (not yet dressed, F4.2 containers) moves over
        /// the equipment and details panels, which step aside while it is open.
        /// </summary>
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
            var corners = new Vector3[4];
            _equipmentPanel.GetWorldCorners(corners);
            var left = corners[1];
            _detailsPanel.GetWorldCorners(corners);
            var right = corners[2];
            var target = new Vector3((left.x + right.x) / 2f, left.y - 20f, 0f);
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
            if (item == null || item == _shownItem) return; // keep the last item shown, like a selection
            _shownItem = item;
            _detailIcon.sprite = item.GetIcon();
            _detailIcon.enabled = true;
            _detailName.text = Localize(item.m_shared.m_name).ToUpperInvariant();
            string tooltip = ItemDrop.ItemData.GetTooltip(item, item.m_quality, false, item.m_worldLevel, -1, false);
            _detailBody.text = Localize(tooltip);
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
            float height = 1080f - Top - Bottom;
            float detailsWidth = 1920f - 2f * Side - InventoryWidth - EquipmentWidth - 2f * Gap;

            _inventoryPanel = Panel("Inventory", Side, InventoryWidth, height, "$genesisui_panel_inventory");
            var equipment = _equipmentPanel = Panel("Equipment", Side + InventoryWidth + Gap, EquipmentWidth, height, "$genesisui_panel_equipment");
            _detailsPanel = Panel("Details", Side + InventoryWidth + EquipmentWidth + 2f * Gap, detailsWidth, height, "$genesisui_panel_details");

            // Inventory: slots in use, filter, grid area, weight.
            _slotsText = Label(_inventoryPanel, "Slots", FontRole.Label, 13f, t.TextFlavor, new Vector2(34f, -58f), new Vector2(200f, 20f), TextAlignmentOptions.Left);
            var filter = Ui.Place(Ui.Child(_inventoryPanel, "Filter"), new Vector2(1f, 1f), new Vector2(-36f, -18f), new Vector2(170f, 30f));
            filter.pivot = new Vector2(1f, 1f);
            Frame.Dress(filter, _theme, "keycap_wide", "Windows", 30f);
            _filterText = Ui.Text(filter, "Text", _theme, FontRole.Body, 16f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.Center);
            Ui.Fill((RectTransform)_filterText.transform, 8f, 0f, 8f, 0f);
            var hit = Ui.Image(Ui.Fill(Ui.Child(filter, "Hit")), null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var button = filter.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Guard.Try("inventory filter", NextFilter));
            ShowFilter();

            int rows = SlotLayout.MaxRows;
            float gridWidth = SlotLayout.Width * Cell + (SlotLayout.Width - 1) * CellGap;
            float gridHeight = rows * Cell + (rows - 1) * CellGap;
            _gridArea = Ui.Place(Ui.Child(_inventoryPanel, "Grid"), new Vector2(0.5f, 1f), new Vector2(0f, -(HeaderHeight + 26f)), new Vector2(gridWidth, gridHeight));
            _gridArea.pivot = new Vector2(0.5f, 1f);

            var weight = Ui.Child(_inventoryPanel, "Weight");
            weight.anchorMin = new Vector2(0f, 0f);
            weight.anchorMax = new Vector2(1f, 0f);
            weight.pivot = new Vector2(0.5f, 0f);
            weight.offsetMin = new Vector2(40f, 34f);
            weight.offsetMax = new Vector2(-40f, 58f);
            Label(weight, "Label", FontRole.Label, 14f, t.TextFlavor, new Vector2(0f, 0f), new Vector2(70f, 24f), TextAlignmentOptions.Left, anchor: new Vector2(0f, 0.5f)).text =
                Localize("$genesisui_weight").ToUpperInvariant();
            var track = Ui.Child(weight, "Track");
            track.anchorMin = new Vector2(0f, 0.5f);
            track.anchorMax = new Vector2(1f, 0.5f);
            track.offsetMin = new Vector2(80f, -3f);
            track.offsetMax = new Vector2(-130f, 3f);
            Ui.Image(track, null, new Color(0f, 0f, 0f, 0.55f));
            _weightFill = Ui.Image(Ui.Fill(Ui.Child(track, "Fill")), _theme.Sprite("bar_fill"), ThemeRuntime.ToUnity(t.AccentGold));
            _weightFill.type = Image.Type.Filled;
            _weightFill.fillMethod = Image.FillMethod.Horizontal;
            _weightText = Label(weight, "Value", FontRole.Label, 15f, t.TextTitle, Vector2.zero, new Vector2(120f, 24f), TextAlignmentOptions.Right, anchor: new Vector2(1f, 0.5f));

            // Equipment: total protection (the slots arrive in F4.2b).
            var armorLabel = Label(equipment, "ArmorLabel", FontRole.Label, 13f, t.TextFlavor, new Vector2(0f, 92f), new Vector2(300f, 20f), TextAlignmentOptions.Center, anchor: new Vector2(0.5f, 0f));
            armorLabel.text = Localize("$genesisui_armor_total").ToUpperInvariant();
            _armorText = Label(equipment, "Armor", FontRole.Display, 30f, t.TextTitle, new Vector2(0f, 50f), new Vector2(200f, 40f), TextAlignmentOptions.Center, anchor: new Vector2(0.5f, 0f));

            // Details: icon, name, vanilla's own tooltip text for the hovered item.
            var iconRt = Ui.Place(Ui.Child(_detailsPanel, "Icon"), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(120f, 120f));
            iconRt.pivot = new Vector2(0.5f, 1f);
            _detailIcon = Ui.Image(iconRt, null, Color.white);
            _detailIcon.preserveAspect = true;
            _detailIcon.enabled = false;
            _detailName = Label(_detailsPanel, "Name", FontRole.Display, 20f, t.AccentGoldBright, new Vector2(0f, -218f), new Vector2(detailsWidth - 60f, 28f), TextAlignmentOptions.Center, anchor: new Vector2(0.5f, 1f));
            _detailBody = Ui.Text(_detailsPanel, "Body", _theme, FontRole.Body, 17f, ThemeRuntime.ToUnity(t.TextBody), TextAlignmentOptions.TopLeft);
            _detailBody.textWrappingMode = TextWrappingModes.Normal;
            _detailBody.overflowMode = TextOverflowModes.Ellipsis;
            var body = (RectTransform)_detailBody.transform;
            body.anchorMin = new Vector2(0f, 0f);
            body.anchorMax = new Vector2(1f, 1f);
            body.offsetMin = new Vector2(34f, 40f);
            body.offsetMax = new Vector2(-34f, -256f);
        }

        private RectTransform Panel(string name, float x, float width, float height, string titleToken)
        {
            var rt = Ui.Place(Ui.Child(_root, name), new Vector2(0f, 1f), new Vector2(x, -Top), new Vector2(width, height));
            rt.pivot = new Vector2(0f, 1f);
            Frame.Dress(rt, _theme, "window_panel", "Windows");
            float k = 1f;
            Frame.Ornament(rt, _theme, "window_panel_rule_knot", Edge.Top, k);
            Frame.Ornament(rt, _theme, "window_panel_bottom_knot", Edge.Bottom, k);
            Frame.Ornament(rt, _theme, "window_panel_knot_left", Edge.Left, k);
            Frame.Ornament(rt, _theme, "window_panel_knot_right", Edge.Right, k);
            var title = Label(rt, "Title", FontRole.Display, 20f, _theme.Tokens.AccentGoldBright, new Vector2(34f, -16f), new Vector2(width - 260f, 26f), TextAlignmentOptions.Left);
            title.characterSpacing = 10f;
            title.text = Localize(titleToken).ToUpperInvariant();
            return rt;
        }

        private TextMeshProUGUI Label(RectTransform parent, string name, FontRole role, float size, ColorRgba color, Vector2 position, Vector2 box,
                                     TextAlignmentOptions alignment, Vector2? anchor = null)
        {
            var text = Ui.Fit(Ui.Text(parent, name, _theme, role, size, ThemeRuntime.ToUnity(color), alignment, outlined: true), Mathf.Min(10f, size));
            var a = anchor ?? new Vector2(0f, 1f);
            var rt = Ui.Place((RectTransform)text.transform, a, position, box);
            rt.pivot = a;
            return text;
        }

        private void NextFilter()
        {
            _filter = (Filter)(((int)_filter + 1) % FilterTokens.Length);
            ShowFilter();
        }

        private void ShowFilter() => _filterText.text = Localize(FilterTokens[(int)_filter]) + "  ◆";

        private static string Localize(string text) => Localization.instance != null ? Localization.instance.Localize(text) : text;
    }
}
