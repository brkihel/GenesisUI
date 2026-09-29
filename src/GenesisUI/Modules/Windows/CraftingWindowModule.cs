using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Gameplay;
using GenesisUI.Host;
using GenesisUI.InventoryModel;
using GenesisUI.Patches;
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
    /// The Crafting tab, ConceptArt (12) (D-032): GenesisUI's own recipe list (search, categories),
    /// details with the stat table, required materials and the craft button, drawn on the design
    /// board. Vanilla stays the engine: it builds the recipe list (other mods' recipes included),
    /// checks materials and station, crafts, upgrades and repairs. A click on our row presses the
    /// vanilla row's button; Criar, the Criar/Aprimorar tabs, Reparar and the style button press
    /// vanilla's own buttons; the materials shown are the ones vanilla laid out for the recipe.
    /// </summary>
    [GameContract("assembly_valheim", "InventoryGui", "m_availableRecipes")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftButton")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftProgressPanel")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftProgressBar")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftTimer")]
    [GameContract("assembly_valheim", "InventoryGui", "m_tabCraft")]
    [GameContract("assembly_valheim", "InventoryGui", "m_tabUpgrade")]
    [GameContract("assembly_valheim", "InventoryGui", "OnTabCraftPressed")]
    [GameContract("assembly_valheim", "InventoryGui", "OnTabUpgradePressed")]
    [GameContract("assembly_valheim", "InventoryGui", "InCraftTab")]
    [GameContract("assembly_valheim", "InventoryGui", "m_repairButton")]
    [GameContract("assembly_valheim", "InventoryGui", "m_variantButton")]
    [GameContract("assembly_valheim", "InventoryGui", "m_variantDialog")]
    [GameContract("assembly_valheim", "InventoryGui", "m_recipeName")]
    [GameContract("assembly_valheim", "InventoryGui", "m_itemCraftType")]
    [GameContract("assembly_valheim", "InventoryGui", "m_minStationLevelIcon")]
    [GameContract("assembly_valheim", "InventoryGui", "m_minStationLevelText")]
    [GameContract("assembly_valheim", "InventoryGui", "m_recipeRequirementList")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftingStationName")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftingStationLevel")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftingStationLevelRoot")]
    [GameContract("assembly_valheim", "InventoryGui", "m_player")]
    [GameContract("assembly_valheim", "InventoryGui", "m_crafting")]
    [GameContract("assembly_valheim", "InventoryGui", "m_info")]
    [GameContract("assembly_valheim", "InventoryGui", "m_container")]
    [GameContract("assembly_valheim", "Recipe", "m_item")]
    [GameContract("assembly_valheim", "Recipe", "m_amount")]
    [GameContract("assembly_valheim", "Recipe", "m_resources")]
    [GameContract("assembly_valheim", "Piece+Requirement", "m_resItem")]
    [GameContract("assembly_valheim", "Inventory", "CountItems")]
    [GameContract("assembly_guiutils", "GuiBar", "m_maxValue")]
    internal sealed class CraftingWindowModule : IUiModule
    {
        private const string Owner = "module:win.crafting";
        private static readonly string[] NoRegions = new string[0];
        private const float CloseHoldSeconds = 0.6f;

        // ConceptArt (12) in design-board units.
        private const float PanelsTop = 102f, PanelsHeight = 673f;
        private const float PanelX = 284f, PanelW = 750f;
        private const float ListX = 20f, ListY = 202f, ListW = 290f, RowH = 54f, RowGap = 3f;
        private const int VisibleRows = 8;
        private const float CardX = 325f, CardY = 202f, CardW = 407f, CardH = 268f;
        private const float MatY = 506f, MatBox = 88f, MatPitch = 96f;
        private const int MaxMaterials = 4;

        private enum Chip { All, Weapons, Tools, Ammo, Armor, Consumables, Materials }

        private static readonly string[] ChipTokens =
        {
            "$genesisui_filter_all", "$genesisui_filter_weapons", "$genesisui_filter_tools", "$genesisui_filter_ammo",
            "$genesisui_filter_armor", "$genesisui_filter_consumables", "$genesisui_filter_materials",
        };

        private sealed class Entry
        {
            public Recipe Recipe;
            public ItemDrop.ItemData Upgrade;
            public GameObject Element;
            public Button Button;
            public GameObject Selected;
            public bool CanCraft;
            public string Name;
            public string Search;
            public ItemCategory Category;
        }

        private sealed class Row
        {
            public RectTransform Root;
            public Image Icon, Selection;
            public TextMeshProUGUI Name, Type, Level;
            public CanvasGroup Group;
            public Entry Bound;
            public bool ShownSelected;
        }

        private sealed class Material
        {
            public RectTransform Root;
            public Image Icon;
            public TextMeshProUGUI Name, Amount;
            public int Have = -1, Need = -1;
            public string ItemName;
        }

        private readonly VanillaSkin _skin = new VanillaSkin(Owner);
        private readonly List<Entry> _entries = new List<Entry>(128);
        private readonly List<Entry> _shown = new List<Entry>(128);
        private readonly List<Row> _rows = new List<Row>(VisibleRows);
        private readonly List<Material> _materials = new List<Material>(MaxMaterials);
        private readonly List<StatRow> _stats = new List<StatRow>(12);
        private readonly TextMeshProUGUI[] _statLabels = new TextMeshProUGUI[4];
        private readonly TextMeshProUGUI[] _statValues = new TextMeshProUGUI[4];
        private readonly List<TextMeshProUGUI> _chips = new List<TextMeshProUGUI>();

        private ThemeRuntime _theme;
        private WindowParts _parts;
        private RectTransform _root, _area, _panel, _card, _progress;
        private CanvasGroup _fade;
        private TextMeshProUGUI _station, _empty, _cardName, _cardType, _cardDescription, _cardNote, _craftLabel, _tabCraftLabel, _tabUpgradeLabel;
        private Image _cardIcon;
        private GameObject _cardBody, _materialsTitle, _tabCraftRule, _tabUpgradeRule;
        private Button _craft, _repair, _variant, _tabCraft, _tabUpgrade;
        private RectTransform _scrollTrack, _scrollThumb;
        private TMP_InputField _search;
        private IDisposable _typingLease;

        private bool _applied;
        private float _closedFor;
        private int _listVersion = -1;
        private int _scroll;
        private Chip _chip = Chip.All;
        private string _query = "";
        private Entry _selected;
        private bool _filterDirty = true;
        private Transform _guiAncestor;
        private bool _behind;
        private string _shownCraftText;

        private FieldInfo _availableField;
        private PropertyInfo _recipeProp, _itemProp, _elementProp, _canCraftProp;
        private AccessTools.FieldRef<InventoryGui, float> _craftTimer;
        private AccessTools.FieldRef<GuiBar, float> _barMax;

        public string Id => "win.crafting";
        public string NameToken => "$genesisui_module_crafting_window";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f;

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _parts = new WindowParts(_theme);
            _availableField = AccessTools.Field(typeof(InventoryGui), "m_availableRecipes");
            var pair = _availableField.FieldType.GetGenericArguments()[0];
            _recipeProp = AccessTools.Property(pair, "Recipe");
            _itemProp = AccessTools.Property(pair, "ItemData");
            _elementProp = AccessTools.Property(pair, "InterfaceElement");
            _canCraftProp = AccessTools.Property(pair, "CanCraft");
            _craftTimer = AccessTools.FieldRefAccess<InventoryGui, float>("m_craftTimer");
            _barMax = AccessTools.FieldRefAccess<GuiBar, float>("m_maxValue");
            ResetState();
        }

        private void ResetState()
        {
            _applied = false;
            _closedFor = 0f;
            _listVersion = -1;
            _scroll = 0;
            _selected = null;
            _filterDirty = true;
            _guiAncestor = null;
            _behind = false;
            _shownCraftText = null;
            _entries.Clear();
            _shown.Clear();
            EndTyping();
        }

        public void Refresh(float deltaSeconds)
        {
            var gui = InventoryGui.instance;
            var player = Player.m_localPlayer;
            bool on = gui != null && player != null && WindowShellModule.Showing && WindowShellModule.ActiveTab == WindowShellModule.Tab.Crafting;
            if (!on)
            {
                if (_applied)
                {
                    if (gui == null || player == null || InventoryGui.IsVisible() || WindowShellModule.Showing) Unapply();
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
            FollowDialogs(gui);

            if (CraftingListPatch.Version != _listVersion) ReadList(gui);
            if (_filterDirty) Filter();
            UpdateSelection();
            UpdateRows();
            UpdateHeader(gui);
            UpdateDetails(gui, player);
            UpdateMaterials(gui, player);
            UpdateCraftButton(gui);
            if (_search != null && !_search.isFocused && _typingLease != null) EndTyping();
        }

        public void Teardown()
        {
            if (_applied) Unapply();
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            _root = null;
            _rows.Clear();
            _materials.Clear();
            _chips.Clear();
            ResetState();
        }

        // ------------------------------------------------------------------ vanilla engine

        private bool EnsureBuilt(InventoryGui gui)
        {
            if (_root != null) return true;
            _root = WindowCanvas.CreateRoot(gui, "GenesisUI.CraftingWindow", behind: false);
            if (_root == null) return false;
            _fade = _root.gameObject.AddComponent<CanvasGroup>();
            _fade.alpha = 0f;
            Ui.Image(Ui.Fill(Ui.Child(_root, "Dim")), null, new Color(0f, 0f, 0f, 0.35f));
            _area = WindowCanvas.Area(_root, "Board");
            Build();
            _root.gameObject.SetActive(false);
            return true;
        }

        private void Apply(InventoryGui gui)
        {
            _applied = true;
            _root.gameObject.SetActive(true);
            Hide(gui.m_player.gameObject);
            Hide(gui.m_container.gameObject);
            Hide(gui.m_crafting.gameObject);
            Hide(gui.m_info.gameObject);
            _guiAncestor = gui.transform;
            while (_guiAncestor.parent != null && _guiAncestor.parent != _root.parent) _guiAncestor = _guiAncestor.parent;
            _behind = true;
            FollowDialogs(gui);
            _listVersion = -1;
            GenesisLog.Info("Module:win.crafting", "window shown; vanilla panels hidden (" + _skin.Count + " change(s))");
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
            EndTyping();
            _skin.Restore();
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void SetFade(float alpha, bool interactive)
        {
            if (_fade == null) return;
            if (!Mathf.Approximately(_fade.alpha, alpha)) _fade.alpha = alpha;
            _fade.blocksRaycasts = interactive;
            _fade.interactable = interactive;
        }

        /// <summary>Vanilla's style (variant) dialog stays vanilla's: while it is open our window steps behind.</summary>
        private void FollowDialogs(InventoryGui gui)
        {
            bool dialog = gui.m_variantDialog != null && gui.m_variantDialog.gameObject.activeInHierarchy;
            if (dialog == _behind || _guiAncestor == null || _guiAncestor.parent != _root.parent) return;
            _behind = dialog;
            int index = _guiAncestor.GetSiblingIndex();
            int mine = _root.GetSiblingIndex();
            if (dialog) _root.SetSiblingIndex(mine < index ? index - 1 : index);
            else _root.SetSiblingIndex(mine < index ? index : index + 1);
        }

        /// <summary>Re-reads vanilla's recipe list after vanilla rebuilt it (CraftingListPatch).</summary>
        private void ReadList(InventoryGui gui)
        {
            _listVersion = CraftingListPatch.Version;
            _entries.Clear();
            if (_availableField.GetValue(gui) is IList list)
            {
                foreach (var pair in list)
                {
                    var recipe = _recipeProp.GetValue(pair) as Recipe;
                    var element = _elementProp.GetValue(pair) as GameObject;
                    if (recipe == null || recipe.m_item == null || element == null) continue;
                    var data = recipe.m_item.m_itemData;
                    string name = Localize(data.m_shared.m_name);
                    if (recipe.m_amount > 1) name += " x" + recipe.m_amount;
                    var selected = element.transform.Find("selected");
                    _entries.Add(new Entry
                    {
                        Recipe = recipe,
                        Upgrade = _itemProp.GetValue(pair) as ItemDrop.ItemData,
                        Element = element,
                        Button = element.GetComponent<Button>(),
                        Selected = selected != null ? selected.gameObject : null,
                        CanCraft = (bool)_canCraftProp.GetValue(pair),
                        Name = name,
                        Search = name.ToLowerInvariant(),
                        Category = ItemCategories.Of(data),
                    });
                }
            }
            _filterDirty = true;
        }

        private void Filter()
        {
            _filterDirty = false;
            _shown.Clear();
            string q = _query;
            foreach (var e in _entries)
            {
                if (!Matches(e.Category, _chip)) continue;
                if (q.Length > 0 && e.Search.IndexOf(q, StringComparison.Ordinal) < 0) continue;
                _shown.Add(e);
            }
            _scroll = Mathf.Clamp(_scroll, 0, Mathf.Max(0, _shown.Count - VisibleRows));
            _empty.gameObject.SetActive(_shown.Count == 0);
            _empty.text = Localize(_entries.Count == 0 ? "$genesisui_crafting_none" : "$genesisui_crafting_nomatch");
        }

        private static bool Matches(ItemCategory c, Chip chip)
        {
            switch (chip)
            {
                case Chip.Weapons: return c == ItemCategory.Weapon;
                case Chip.Tools: return c == ItemCategory.Tool;
                case Chip.Ammo: return c == ItemCategory.Ammo;
                case Chip.Armor: return c == ItemCategory.Armor || c == ItemCategory.Shield;
                case Chip.Consumables: return c == ItemCategory.Consumable;
                case Chip.Materials: return c == ItemCategory.Material;
                default: return true;
            }
        }

        private void UpdateSelection()
        {
            Entry selected = null;
            foreach (var e in _entries)
                if (e.Selected != null && e.Selected.activeSelf) { selected = e; break; }
            if (selected == _selected) return;
            _selected = selected;
            ShowSelected();
        }

        private void UpdateRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                int index = i + _scroll;
                var entry = index < _shown.Count ? _shown[index] : null;
                if (row.Root.gameObject.activeSelf != (entry != null)) row.Root.gameObject.SetActive(entry != null);
                if (entry == null) { row.Bound = null; continue; }
                if (row.Bound != entry)
                {
                    row.Bound = entry;
                    var data = entry.Recipe.m_item.m_itemData;
                    row.Icon.sprite = data.GetIcon();
                    row.Name.text = entry.Name;
                    row.Type.text = Localize(ItemStats.TypeToken(data));
                    row.Level.text = entry.Upgrade != null ? (entry.Upgrade.m_quality + 1).ToString() : "";
                    row.Group.alpha = entry.CanCraft ? 1f : 0.45f;
                    row.ShownSelected = !(entry == _selected);
                }
                bool sel = entry == _selected;
                if (sel != row.ShownSelected)
                {
                    row.ShownSelected = sel;
                    row.Selection.enabled = sel;
                    row.Name.color = ThemeRuntime.ToUnity(sel ? _theme.Tokens.AccentGoldBright : _theme.Tokens.TextTitle);
                }
            }
            bool bar = _shown.Count > VisibleRows;
            if (_scrollTrack.gameObject.activeSelf != bar) _scrollTrack.gameObject.SetActive(bar);
            if (bar)
            {
                float track = _scrollTrack.sizeDelta.y;
                float thumb = Mathf.Max(24f, track * VisibleRows / _shown.Count);
                _scrollThumb.sizeDelta = new Vector2(_scrollThumb.sizeDelta.x, thumb);
                _scrollThumb.anchoredPosition = new Vector2(-1f, -(track - thumb) * _scroll / Mathf.Max(1, _shown.Count - VisibleRows));
            }
        }

        private void UpdateHeader(InventoryGui gui)
        {
            string station = gui.m_craftingStationName != null ? gui.m_craftingStationName.text : "";
            if (gui.m_craftingStationLevelRoot != null && gui.m_craftingStationLevelRoot.gameObject.activeSelf && gui.m_craftingStationLevel != null)
                station += "  ·  " + Localize("$genesisui_level") + " " + gui.m_craftingStationLevel.text;
            if (_station.text != station) _station.text = station;

            bool craftTab = gui.m_tabCraft != null && gui.m_tabCraft.gameObject.activeSelf;
            bool upgradeTab = gui.m_tabUpgrade != null && gui.m_tabUpgrade.gameObject.activeSelf;
            bool inCraft = gui.InCraftTab();
            SetTab(_tabCraft, _tabCraftLabel, _tabCraftRule, craftTab, inCraft);
            SetTab(_tabUpgrade, _tabUpgradeLabel, _tabUpgradeRule, upgradeTab, !inCraft);

            bool repair = gui.m_repairButton != null && gui.m_repairButton.gameObject.activeSelf;
            if (_repair.gameObject.activeSelf != repair) _repair.gameObject.SetActive(repair);
            if (repair && _repair.interactable != gui.m_repairButton.interactable) _repair.interactable = gui.m_repairButton.interactable;
            bool variant = gui.m_variantButton != null && gui.m_variantButton.gameObject.activeSelf;
            if (_variant.gameObject.activeSelf != variant) _variant.gameObject.SetActive(variant);
        }

        private void SetTab(Button button, TextMeshProUGUI label, GameObject rule, bool visible, bool active)
        {
            if (button.gameObject.activeSelf != visible) button.gameObject.SetActive(visible);
            if (!visible) return;
            if (rule.activeSelf != active) rule.SetActive(active);
            var color = ThemeRuntime.ToUnity(active ? _theme.Tokens.AccentGoldBright : _theme.Tokens.TextFlavor);
            if (label.color != color) label.color = color;
        }

        private void ShowSelected()
        {
            bool any = _selected != null;
            _cardBody.SetActive(any);
            _materialsTitle.SetActive(any);
            foreach (var m in _materials) { m.ItemName = null; m.Have = m.Need = -1; }
            if (!any) return;
            var recipe = _selected.Recipe;
            var data = _selected.Upgrade ?? recipe.m_item.m_itemData;
            int quality = _selected.Upgrade != null ? _selected.Upgrade.m_quality + 1 : 1;
            _cardIcon.sprite = data.GetIcon();
            _cardType.text = Localize(ItemStats.TypeToken(data));
            _cardDescription.text = Localize(data.m_shared.m_description);
            ItemStats.Collect(data, quality, crafting: true, _stats);
            for (int i = 0; i < _statLabels.Length; i++)
            {
                bool show = i < _stats.Count;
                _statLabels[i].transform.parent.gameObject.SetActive(show);
                if (!show) continue;
                _statLabels[i].text = Localize(_stats[i].Token);
                _statValues[i].text = _stats[i].Value;
            }
        }

        private void UpdateDetails(InventoryGui gui, Player player)
        {
            if (_selected == null) return;
            // Name (with the amount) and the upgrade note are vanilla's own texts for this recipe.
            string name = gui.m_recipeName != null ? gui.m_recipeName.text.ToUpperInvariant() : "";
            if (_cardName.text != name) _cardName.text = name;
            string note = "";
            if (gui.m_itemCraftType != null && gui.m_itemCraftType.gameObject.activeSelf) note = gui.m_itemCraftType.text;
            if (gui.m_minStationLevelIcon != null && gui.m_minStationLevelIcon.gameObject.activeSelf && gui.m_minStationLevelText != null)
                note = (note.Length > 0 ? note + "\n" : "") + Localize("$genesisui_station_level") + " " + gui.m_minStationLevelText.text;
            if (_cardNote.text != note) _cardNote.text = note;
        }

        /// <summary>The materials vanilla laid out for the selected recipe, with how many the player has.</summary>
        private void UpdateMaterials(InventoryGui gui, Player player)
        {
            var list = gui.m_recipeRequirementList;
            var inventory = player.GetInventory();
            for (int i = 0; i < _materials.Count; i++)
            {
                var m = _materials[i];
                Transform element = list != null && i < list.Length && list[i] != null ? list[i].transform : null;
                var icon = element != null ? element.Find("res_icon") : null;
                bool show = _selected != null && icon != null && icon.gameObject.activeSelf;
                if (m.Root.gameObject.activeSelf != show) m.Root.gameObject.SetActive(show);
                if (!show) continue;
                var sprite = icon.GetComponent<Image>().sprite;
                if (m.Icon.sprite != sprite)
                {
                    m.Icon.sprite = sprite;
                    m.ItemName = NameFor(sprite);
                    var nameText = element.Find("res_name");
                    m.Name.text = nameText != null ? nameText.GetComponent<TMP_Text>().text : "";
                    m.Have = m.Need = -1;
                }
                var amount = element.Find("res_amount");
                int need = 0;
                if (amount != null) int.TryParse(amount.GetComponent<TMP_Text>().text, out need);
                int have = m.ItemName != null ? inventory.CountItems(m.ItemName) : 0;
                if (have != m.Have || need != m.Need)
                {
                    m.Have = have;
                    m.Need = need;
                    m.Amount.SetText("{0} / {1}", have, need);
                    m.Amount.color = ThemeRuntime.ToUnity(have >= need ? _theme.Tokens.StatePositive : _theme.Tokens.StateDanger);
                }
            }
        }

        /// <summary>The requirement whose icon vanilla shows, to count it in the inventory.</summary>
        private string NameFor(Sprite icon)
        {
            if (_selected == null || _selected.Recipe.m_resources == null) return null;
            foreach (var req in _selected.Recipe.m_resources)
                if (req != null && req.m_resItem != null && req.m_resItem.m_itemData.GetIcon() == icon)
                    return req.m_resItem.m_itemData.m_shared.m_name;
            return null;
        }

        private void UpdateCraftButton(InventoryGui gui)
        {
            var vanilla = gui.m_craftButton;
            bool crafting = gui.m_craftProgressPanel != null && gui.m_craftProgressPanel.gameObject.activeSelf;
            bool interactable = vanilla != null && vanilla.interactable && !crafting && _selected != null;
            if (_craft.interactable != interactable) _craft.interactable = interactable;
            var text = vanilla != null ? vanilla.GetComponentInChildren<TMP_Text>() : null;
            string label = text != null ? text.text : Localize("$inventory_craftbutton");
            if (label != _shownCraftText)
            {
                _shownCraftText = label;
                _craftLabel.text = label.ToUpperInvariant();
            }
            float ratio = 0f;
            if (crafting && gui.m_craftProgressBar != null)
            {
                float max = _barMax(gui.m_craftProgressBar);
                ratio = max > 0f ? Mathf.Clamp01(_craftTimer(gui) / max) : 0f;
            }
            _progress.anchorMax = new Vector2(ratio, 1f);
        }

        // ------------------------------------------------------------------ actions

        private void Select(Row row)
        {
            if (row.Bound == null || row.Bound.Button == null) return;
            row.Bound.Button.onClick.Invoke(); // vanilla's OnSelectedRecipe for its own row
        }

        private void PressCraft()
        {
            var gui = InventoryGui.instance;
            if (gui != null && gui.m_craftButton != null && gui.m_craftButton.interactable) gui.m_craftButton.onClick.Invoke();
        }

        private void SelectChip(Chip chip)
        {
            _chip = chip;
            _scroll = 0;
            _filterDirty = true;
            for (int i = 0; i < _chips.Count; i++)
                _chips[i].color = ThemeRuntime.ToUnity(i == (int)chip ? _theme.Tokens.AccentGoldBright : _theme.Tokens.TextFlavor);
        }

        internal void Scroll(float delta)
        {
            int step = delta > 0f ? -1 : delta < 0f ? 1 : 0;
            _scroll = Mathf.Clamp(_scroll + step, 0, Mathf.Max(0, _shown.Count - VisibleRows));
        }

        private void BeginTyping()
        {
            TextInputFocus.Active = true;
            if (_typingLease == null) _typingLease = InputLeases.Acquire(Owner);
        }

        private void EndTyping()
        {
            TextInputFocus.Active = false;
            _typingLease?.Dispose();
            _typingLease = null;
        }

        // ------------------------------------------------------------------ building

        private void Build()
        {
            var t = _theme.Tokens;
            var panels = WindowCanvas.At(_area, "Panels", 0f, PanelsTop, WindowCanvas.Design.x, PanelsHeight);
            _panel = _parts.Panel(panels, "Crafting", PanelX, 0f, PanelW, PanelsHeight, "$genesisui_panel_crafting", 68f, 26f, TextAlignmentOptions.Left);
            _station = _parts.Label(_panel, "Station", FontRole.Body, 17f, t.TextFlavor, 380f, 18f, 340f, 28f, TextAlignmentOptions.Right);

            // Criar / Aprimorar, like the concept's sub-tabs; Reparar and the style button on the right.
            _tabCraft = Tab(_panel, "TabCraft", 40f, 170f, "$genesisui_tab_craft", () => { var gui = InventoryGui.instance; if (gui != null) gui.OnTabCraftPressed(); }, out _tabCraftLabel, out _tabCraftRule);
            _tabUpgrade = Tab(_panel, "TabUpgrade", 230f, 190f, "$genesisui_tab_upgrade", () => { var gui = InventoryGui.instance; if (gui != null) gui.OnTabUpgradePressed(); }, out _tabUpgradeLabel, out _tabUpgradeRule);
            _repair = _parts.Button(_panel, "Repair", 600f, 62f, 128f, 32f, "$genesisui_repair", 16f, "crafting repair",
                () => { var gui = InventoryGui.instance; if (gui != null && gui.m_repairButton.interactable) gui.m_repairButton.onClick.Invoke(); }, out _);

            // Search and category chips.
            BuildSearch(t);
            float x = 22f;
            for (int i = 0; i < ChipTokens.Length; i++)
            {
                var chip = (Chip)i;
                string text = Localize(ChipTokens[i]);
                var probe = _parts.Label(_panel, "Probe", FontRole.Body, 16f, t.TextTitle, 0f, 0f, 400f, 30f, TextAlignmentOptions.Left);
                float w = Mathf.Ceil(probe.GetPreferredValues(text).x) + 26f;
                UnityEngine.Object.Destroy(probe.gameObject);
                if (x + w > PanelW - 22f) break;
                _parts.Button(_panel, "Chip " + chip, x, 156f, w, 32f, null, 16f, "crafting chip", () => SelectChip(chip), out var label);
                label.text = text;
                _chips.Add(label);
                x += w + 8f;
            }
            SelectChip(Chip.All);

            // The recipe list.
            var list = WindowCanvas.At(_panel, "List", ListX, ListY, ListW, VisibleRows * (RowH + RowGap));
            Ui.Image(list, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            list.gameObject.AddComponent<ListScroll>().Owner = this;
            for (int i = 0; i < VisibleRows; i++) _rows.Add(MakeRow(list, i));
            _empty = _parts.Label(list, "Empty", FontRole.Body, 17f, t.TextFlavor, 10f, 30f, ListW - 20f, 80f, TextAlignmentOptions.Center);
            _empty.textWrappingMode = TextWrappingModes.Normal;
            _scrollTrack = WindowCanvas.At(_panel, "Scroll", ListX + ListW + 4f, ListY, 3f, VisibleRows * (RowH + RowGap) - RowGap);
            Ui.Image(_scrollTrack, null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.25f));
            _scrollThumb = WindowCanvas.At(_scrollTrack, "Thumb", -1f, 0f, 5f, 40f);
            Ui.Image(_scrollThumb, null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.85f));

            BuildCard(t);
            BuildMaterials(t);

            // Variant (style) and the craft button with its progress.
            _variant = _parts.Button(_panel, "Variant", CardX, 612f, 150f, 44f, "$genesisui_style", 17f, "crafting style",
                () => { var gui = InventoryGui.instance; if (gui != null) gui.m_variantButton.onClick.Invoke(); }, out _);
            _craft = _parts.Button(_panel, "Craft", CardX + CardW - 230f, 612f, 230f, 44f, null, 20f, "crafting craft", PressCraft, out _craftLabel);
            _craftLabel.font = _theme.Font(FontRole.Display);
            _craftLabel.characterSpacing = 4f;
            _progress = Ui.Child(_craft.transform, "Progress");
            _progress.anchorMin = Vector2.zero;
            _progress.anchorMax = new Vector2(0f, 1f);
            _progress.offsetMin = new Vector2(3f, 3f);
            _progress.offsetMax = new Vector2(-3f, -3f);
            _progress.SetSiblingIndex(1);
            Ui.Image(_progress, _theme.Sprite("bar_fill"), ThemeRuntime.ToUnity(t.AccentGold).WithA(0.45f));
        }

        private Button Tab(RectTransform parent, string name, float x, float width, string token, Action onClick,
                           out TextMeshProUGUI label, out GameObject rule)
        {
            var rt = WindowCanvas.At(parent, name, x, 60f, width, 36f);
            label = _parts.Label(rt, "Text", FontRole.Display, 17f, _theme.Tokens.TextFlavor, 0f, 0f, width, 32f, TextAlignmentOptions.Center);
            label.characterSpacing = 4f;
            label.text = Localize(token).ToUpperInvariant();
            rule = _parts.Rule(rt, 10f, 36f, width - 20f).gameObject;
            return _parts.Clickable(rt, name, onClick);
        }

        private void BuildSearch(ThemeTokens t)
        {
            var rt = WindowCanvas.At(_panel, "Search", 22f, 108f, 706f, 36f);
            Frame.Dress(rt, _theme, "keycap_wide", "Windows", 36f);
            var viewport = Ui.Fill(Ui.Child(rt, "Viewport"), 16f, 2f, 16f, 2f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var text = _parts.Label(viewport, "Text", FontRole.Body, 17f, t.TextTitle, 0f, 0f, 674f, 32f, TextAlignmentOptions.MidlineLeft);
            Ui.Fill((RectTransform)text.transform);
            var placeholder = _parts.Label(viewport, "Placeholder", FontRole.Body, 17f, t.TextFlavor, 0f, 0f, 674f, 32f, TextAlignmentOptions.MidlineLeft);
            Ui.Fill((RectTransform)placeholder.transform);
            placeholder.text = Localize("$genesisui_search_recipe");
            placeholder.fontStyle = FontStyles.Italic;
            var hit = Ui.Image(rt, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            _search = rt.gameObject.AddComponent<TMP_InputField>();
            _search.textViewport = viewport;
            _search.textComponent = text;
            _search.placeholder = placeholder;
            _search.targetGraphic = hit;
            _search.transition = Selectable.Transition.None;
            _search.characterLimit = 40;
            _search.caretColor = ThemeRuntime.ToUnity(t.AccentGoldBright);
            _search.selectionColor = ThemeRuntime.ToUnity(t.AccentGold).WithA(0.3f);
            _search.onSelect.AddListener(_ => Guard.Try("recipe search focus", BeginTyping));
            _search.onDeselect.AddListener(_ => Guard.Try("recipe search blur", EndTyping));
            _search.onEndEdit.AddListener(_ => Guard.Try("recipe search end", EndTyping));
            _search.onValueChanged.AddListener(value => Guard.Try("recipe search", () =>
            {
                _query = (value ?? "").Trim().ToLowerInvariant();
                _scroll = 0;
                _filterDirty = true;
            }));
        }

        private Row MakeRow(RectTransform list, int i)
        {
            var t = _theme.Tokens;
            var row = new Row { Root = WindowCanvas.At(list, "Row " + i, 0f, i * (RowH + RowGap), ListW, RowH) };
            row.Group = row.Root.gameObject.AddComponent<CanvasGroup>();
            Frame.Dress(row.Root, _theme, "keycap_wide", "Windows", RowH);
            row.Selection = Ui.Image(Ui.Fill(Ui.Child(row.Root, "Selected"), 3f, 3f, 3f, 3f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.14f));
            row.Selection.enabled = false;
            row.Icon = Ui.Image(WindowCanvas.At(row.Root, "Icon", 8f, 6f, 42f, 42f), null, Color.white);
            row.Icon.preserveAspect = true;
            row.Name = _parts.Label(row.Root, "Name", FontRole.Body, 17f, t.TextTitle, 60f, 5f, ListW - 100f, 24f, TextAlignmentOptions.Left);
            row.Type = _parts.Label(row.Root, "Type", FontRole.Body, 14f, t.TextFlavor, 60f, 28f, ListW - 100f, 20f, TextAlignmentOptions.Left);
            row.Level = _parts.Label(row.Root, "Level", FontRole.Display, 16f, t.AccentGoldBright, ListW - 40f, 15f, 30f, 24f, TextAlignmentOptions.Center);
            row.ShownSelected = true;
            _parts.Clickable(row.Root, "crafting recipe", () => Select(row));
            row.Root.gameObject.AddComponent<ListScroll>().Owner = this;
            return row;
        }

        private void BuildCard(ThemeTokens t)
        {
            _card = WindowCanvas.At(_panel, "Card", CardX, CardY, CardW, CardH);
            Frame.Dress(_card, _theme, "card", "Windows");
            _cardBody = WindowCanvas.At(_card, "Body", 0f, 0f, CardW, CardH).gameObject;
            var body = (RectTransform)_cardBody.transform;
            _cardIcon = Ui.Image(WindowCanvas.At(body, "Icon", 14f, 22f, 118f, 150f), null, Color.white);
            _cardIcon.preserveAspect = true;
            _cardName = _parts.Label(body, "Name", FontRole.Display, 22f, t.AccentGoldBright, 146f, 18f, CardW - 160f, 30f, TextAlignmentOptions.Left);
            _cardName.characterSpacing = 3f;
            _cardType = _parts.Label(body, "Type", FontRole.Body, 16f, t.TextFlavor, 146f, 46f, CardW - 160f, 22f, TextAlignmentOptions.Left);
            _cardDescription = _parts.Label(body, "Description", FontRole.Body, 15f, t.TextBody, 146f, 70f, CardW - 162f, 58f, TextAlignmentOptions.TopLeft);
            _cardDescription.textWrappingMode = TextWrappingModes.Normal;
            _cardDescription.overflowMode = TextOverflowModes.Ellipsis;
            _cardDescription.enableAutoSizing = false;
            for (int i = 0; i < _statLabels.Length; i++)
            {
                var row = WindowCanvas.At(body, "Stat " + i, 146f, 134f + i * 26f, CardW - 162f, 24f);
                _statLabels[i] = _parts.Label(row, "Label", FontRole.Body, 16f, t.TextBody, 0f, 0f, (CardW - 162f) * 0.6f, 22f, TextAlignmentOptions.Left);
                _statValues[i] = _parts.Label(row, "Value", FontRole.Body, 16f, t.TextTitle, (CardW - 162f) * 0.4f, 0f, (CardW - 162f) * 0.6f, 22f, TextAlignmentOptions.Right);
                Ui.Image(WindowCanvas.At(row, "Line", 0f, 23f, CardW - 162f, 1f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.12f));
            }
            _cardNote = _parts.Label(body, "Note", FontRole.Body, 15f, t.AccentGold, 14f, 238f, CardW - 28f, 24f, TextAlignmentOptions.Left);
            _cardBody.SetActive(false);
        }

        private void BuildMaterials(ThemeTokens t)
        {
            var title = _parts.Label(_panel, "MaterialsTitle", FontRole.Label, 14f, t.AccentGoldBright, CardX + 14f, MatY - 26f, 380f, 20f, TextAlignmentOptions.Left);
            title.text = Localize("$genesisui_materials_needed").ToUpperInvariant();
            title.characterSpacing = 3f;
            _materialsTitle = title.gameObject;
            for (int i = 0; i < MaxMaterials; i++)
            {
                var m = new Material { Root = WindowCanvas.At(_panel, "Material " + i, CardX + 14f + i * MatPitch, MatY, MatBox, MatBox) };
                Frame.Dress(m.Root, _theme, "hotslot", "Windows", MatBox);
                m.Icon = Ui.Image(WindowCanvas.At(m.Root, "Icon", (MatBox - 40f) / 2f, 8f, 40f, 40f), null, Color.white);
                m.Icon.preserveAspect = true;
                m.Name = _parts.Label(m.Root, "Name", FontRole.Body, 13f, t.TextTitle, 4f, 48f, MatBox - 8f, 18f, TextAlignmentOptions.Center);
                m.Amount = _parts.Label(m.Root, "Amount", FontRole.Display, 14f, t.StatePositive, 4f, 66f, MatBox - 8f, 18f, TextAlignmentOptions.Center);
                m.Root.gameObject.SetActive(false);
                _materials.Add(m);
            }
        }

        private static string Localize(string text) => WindowParts.Localize(text);

        /// <summary>Mouse wheel over the recipe list.</summary>
        private sealed class ListScroll : MonoBehaviour, IScrollHandler
        {
            internal CraftingWindowModule Owner;
            public void OnScroll(PointerEventData e) { if (Owner != null) Owner.Scroll(e.scrollDelta.y); }
        }
    }
}
