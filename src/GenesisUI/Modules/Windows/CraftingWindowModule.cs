using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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
    /// The Crafting tab (D-032, Diego's layout after R-056): item details on the left — with the
    /// required materials, the reason when it cannot be made, and the craft button — and the crafting
    /// panel on the right with search, categories and two columns, Criar and Aprimorar.
    /// Vanilla stays the engine. Both columns follow vanilla's own rules (the recipes the player
    /// knows here, the items that can be upgraded); picking a row switches vanilla to that mode and
    /// presses vanilla's own row, so the selection, the requirement check and the crafting itself
    /// are vanilla's. Criar, Reparar and Estilo press vanilla's buttons (Shift + Criar makes several,
    /// as in vanilla).
    /// </summary>
    [GameContract("assembly_valheim", "InventoryGui", "Hide")]
    [GameContract("assembly_valheim", "InventoryGui", "IsVisible")]
    [GameContract("assembly_valheim", "InventoryGui", "m_availableRecipes", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.List\u00601[[InventoryGui\u002BRecipeDataPair, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftButton", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftProgressPanel", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Transform")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftProgressBar", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "GuiBar")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftTimer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GameContract("assembly_valheim", "InventoryGui", "m_tabCraft", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "InventoryGui", "m_tabUpgrade", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "InventoryGui", "OnTabCraftPressed")]
    [GameContract("assembly_valheim", "InventoryGui", "OnTabUpgradePressed")]
    [GameContract("assembly_valheim", "InventoryGui", "InCraftTab")]
    [GameContract("assembly_valheim", "InventoryGui", "m_repairButton", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "InventoryGui", "m_variantButton", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Button")]
    [GameContract("assembly_valheim", "InventoryGui", "m_variantDialog", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "VariantDialog")]
    [GameContract("assembly_valheim", "InventoryGui", "m_recipeName", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "TMPro.TMP_Text")]
    [GameContract("assembly_valheim", "InventoryGui", "m_itemCraftType", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "TMPro.TMP_Text")]
    [GameContract("assembly_valheim", "InventoryGui", "m_recipeRequirementList", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.GameObject[]")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftingStationName", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "TMPro.TMP_Text")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftingStationLevel", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "TMPro.TMP_Text")]
    [GameContract("assembly_valheim", "InventoryGui", "m_craftingStationLevelRoot", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "InventoryGui", "m_player", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "InventoryGui", "m_crafting", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "InventoryGui", "m_info", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "InventoryGui", "m_container", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "Player", "GetAvailableRecipes")]
    [GameContract("assembly_valheim", "Player", "HaveRequirements", Parameters = new[] { "Recipe", "System.Boolean", "System.Int32", "System.Int32" })]
    [GameContract("assembly_valheim", "Player", "GetCurrentCraftingStation")]
    [GameContract("assembly_valheim", "CraftingStation", "m_name", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GameContract("assembly_valheim", "CraftingStation", "m_upgrader", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GameContract("assembly_valheim", "CraftingStation", "m_name", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GameContract("assembly_valheim", "CraftingStation", "GetLevel")]
    [GameContract("assembly_valheim", "Recipe", "m_item", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop")]
    [GameContract("assembly_valheim", "Recipe", "m_amount", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "Recipe", "m_resources", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Piece\u002BRequirement[]")]
    [GameContract("assembly_valheim", "Recipe", "m_noCraftOnlyUpgrade", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GameContract("assembly_valheim", "Recipe", "GetRequiredStation")]
    [GameContract("assembly_valheim", "Recipe", "GetRequiredStationLevel")]
    [GameContract("assembly_valheim", "Piece+Requirement", "m_resItem", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop")]
    [GameContract("assembly_valheim", "Piece+Requirement", "m_upgraderResource", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GameContract("assembly_valheim", "Inventory", "CountItems")]
    [GameContract("assembly_valheim", "Inventory", "GetAllItems", Parameters = new[] { "System.String", "System.Collections.Generic.List`1[[ItemDrop+ItemData, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]" })]
    [GameContract("assembly_valheim", "ZoneSystem", "GetGlobalKey", Parameters = new[] { "GlobalKeys" })]
    [GameContract("assembly_guiutils", "GuiBar", "m_maxValue", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "InventoryGui", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "InventoryGui")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Player")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ZoneSystem", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "ZoneSystem")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Humanoid", "GetInventory", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Inventory")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop", "m_itemData", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_shared", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "ItemDrop\u002BItemData\u002BSharedData")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData\u002BSharedData", "m_maxQuality", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData\u002BSharedData", "m_name", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "m_quality", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData", "GetIcon", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Sprite")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "ItemDrop\u002BItemData\u002BSharedData", "m_description", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Host.ModuleContext), typeof(GenesisUI.Widgets.WindowParts), typeof(GenesisUI.Widgets.WindowCanvas), typeof(GenesisUI.Patches.CraftingListPatch), typeof(GenesisUI.Widgets.Backdrop), typeof(GenesisUI.Modules.Windows.VanillaPanels), typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.Collections.ListOrder), typeof(GenesisUI.Gameplay.ItemCategories), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Widgets.ItemStats), typeof(GenesisUI.Widgets.OneShotLight), typeof(GenesisUI.Modules.Windows.RequirementBindings), typeof(GenesisUI.Widgets.UiSound), typeof(GenesisUI.Widgets.EdgeLight), typeof(GenesisUI.Widgets.EmberField), typeof(GenesisUI.Patches.TextInputFocus), typeof(GenesisUI.Foundation.InputLeases), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Widgets.ColorExtensions), typeof(GenesisUI.Widgets.Frame), typeof(GenesisUI.Foundation.Guard))]
    [GameContract("assembly_guiutils", "Localization", "get_instance", Parameters = new string[0], ValueType = "Localization")]
    [GameContract("assembly_guiutils", "Localization", "GetSelectedLanguage", Parameters = new string[0], ValueType = "System.String")]
    [GameContract("assembly_valheim", "InventoryGui+RecipeDataPair", "get_CanCraft", Parameters = new string[0])]
    internal sealed class CraftingWindowModule : IUiModule, IRecoverable
    {
        /// <summary>IRecoverable: on a fault the windows close (vanilla's never shows in their place).</summary>
        public void CloseVanillaWindow()
        {
            var gui = InventoryGui.instance;
            if (gui != null && InventoryGui.IsVisible()) gui.Hide();
        }

        private const string Owner = "module:win.crafting";
        private static readonly string[] NoRegions = new string[0];
        private const float CloseHoldSeconds = 0.6f;
        private const float RecomputeSeconds = 1f;

        // Design-board units (WindowCanvas.Design), panels between the bars like every window.
        private const float PanelsTop = 102f, PanelsHeight = 673f;
        private const float DetailsW = 480f, PanelX = 484f, PanelW = 1096f;
        private const float ColumnY = 196f, RowH = 54f, RowGap = 4f;
        private const float ColumnW = 512f, LeftX = 22f, RightX = 562f;
        private const int VisibleRows = 7;
        private const int MaxStats = 6, MaxMaterials = 5;

        private enum Chip { All, Weapons, Tools, Ammo, Armor, Consumables, Materials }

        private static readonly string[] ChipTokens =
        {
            "$genesisui_filter_all", "$genesisui_filter_weapons", "$genesisui_filter_tools", "$genesisui_filter_ammo",
            "$genesisui_filter_armor", "$genesisui_filter_consumables", "$genesisui_filter_materials",
        };

        private sealed class Entry
        {
            public string NameToken;
            public int Amount;
            public Recipe Recipe;
            public ItemDrop.ItemData Upgrade;
            public bool CanCraft;
            public bool JustReady;
            public string Name;
            public string Search;
            public ItemCategory Category;
        }

        private sealed class Row
        {
            public RectTransform Root;
            public Image Icon, Selection;
            public TextMeshProUGUI Name, Sub, Right;
            public CanvasGroup Group;
            public OneShotLight Shine;
            public Entry Bound;
            public int ShownState = -1;
        }

        private sealed class Column
        {
            public bool Upgrade;
            public readonly List<Entry> All = new List<Entry>(128);
            public readonly List<Entry> Shown = new List<Entry>(128);
            public readonly List<Row> Rows = new List<Row>(VisibleRows);
            public int Scroll;
            public RectTransform Track, Thumb;
            public TextMeshProUGUI Empty, Title;
            public bool Available = true;
        }

        /// <summary>One of vanilla's recipe rows, cached when vanilla rebuilds its list.</summary>
        private struct VanillaRow
        {
            public Recipe Recipe;
            public ItemDrop.ItemData Item;
            public GameObject Mark;
            public bool CanCraft;
            public Button Button;
        }

        private sealed class Material
        {
            public RectTransform Root;
            public Image Icon;
            public TextMeshProUGUI Name, Amount;
            public int Have = -1, Need = -1;
            public string ItemName, Label;
            public Transform NativeRoot;
            public Image NativeIcon;
            public TMP_Text NativeName, NativeAmount;
            public string ShownAmount;
        }

        private readonly Column _craftColumn = new Column { Upgrade = false };
        private readonly Column _upgradeColumn = new Column { Upgrade = true };
        private readonly Column[] _columns;
        private readonly Dictionary<(Recipe, ItemDrop.ItemData), Entry> _entryCache = new Dictionary<(Recipe, ItemDrop.ItemData), Entry>();
        private readonly List<(Recipe, ItemDrop.ItemData)> _retiredEntries = new List<(Recipe, ItemDrop.ItemData)>();
        private string _language;
        public CraftingWindowModule() { _columns = new[] { _craftColumn, _upgradeColumn }; }
        private readonly List<Material> _materials = new List<Material>(MaxMaterials);
        private readonly List<StatRow> _stats = new List<StatRow>(12);
        private readonly List<StatRow> _next = new List<StatRow>(12);
        private readonly TextMeshProUGUI[] _statLabels = new TextMeshProUGUI[MaxStats];
        private readonly TextMeshProUGUI[] _statValues = new TextMeshProUGUI[MaxStats];
        private readonly List<TextMeshProUGUI> _chips = new List<TextMeshProUGUI>();
        private List<Recipe> _recipes = new List<Recipe>(256); // by ref: Player.GetAvailableRecipes
        private readonly List<ItemDrop.ItemData> _owned = new List<ItemDrop.ItemData>(16);
        private readonly StringBuilder _reason = new StringBuilder(128);
        private readonly List<VanillaRow> _vanillaRows = new List<VanillaRow>(128);
        private int _vanillaVersion = -1;

        private ThemeRuntime _theme;
        private WindowParts _parts;
        private RectTransform _root, _area, _details, _panel, _progress;
        private CanvasGroup _fade;
        private TextMeshProUGUI _station, _name, _type, _description, _reasonText, _craftLabel, _detailsEmpty;
        private TextMeshProUGUI _socketWarning;
        private Image _icon;
        private GameObject _detailsBody;
        private Button _craft, _repair, _variant, _socketTab, _normalTab, _editSockets;
        private bool _socketMode;
        // Light effects: the craft's progress around the button; embers behind the details at a forge.
        private EdgeLight _craftLight;
        private EmberField _forgeEmbers;
        private CraftingStation _shownStation;
        private float _craftRatio;
        private TMP_InputField _search;
        private IDisposable _typingLease;

        private bool _applied;
        private float _closedFor, _recomputeIn;
        private int _listVersion = -1;
        private Chip _chip = Chip.All;
        private string _query = "";
        private bool _filterDirty = true;
        private Recipe _selRecipe;
        private ItemDrop.ItemData _selItem;
        private bool _selValid, _detailsDirty = true;
        private Transform _guiAncestor;
        private bool _behind;
        private string _shownCraftText, _shownReason;

        private FieldInfo _availableField;
        private PropertyInfo _recipeProp, _itemProp, _elementProp, _canCraftProp;
        private AccessTools.FieldRef<InventoryGui, float> _craftTimer;
        private AccessTools.FieldRef<GuiBar, float> _barMax;

        public string Id => "win.crafting";
        public string NameToken => "$genesisui_module_crafting_window";
        public IReadOnlyList<string> Regions => new[] { Id };
        public float RefreshRate => 0f;

        /// <summary>
        /// While this window module runs (built, not torn down or faulted): with the inventory and the
        /// crafting windows both running, the window shell keeps vanilla's panels hidden at all times (D-038).
        /// </summary>
        internal static bool Running { get; private set; }

        public void Build(ModuleContext context)
        {
            Running = true;
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
            _recomputeIn = 0f;
            _listVersion = -1;
            _vanillaVersion = -1;
            _vanillaRows.Clear();
            _filterDirty = true;
            _detailsDirty = true;
            _selRecipe = null;
            _selItem = null;
            _selValid = false;
            _guiAncestor = null;
            _behind = false;
            _shownCraftText = _shownReason = null;
            _shownStation = null;
            foreach (var c in _columns) { c.All.Clear(); c.Shown.Clear(); c.Scroll = 0; }
            _entryCache.Clear(); _retiredEntries.Clear(); _language = null;
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

            // Our two lists follow vanilla's rules; re-read them when vanilla rebuilt its list and
            // once a second (materials picked up or spent change what can be made).
            _recomputeIn -= deltaSeconds;
            if (CraftingListPatch.Version != _vanillaVersion) CacheVanillaRows(gui);
            if (CraftingListPatch.Version != _listVersion || _recomputeIn <= 0f) Recompute(gui, player);
            if (_filterDirty) Filter();
            ReadSelection(gui);
            UpdateColumn(_craftColumn);
            UpdateColumn(_upgradeColumn);
            UpdateHeader(gui);
            if (_detailsDirty) ShowDetails();
            UpdateMaterials(gui, player);
            UpdateAction(gui, player);
            if (_search != null && !_search.isFocused && _typingLease != null) EndTyping();
        }

        public void Teardown()
        {
            Running = false;
            if (_applied) Unapply();
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            _root = null;
            _craftColumn.Rows.Clear();
            _upgradeColumn.Rows.Clear();
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
            Backdrop.Create(_root, _theme);
            _area = WindowCanvas.Area(_root, "Board");
            Build();
            _root.gameObject.SetActive(false);
            return true;
        }

        private void Apply(InventoryGui gui)
        {
            _applied = true;
            _root.gameObject.SetActive(true);
            VanillaPanels.Hold("win.crafting", gui);
            _guiAncestor = gui.transform;
            while (_guiAncestor.parent != null && _guiAncestor.parent != _root.parent) _guiAncestor = _guiAncestor.parent;
            _behind = true;
            FollowDialogs(gui);
            _listVersion = -1;
            _vanillaVersion = -1;
            _detailsDirty = true;
            GenesisLog.Info("Module:win.crafting", "window shown; vanilla panels hidden (" + VanillaPanels.HolderCount + " window(s) holding them)");
        }

        private void Unapply()
        {
            _applied = false;
            VanillaPanels.Release("win.crafting");
            _closedFor = 0f;
            EndTyping();
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void SetFade(float alpha, bool interactive)
        {
            if (_fade == null) return;
            if (!Mathf.Approximately(_fade.alpha, alpha)) _fade.alpha = alpha;
            _fade.blocksRaycasts = interactive;
            _fade.interactable = interactive;
        }

        /// <summary>Vanilla's style dialog stays vanilla's: while it is open our window steps behind.</summary>
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

        /// <summary>
        /// Both columns with vanilla's rules (InventoryGui.UpdateRecipeList): Criar lists the known
        /// recipes that are not upgrade-only; Aprimorar lists the player's items of an upgradable
        /// recipe below their maximum quality (at an upgrader station, those with an upgrader resource).
        /// </summary>
        private void Recompute(InventoryGui gui, Player player)
        {
            string language = Localization.instance != null ? Localization.instance.GetSelectedLanguage() : "";
            if (_language != language) { _language = language; _entryCache.Clear(); }
            _listVersion = CraftingListPatch.Version;
            _recomputeIn = RecomputeSeconds;
            bool noCost = ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost);
            var station = player.GetCurrentCraftingStation();
            bool upgrader = station != null && station.m_upgrader;
            _recipes.Clear();
            player.GetAvailableRecipes(ref _recipes);

            _craftColumn.Available = gui.m_tabCraft != null && gui.m_tabCraft.gameObject.activeSelf;
            _upgradeColumn.Available = gui.m_tabUpgrade != null && gui.m_tabUpgrade.gameObject.activeSelf;
            _craftColumn.All.Clear();
            _upgradeColumn.All.Clear();
            var inventory = player.GetInventory();
            _socketMode = Adapters.Jewelcrafting.JewelcraftingAdapter.SocketMode;
            if (_socketMode)
            {
                _craftColumn.Available = true; _upgradeColumn.Available = false;
                foreach (var row in _vanillaRows)
                    if (row.Recipe != null && row.Item != null) _craftColumn.All.Add(NewEntry(row.Recipe, row.Item, row.CanCraft));
            }
            else foreach (var recipe in _recipes)
            {
                if (recipe == null || recipe.m_item == null) continue;
                var data = recipe.m_item.m_itemData;
                if (_craftColumn.Available && !recipe.m_noCraftOnlyUpgrade)
                    _craftColumn.All.Add(NewEntry(recipe, null, player.HaveRequirements(recipe, false, 1, 1) || noCost));
                if (!_upgradeColumn.Available || data.m_shared.m_maxQuality <= 1) continue;
                _owned.Clear();
                inventory.GetAllItems(data.m_shared.m_name, _owned);
                foreach (var item in _owned)
                {
                    if (upgrader)
                    {
                        bool hasUpgraderResource = false;
                        foreach (var req in recipe.m_resources)
                            if (req != null && req.m_upgraderResource) { hasUpgraderResource = true; break; }
                        if (!hasUpgraderResource) continue;
                    }
                    else if (item.m_quality >= item.m_shared.m_maxQuality) continue;
                    bool can = (item.m_quality < item.m_shared.m_maxQuality || upgrader || noCost) &&
                               (player.HaveRequirements(recipe, false, item.m_quality + 1, 1) || noCost);
                    _upgradeColumn.All.Add(NewEntry(recipe, item, can));
                }
            }
            _craftColumn.Title.text = Localize(_socketMode ? "$genesisui_sockets" : "$genesisui_tab_craft").ToUpperInvariant();
            // What can be made now first; vanilla's order otherwise (a stable sort).
            GenesisUI.Collections.ListOrder.StablePartition(_craftColumn.All, CanCraftFirst, _partitionBuffer);
            GenesisUI.Collections.ListOrder.StablePartition(_upgradeColumn.All, CanCraftFirst, _partitionBuffer);
            FindJustReady();
            _retiredEntries.Clear();
            foreach (var entry in _entryCache) if (!_listed.Contains(entry.Key)) _retiredEntries.Add(entry.Key);
            foreach (var key in _retiredEntries) _entryCache.Remove(key);
            foreach (var column in _columns) foreach (var row in column.Rows) row.ShownState = -1;
            _detailsDirty = true;
            _filterDirty = true;
        }

        private readonly List<Entry> _partitionBuffer = new List<Entry>(128);

        // A recipe that has just become craftable shines once (Diego, 2026-10-02). Only one listed at the
        // previous check and not craftable then: a new station's list or the session's first check
        // shine nothing. Kept across openings, so what became craftable while away shines on return.
        private const float ShineWithinSeconds = 4f;
        private HashSet<(Recipe, ItemDrop.ItemData)> _listed = new HashSet<(Recipe, ItemDrop.ItemData)>();
        private HashSet<(Recipe, ItemDrop.ItemData)> _craftable = new HashSet<(Recipe, ItemDrop.ItemData)>();
        private HashSet<(Recipe, ItemDrop.ItemData)> _nextListed = new HashSet<(Recipe, ItemDrop.ItemData)>();
        private HashSet<(Recipe, ItemDrop.ItemData)> _nextCraftable = new HashSet<(Recipe, ItemDrop.ItemData)>();
        private readonly Dictionary<(Recipe, ItemDrop.ItemData), float> _readySince = new Dictionary<(Recipe, ItemDrop.ItemData), float>();
        private readonly List<(Recipe, ItemDrop.ItemData)> _expired = new List<(Recipe, ItemDrop.ItemData)>();

        /// <summary>Marks the entries that became craftable since the previous check (see <see cref="ShineWithinSeconds"/>).</summary>
        private void FindJustReady()
        {
            float now = Time.unscaledTime;
            _nextListed.Clear();
            _nextCraftable.Clear();
            foreach (var column in _columns)
                foreach (var e in column.All)
                {
                    var key = (e.Recipe, e.Upgrade);
                    _nextListed.Add(key);
                    if (!e.CanCraft) continue;
                    _nextCraftable.Add(key);
                    if (_listed.Contains(key) && !_craftable.Contains(key) && !_readySince.ContainsKey(key)) _readySince[key] = now;
                }
            _expired.Clear();
            foreach (var pair in _readySince)
                if (now - pair.Value > ShineWithinSeconds || !_nextCraftable.Contains(pair.Key)) _expired.Add(pair.Key);
            foreach (var key in _expired) _readySince.Remove(key);
            foreach (var column in _columns)
                foreach (var e in column.All)
                    e.JustReady = e.CanCraft && _readySince.ContainsKey((e.Recipe, e.Upgrade));
            (_listed, _nextListed) = (_nextListed, _listed);
            (_craftable, _nextCraftable) = (_nextCraftable, _craftable);
        }
        private static readonly Func<Entry, bool> CanCraftFirst = e => e.CanCraft;

        private Entry NewEntry(Recipe recipe, ItemDrop.ItemData upgrade, bool canCraft)
        {
            var key = (recipe, upgrade);
            var data = recipe.m_item.m_itemData;
            bool exists = _entryCache.TryGetValue(key, out var cached);
            var category = ItemCategories.Of(data);
            if (exists && cached.NameToken == data.m_shared.m_name && cached.Amount == recipe.m_amount && cached.Category == category)
            { cached.CanCraft = canCraft; return cached; }
            string name = Localize(data.m_shared.m_name);
            if (upgrade == null && recipe.m_amount > 1) name += " x" + recipe.m_amount;
            var entry = new Entry
            {
                Recipe = recipe, Upgrade = upgrade, CanCraft = canCraft, Name = name,
                Search = name.ToLowerInvariant(), Category = category, NameToken = data.m_shared.m_name, Amount = recipe.m_amount,
            };
            _entryCache[key] = entry;
            return entry;
        }

        private void Filter()
        {
            _filterDirty = false;
            foreach (var column in _columns)
            {
                column.Shown.Clear();
                foreach (var e in column.All)
                {
                    if (!Matches(e.Category, _chip)) continue;
                    if (_query.Length > 0 && e.Search.IndexOf(_query, StringComparison.Ordinal) < 0) continue;
                    column.Shown.Add(e);
                }
                column.Scroll = Mathf.Clamp(column.Scroll, 0, Mathf.Max(0, column.Shown.Count - VisibleRows));
                string empty = !column.Available
                    ? (column.Upgrade ? "$genesisui_upgrade_unavailable" : "$genesisui_craft_unavailable")
                    : column.All.Count == 0
                        ? (column.Upgrade ? "$genesisui_upgrade_none" : "$genesisui_crafting_none")
                        : "$genesisui_crafting_nomatch";
                column.Empty.text = Localize(empty);
                column.Empty.gameObject.SetActive(column.Shown.Count == 0);
            }
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

        /// <summary>Vanilla's rows, read through reflection only when vanilla rebuilt them (no per-frame boxing).</summary>
        private void CacheVanillaRows(InventoryGui gui)
        {
            _vanillaVersion = CraftingListPatch.Version;
            _vanillaRows.Clear();
            if (!(_availableField.GetValue(gui) is IList list)) return;
            foreach (var pair in list)
            {
                var element = _elementProp.GetValue(pair) as GameObject;
                if (element == null) continue;
                var mark = element.transform.Find("selected");
                _vanillaRows.Add(new VanillaRow
                {
                    Recipe = _recipeProp.GetValue(pair) as Recipe,
                    Item = _itemProp.GetValue(pair) as ItemDrop.ItemData,
                    Mark = mark != null ? mark.gameObject : null,
                    Button = element.GetComponent<Button>(),
                    CanCraft = (bool)_canCraftProp.GetValue(pair),
                });
            }
        }

        /// <summary>The recipe vanilla has selected (its row's "selected" mark), which the details show.</summary>
        private void ReadSelection(InventoryGui gui)
        {
            Recipe recipe = null;
            ItemDrop.ItemData item = null;
            bool valid = false;
            for (int i = 0; i < _vanillaRows.Count; i++)
            {
                var row = _vanillaRows[i];
                if (row.Mark == null || !row.Mark.activeSelf || row.Recipe == null) continue;
                recipe = row.Recipe;
                item = row.Item;
                valid = true;
                break;
            }
            if (recipe == _selRecipe && item == _selItem && valid == _selValid) return;
            _selRecipe = recipe;
            _selItem = item;
            _selValid = valid;
            _detailsDirty = true;
        }

        private bool IsSelected(Entry e) => _selValid && e.Recipe == _selRecipe && e.Upgrade == _selItem;

        private void UpdateColumn(Column column)
        {
            for (int i = 0; i < column.Rows.Count; i++)
            {
                var row = column.Rows[i];
                int index = i + column.Scroll;
                var entry = index < column.Shown.Count ? column.Shown[index] : null;
                if (row.Root.gameObject.activeSelf != (entry != null)) row.Root.gameObject.SetActive(entry != null);
                if (entry == null) { row.Bound = null; continue; }
                bool sel = IsSelected(entry);
                int state = (sel ? 1 : 0) | (entry.CanCraft ? 2 : 0);
                if (row.Bound == entry && row.ShownState == state) continue;
                row.Bound = entry;
                row.ShownState = state;
                var data = entry.Recipe.m_item.m_itemData;
                row.Icon.sprite = entry.Upgrade != null ? entry.Upgrade.GetIcon() : data.GetIcon();
                row.Name.text = entry.Name;
                row.Name.color = ThemeRuntime.ToUnity(sel ? _theme.Tokens.AccentGoldBright : _theme.Tokens.TextTitle);
                if (entry.CanCraft) row.Sub.text = Localize(ItemStats.TypeToken(data));
                else row.Sub.text = "<color=#B5613F>" + Localize("$genesisui_missing_materials") + "</color>";
                row.Right.text = !_socketMode && entry.Upgrade != null ? Localize("$genesisui_level") + " " + entry.Upgrade.m_quality + " → " + (entry.Upgrade.m_quality + 1) : "";
                row.Group.alpha = entry.CanCraft ? 1f : 0.55f;
                row.Selection.enabled = sel;
                if (entry.JustReady && row.Shine != null)
                {
                    // Once: the next check's entry is not marked again.
                    entry.JustReady = false;
                    _readySince.Remove((entry.Recipe, entry.Upgrade));
                    row.Shine.Play();
                }
            }
            bool bar = column.Shown.Count > VisibleRows;
            if (column.Track.gameObject.activeSelf != bar) column.Track.gameObject.SetActive(bar);
            if (bar)
            {
                float track = column.Track.sizeDelta.y;
                float thumb = Mathf.Max(24f, track * VisibleRows / column.Shown.Count);
                column.Thumb.sizeDelta = new Vector2(column.Thumb.sizeDelta.x, thumb);
                column.Thumb.anchoredPosition = new Vector2(-1f, -(track - thumb) * column.Scroll / Mathf.Max(1, column.Shown.Count - VisibleRows));
            }
        }

        private void UpdateHeader(InventoryGui gui)
        {
            string station = gui.m_craftingStationName != null ? gui.m_craftingStationName.text : "";
            if (gui.m_craftingStationLevelRoot != null && gui.m_craftingStationLevelRoot.gameObject.activeSelf && gui.m_craftingStationLevel != null)
                station += "  ·  " + Localize("$genesisui_level") + " " + gui.m_craftingStationLevel.text;
            if (_station.text != station) _station.text = station;
            bool repair = gui.m_repairButton != null && gui.m_repairButton.gameObject.activeSelf;
            if (_repair.gameObject.activeSelf != repair) _repair.gameObject.SetActive(repair);
            if (repair && _repair.interactable != gui.m_repairButton.interactable) _repair.interactable = gui.m_repairButton.interactable;
            bool available = Adapters.Jewelcrafting.JewelcraftingAdapter.Available;
            if (_socketTab.gameObject.activeSelf != available) _socketTab.gameObject.SetActive(available);
            if (_normalTab.gameObject.activeSelf != available) _normalTab.gameObject.SetActive(available);
            bool socketMode = Adapters.Jewelcrafting.JewelcraftingAdapter.SocketMode;
            if (_socketTab.interactable == socketMode) _socketTab.interactable = !socketMode;
            if (_normalTab.interactable != socketMode) _normalTab.interactable = socketMode;
            bool edit = Adapters.Jewelcrafting.JewelcraftingAdapter.CanEditSelected;
            if (_editSockets.gameObject.activeSelf != edit) _editSockets.gameObject.SetActive(edit);
            bool variant = !Adapters.Jewelcrafting.JewelcraftingAdapter.CanEditSelected && gui.m_variantButton != null && gui.m_variantButton.gameObject.activeSelf;
            if (_variant.gameObject.activeSelf != variant) _variant.gameObject.SetActive(variant);
        }

        /// <summary>The selected item: name, type, description and the stat table (current → next for an upgrade).</summary>
        private void ShowDetails()
        {
            _detailsDirty = false;
            _detailsBody.SetActive(_selValid);
            _detailsEmpty.gameObject.SetActive(!_selValid);
            foreach (var m in _materials) { m.ItemName = null; m.Have = m.Need = -1; }
            if (!_selValid) return;
            var data = _selItem ?? _selRecipe.m_item.m_itemData;
            int quality = _selItem != null ? _selItem.m_quality + (_socketMode ? 0 : 1) : 1;
            _icon.sprite = data.GetIcon();
            string name = Localize(data.m_shared.m_name);
            if (_selItem == null && _selRecipe.m_amount > 1) name += " x" + _selRecipe.m_amount;
            _name.text = name.ToUpperInvariant();
            string type = Localize(ItemStats.TypeToken(data));
            if (_selItem != null && !_socketMode) type += "  ·  " + Localize("$genesisui_level") + " " + _selItem.m_quality + " → " + quality;
            _type.text = type;
            _description.text = Localize(data.m_shared.m_description);

            if (_selItem != null && !_socketMode)
            {
                ItemStats.Collect(data, _selItem.m_quality, crafting: true, _stats);
                ItemStats.Collect(data, quality, crafting: true, _next);
            }
            else
            {
                ItemStats.Collect(data, quality, crafting: true, _stats);
                _next.Clear();
            }
            for (int i = 0; i < MaxStats; i++)
            {
                bool show = !_socketMode && i < _stats.Count;
                _statLabels[i].transform.parent.gameObject.SetActive(show);
                if (!show) continue;
                _statLabels[i].text = Localize(_stats[i].Token);
                string value = _stats[i].Value;
                if (_selItem != null)
                {
                    string next = null;
                    foreach (var n in _next) if (n.Token == _stats[i].Token) { next = n.Value; break; }
                    if (next != null && next != value) value = value + "  <color=#8FC77A>→ " + next + "</color>";
                }
                _statValues[i].text = value;
            }
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
                if (m.NativeRoot != element || m.NativeIcon == null)
                {
                    m.NativeRoot = element;
                    var icon = element != null ? element.Find("res_icon") : null;
                    var name = element != null ? element.Find("res_name") : null;
                    var amount = element != null ? element.Find("res_amount") : null;
                    m.NativeIcon = icon != null ? icon.GetComponent<Image>() : null;
                    m.NativeName = name != null ? name.GetComponent<TMP_Text>() : null;
                    m.NativeAmount = amount != null ? amount.GetComponent<TMP_Text>() : null;
                }
                bool show = _selValid && m.NativeIcon != null && m.NativeIcon.gameObject.activeSelf &&
                    element != null && element.gameObject.activeSelf && (element.parent == null || element.parent.gameObject.activeSelf);
                if (m.Root.gameObject.activeSelf != show) m.Root.gameObject.SetActive(show);
                if (!show) continue;
                var sprite = m.NativeIcon.sprite;
                var binding = RequirementBindings.Get(element);
                m.ItemName = binding != null && binding.m_resItem != null ? binding.m_resItem.m_itemData.m_shared.m_name : null;
                string label = m.NativeName != null ? m.NativeName.text : "";
                if (m.Icon.sprite != sprite || m.Label != label)
                {
                    m.Icon.sprite = sprite;
                    m.Label = label;
                    m.Name.text = m.Label;
                    m.Have = m.Need = -1;
                }
                string need = m.NativeAmount != null ? m.NativeAmount.text : "";
                int have = m.ItemName != null ? inventory.CountItems(m.ItemName) : 0;
                if (have != m.Have || need != m.ShownAmount)
                {
                    m.Have = have;
                    m.ShownAmount = need;
                    m.Amount.text = m.ItemName != null ? have + " / " + need : need;
                }
                if (m.NativeAmount != null) m.Amount.color = m.NativeAmount.color;
            }
        }

        /// <summary>The craft button (vanilla's label and state), its progress, and why it cannot be pressed.</summary>
        private void UpdateAction(InventoryGui gui, Player player)
        {
            var vanilla = gui.m_craftButton;
            bool crafting = gui.m_craftProgressPanel != null && gui.m_craftProgressPanel.gameObject.activeSelf;
            bool interactable = vanilla != null && vanilla.interactable && !crafting && _selValid;
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
            if (!Mathf.Approximately(_progress.anchorMax.x, ratio)) _progress.anchorMax = new Vector2(ratio, 0f);
            // A craft that ran to its end (not one cancelled halfway): two light strikes on metal.
            if (!crafting && _craftRatio > 0.9f) UiSound.Play(UiSound.Cue.Craft);
            _craftRatio = crafting ? ratio : 0f;
            if (_craftLight != null)
            {
                // A quiet, narrow border breathes while working; completion brightens it slightly.
                _craftLight.Intensity = crafting ? 0.35f + 0.2f * ratio : 0f;
                _craftLight.Progress = 1f;
            }
            UpdateForge(player);

            string warning = _socketMode && _selValid && gui.m_itemCraftType != null ? gui.m_itemCraftType.text : "";
            bool showWarning = _socketMode && _selValid;
            if (_socketWarning.gameObject.activeSelf != showWarning) _socketWarning.gameObject.SetActive(showWarning);
            if (_socketWarning.text != warning) _socketWarning.text = warning;
            string reason = !_socketMode && _selValid && !interactable && !crafting ? Reason(gui, player) : "";
            if (reason != _shownReason)
            {
                _shownReason = reason;
                _reasonText.text = reason;
            }
        }

        /// <summary>At a forge (any station named one: the black forge too), embers rise behind the details.</summary>
        private void UpdateForge(Player player)
        {
            if (_forgeEmbers == null) return;
            var station = player.GetCurrentCraftingStation();
            if (station == _shownStation) return;
            _shownStation = station;
            bool forge = station != null && station.m_name != null && station.m_name.IndexOf("forge", StringComparison.OrdinalIgnoreCase) >= 0;
            _forgeEmbers.Intensity = forge ? 0.7f : 0f;
        }

        /// <summary>Why vanilla's button is off, in words: missing materials, a better station, or vanilla's own note.</summary>
        private string Reason(InventoryGui gui, Player player)
        {
            _reason.Length = 0;
            foreach (var m in _materials)
            {
                if (!m.Root.gameObject.activeSelf || m.Have >= m.Need) continue;
                _reason.Append(_reason.Length == 0 ? Localize("$genesisui_missing") + " " : ", ");
                _reason.Append(m.Need - m.Have).Append(' ').Append(m.Label);
            }
            if (_reason.Length > 0) return _reason.ToString();
            int quality = _selItem != null ? _selItem.m_quality + (_socketMode ? 0 : 1) : 1;
            var required = _selRecipe.GetRequiredStation(quality);
            if (required != null)
            {
                int level = _selRecipe.GetRequiredStationLevel(quality);
                var here = player.GetCurrentCraftingStation();
                if (here == null || here.m_name != required.m_name || here.GetLevel(true) < level)
                    return Localize("$genesisui_needs_station").Replace("{0}", Localize(required.m_name)).Replace("{1}", level.ToString());
            }
            if (gui.m_itemCraftType != null && gui.m_itemCraftType.gameObject.activeSelf) return gui.m_itemCraftType.text;
            return "";
        }

        // ------------------------------------------------------------------ actions

        /// <summary>
        /// A row picked: vanilla switches to that mode (its tab) if needed, which rebuilds its list,
        /// then vanilla's own row for the same recipe (and item) is pressed.
        /// </summary>
        private void Select(Entry entry)
        {
            var gui = InventoryGui.instance;
            if (gui == null || entry == null) return;
            bool upgrade = entry.Upgrade != null;
            if (!_socketMode && upgrade && gui.InCraftTab()) gui.OnTabUpgradePressed();
            else if (!_socketMode && !upgrade && !gui.InCraftTab()) gui.OnTabCraftPressed();
            if (CraftingListPatch.Version != _vanillaVersion) CacheVanillaRows(gui); // the tab switch rebuilt vanilla's list
            foreach (var row in _vanillaRows)
            {
                if (row.Recipe != entry.Recipe || row.Item != entry.Upgrade) continue;
                if (row.Button != null) row.Button.onClick.Invoke();
                return;
            }
            GenesisLog.Warn("Module:win.crafting", "vanilla has no row for " + entry.Name + (upgrade ? " (upgrade)" : ""));
        }

        private void PressCraft()
        {
            var gui = InventoryGui.instance;
            if (gui != null && gui.m_craftButton != null && gui.m_craftButton.interactable) gui.m_craftButton.onClick.Invoke();
        }

        private void SelectChip(Chip chip)
        {
            _chip = chip;
            _craftColumn.Scroll = _upgradeColumn.Scroll = 0;
            _filterDirty = true;
            for (int i = 0; i < _chips.Count; i++)
                _chips[i].color = ThemeRuntime.ToUnity(i == (int)chip ? _theme.Tokens.AccentGoldBright : _theme.Tokens.TextFlavor);
        }

        private void Scroll(Column column, float delta)
        {
            int step = delta > 0f ? -1 : delta < 0f ? 1 : 0;
            column.Scroll = Mathf.Clamp(column.Scroll + step, 0, Mathf.Max(0, column.Shown.Count - VisibleRows));
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
            _details = _parts.Panel(panels, "Details", 0f, 0f, DetailsW, PanelsHeight, "$genesisui_panel_details", 0f, 19f, TextAlignmentOptions.Center);
            _panel = _parts.Panel(panels, "Crafting", PanelX, 0f, PanelW, PanelsHeight, "$genesisui_panel_crafting", 68f, 26f, TextAlignmentOptions.Left);
            BuildDetails(t);
            BuildPanel(t);
        }

        private void BuildDetails(ThemeTokens t)
        {
            const float pad = 24f, w = DetailsW - 2f * pad;
            _detailsEmpty = _parts.Label(_details, "Empty", FontRole.Body, 17f, t.TextFlavor, pad, 290f, w, 60f, TextAlignmentOptions.Center);
            _detailsEmpty.textWrappingMode = TextWrappingModes.Normal;
            _detailsEmpty.text = Localize("$genesisui_crafting_pick");
            // Behind the details' content: the forge's embers rise from the panel's bottom.
            _forgeEmbers = EmberField.Create(WindowCanvas.At(_details, "Forge", 12f, PanelsHeight * 0.3f, DetailsW - 24f, PanelsHeight * 0.7f - 12f), _theme);
            if (_forgeEmbers != null)
            {
                _forgeEmbers.Speed = 0.8f;
                _forgeEmbers.SetFloat("_Count", 6f);
                _forgeEmbers.SetFloat("_Speed", 0.07f);
                _forgeEmbers.SetFloat("_Glow", 0.18f);
                _forgeEmbers.SetFloat("_GlowHeight", 30f);
                _forgeEmbers.SetFloat("_EmberSize", 1.4f);
            }
            _detailsBody = WindowCanvas.At(_details, "Body", 0f, 0f, DetailsW, PanelsHeight).gameObject;
            var body = (RectTransform)_detailsBody.transform;
            _icon = Ui.Image(WindowCanvas.At(body, "Icon", pad, 66f, w, 118f), null, Color.white);
            _icon.preserveAspect = true;
            _name = _parts.Label(body, "Name", FontRole.Display, 23f, t.AccentGoldBright, pad, 192f, w, 30f, TextAlignmentOptions.Left);
            _name.characterSpacing = 3f;
            _type = _parts.Label(body, "Type", FontRole.Body, 17f, t.TextFlavor, pad, 222f, w, 22f, TextAlignmentOptions.Left);
            _description = _parts.Label(body, "Description", FontRole.Body, 16f, t.TextBody, pad, 248f, w, 44f, TextAlignmentOptions.TopLeft);
            _description.textWrappingMode = TextWrappingModes.Normal;
            _description.overflowMode = TextOverflowModes.Ellipsis;
            _description.enableAutoSizing = false;
            _parts.Rule(body, pad, 300f, w);
            for (int i = 0; i < MaxStats; i++)
            {
                var row = WindowCanvas.At(body, "Stat " + i, pad, 310f + i * 26f, w, 25f);
                _statLabels[i] = _parts.Label(row, "Label", FontRole.Body, 16f, t.TextBody, 0f, 0f, w * 0.5f, 24f, TextAlignmentOptions.Left);
                _statValues[i] = _parts.Label(row, "Value", FontRole.Body, 16f, t.TextTitle, w * 0.3f, 0f, w * 0.7f, 24f, TextAlignmentOptions.Right);
                Ui.Image(WindowCanvas.At(row, "Line", 0f, 24f, w, 1f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.12f));
            }

            var title = _parts.Label(body, "MaterialsTitle", FontRole.Label, 14f, t.AccentGoldBright, pad, 470f, w, 20f, TextAlignmentOptions.Left);
            title.text = Localize("$genesisui_materials_needed").ToUpperInvariant();
            title.characterSpacing = 3f;
            for (int i = 0; i < MaxMaterials; i++)
            {
                const float box = 80f;
                var m = new Material { Root = WindowCanvas.At(body, "Material " + i, pad + i * 87f, 494f, box, box) };
                Frame.Dress(m.Root, _theme, "hotslot", "Windows", box);
                m.Icon = Ui.Image(WindowCanvas.At(m.Root, "Icon", (box - 36f) / 2f, 7f, 36f, 36f), null, Color.white);
                m.Icon.preserveAspect = true;
                m.Name = _parts.Label(m.Root, "Name", FontRole.Body, 12f, t.TextTitle, 3f, 44f, box - 6f, 16f, TextAlignmentOptions.Center);
                m.Amount = _parts.Label(m.Root, "Amount", FontRole.Display, 13f, t.StatePositive, 3f, 60f, box - 6f, 16f, TextAlignmentOptions.Center);
                m.Root.gameObject.SetActive(false);
                _materials.Add(m);
            }
            _socketWarning = _parts.Label(body, "SocketWarning", FontRole.Body, 17f, t.StateDanger, pad, 310f, w, 140f, TextAlignmentOptions.TopLeft);
            _socketWarning.textWrappingMode = TextWrappingModes.Normal;
            _reasonText = _parts.Label(body, "Reason", FontRole.Body, 15f, t.StateDanger, pad, 580f, w, 22f, TextAlignmentOptions.Left);
            _editSockets = _parts.Button(body, "EditSockets", pad, 612f, 130f, 44f, "$genesisui_insert_gems", 16f, "socket gems",
                () => { Adapters.Jewelcrafting.JewelcraftingAdapter.PressSockets(); WindowShellModule.RequestInventory(); }, out _);
            _variant = _parts.Button(body, "Variant", pad, 612f, 130f, 44f, "$genesisui_style", 17f, "crafting style",
                () => { var gui = InventoryGui.instance; if (gui != null) gui.m_variantButton.onClick.Invoke(); }, out _);
            _craft = _parts.Button(body, "Craft", pad + 140f, 612f, w - 140f, 44f, null, 20f, "crafting craft", PressCraft, out _craftLabel);
            _craftLabel.font = _theme.Font(FontRole.Display);
            _craftLabel.characterSpacing = 4f;
            _progress = Ui.Child(_craft.transform, "Progress");
            _progress.anchorMin = new Vector2(0f, 0f);
            _progress.anchorMax = new Vector2(0f, 0f);
            _progress.offsetMin = new Vector2(3f, 3f);
            _progress.offsetMax = new Vector2(-3f, 4.5f);
            _progress.SetSiblingIndex(1);
            Ui.Image(_progress, _theme.Sprite("bar_fill"), ThemeRuntime.ToUnity(t.AccentGold).WithA(0.45f));
            // Without shaders, progress is a thin rule; the button interior is never filled.
            _craftLight = EdgeLight.Create((RectTransform)_craft.transform, _theme, 10f);
            if (_craftLight != null)
            {
                _craftLight.Target = (RectTransform)_craft.transform;
                _craftLight.Speed = 4f;
                _craftLight.SetFloat("_Inset", 2f);
                _craftLight.SetFloat("_Radius", 4f);
                _craftLight.SetFloat("_Line", 0.9f);
                _craftLight.SetFloat("_Halo", 1.5f);
                _craftLight.Pulse = 0.18f;
                _progress.gameObject.SetActive(false);
            }
            _detailsBody.SetActive(false);
        }

        private void BuildPanel(ThemeTokens t)
        {
            _normalTab = _parts.Button(_panel, "NativeCraftTab", 220f, 16f, 126f, 32f, "$genesisui_tab_craft", 16f, "native crafting tab",
                Adapters.Jewelcrafting.JewelcraftingAdapter.PressCraft, out _);
            _socketTab = _parts.Button(_panel, "NativeSocketTab", 354f, 16f, 126f, 32f, "$genesisui_sockets", 16f, "native socket tab",
                Adapters.Jewelcrafting.JewelcraftingAdapter.PressTab, out _);
            _station = _parts.Label(_panel, "Station", FontRole.Body, 17f, t.TextFlavor, 490f, 18f, 430f, 28f, TextAlignmentOptions.Right);
            _repair = _parts.Button(_panel, "Repair", PanelW - 24f - 140f, 16f, 140f, 32f, "$genesisui_repair", 16f, "crafting repair",
                () => { var gui = InventoryGui.instance; if (gui != null && gui.m_repairButton.interactable) gui.m_repairButton.onClick.Invoke(); }, out _);
            BuildSearch(t);
            float x = 22f;
            for (int i = 0; i < ChipTokens.Length; i++)
            {
                var chip = (Chip)i;
                string text = Localize(ChipTokens[i]);
                var probe = _parts.Label(_panel, "Probe", FontRole.Body, 16f, t.TextTitle, 0f, 0f, 400f, 30f, TextAlignmentOptions.Left);
                float w = Mathf.Ceil(probe.GetPreferredValues(text).x) + 30f;
                UnityEngine.Object.Destroy(probe.gameObject);
                _parts.Button(_panel, "Chip " + chip, x, 116f, w, 32f, null, 16f, "crafting chip", () => SelectChip(chip), out var label);
                label.text = text;
                _chips.Add(label);
                x += w + 8f;
            }
            SelectChip(Chip.All);

            BuildColumn(_craftColumn, LeftX, "$genesisui_tab_craft", t);
            BuildColumn(_upgradeColumn, RightX, "$genesisui_tab_upgrade", t);
            // The separator between the columns: a fine vertical line with the knot in its middle.
            float sepX = (LeftX + ColumnW + RightX) / 2f;
            var sep = WindowCanvas.At(_panel, "Separator", sepX, ColumnY - 26f, 1f, PanelsHeight - ColumnY + 6f);
            Ui.Image(sep, null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.35f));
            _parts.Knot(sep, 14f);
        }

        private void BuildColumn(Column column, float x, string titleToken, ThemeTokens t)
        {
            var title = _parts.Label(_panel, "Title " + titleToken, FontRole.Display, 17f, t.AccentGoldBright, x, 158f, ColumnW, 26f, TextAlignmentOptions.Center);
            column.Title = title;
            title.text = Localize(titleToken).ToUpperInvariant();
            title.characterSpacing = 5f;
            _parts.Rule(_panel, x + ColumnW / 2f - 90f, 186f, 180f);
            float height = VisibleRows * (RowH + RowGap) - RowGap;
            var list = WindowCanvas.At(_panel, "Column " + titleToken, x, ColumnY, ColumnW - 12f, height);
            Ui.Image(list, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            list.gameObject.AddComponent<ListScroll>().Init(delta => Scroll(column, delta));
            for (int i = 0; i < VisibleRows; i++) column.Rows.Add(MakeRow(list, column, i, ColumnW - 12f));
            column.Empty = _parts.Label(list, "Empty", FontRole.Body, 17f, t.TextFlavor, 20f, 40f, ColumnW - 52f, 80f, TextAlignmentOptions.Center);
            column.Empty.textWrappingMode = TextWrappingModes.Normal;
            column.Track = WindowCanvas.At(_panel, "Scroll " + titleToken, x + ColumnW - 6f, ColumnY, 3f, height);
            Ui.Image(column.Track, null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.25f));
            column.Thumb = WindowCanvas.At(column.Track, "Thumb", -1f, 0f, 5f, 40f);
            Ui.Image(column.Thumb, null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.85f));
        }

        private void BuildSearch(ThemeTokens t)
        {
            var rt = WindowCanvas.At(_panel, "Search", 22f, 66f, PanelW - 44f, 36f);
            Frame.Dress(rt, _theme, "keycap_wide", "Windows", 36f);
            var viewport = Ui.Fill(Ui.Child(rt, "Viewport"), 16f, 2f, 16f, 2f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var text = _parts.Label(viewport, "Text", FontRole.Body, 17f, t.TextTitle, 0f, 0f, PanelW - 76f, 32f, TextAlignmentOptions.MidlineLeft);
            Ui.Fill((RectTransform)text.transform);
            var placeholder = _parts.Label(viewport, "Placeholder", FontRole.Body, 17f, t.TextFlavor, 0f, 0f, PanelW - 76f, 32f, TextAlignmentOptions.MidlineLeft);
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
            _search.onSelect.AddListener(_ => Guard.Run("module:win.crafting", BeginTyping));
            _search.onDeselect.AddListener(_ => Guard.Try("recipe search blur", EndTyping));
            _search.onEndEdit.AddListener(_ => Guard.Try("recipe search end", EndTyping));
            _search.onValueChanged.AddListener(value => Guard.Run("module:win.crafting", () =>
            {
                _query = (value ?? "").Trim().ToLowerInvariant();
                _craftColumn.Scroll = _upgradeColumn.Scroll = 0;
                _filterDirty = true;
            }));
        }

        private Row MakeRow(RectTransform list, Column column, int i, float width)
        {
            var t = _theme.Tokens;
            var row = new Row { Root = WindowCanvas.At(list, "Row " + i, 0f, i * (RowH + RowGap), width, RowH) };
            row.Group = row.Root.gameObject.AddComponent<CanvasGroup>();
            Frame.Dress(row.Root, _theme, "keycap_wide", "Windows", RowH);
            row.Selection = Ui.Image(Ui.Fill(Ui.Child(row.Root, "Selected"), 3f, 3f, 3f, 3f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.14f));
            row.Selection.enabled = false;
            row.Shine = OneShotLight.Shine(row.Root, row.Selection.transform.GetSiblingIndex() + 1, _theme);
            row.Icon = Ui.Image(WindowCanvas.At(row.Root, "Icon", 8f, 6f, 42f, 42f), null, Color.white);
            row.Icon.preserveAspect = true;
            row.Name = _parts.Label(row.Root, "Name", FontRole.Body, 17f, t.TextTitle, 60f, 5f, width - 170f, 24f, TextAlignmentOptions.Left);
            row.Sub = _parts.Label(row.Root, "Sub", FontRole.Body, 14f, t.TextFlavor, 60f, 28f, width - 170f, 20f, TextAlignmentOptions.Left);
            row.Right = _parts.Label(row.Root, "Right", FontRole.Display, 15f, t.AccentGoldBright, width - 110f, 15f, 100f, 24f, TextAlignmentOptions.Right);
            _parts.Clickable(row.Root, "crafting recipe", () => Select(row.Bound));
            row.Root.gameObject.AddComponent<ListScroll>().Init(delta => Scroll(column, delta));
            return row;
        }

        private static string Localize(string text) => WindowParts.Localize(text);

        /// <summary>Mouse wheel over a column.</summary>
        private sealed class ListScroll : GuardedBehaviour, IScrollHandler
        {
            private Action<float> _onScroll;
            internal void Init(Action<float> onScroll) => _onScroll = onScroll;
            public void OnScroll(PointerEventData e) { if (_onScroll != null) Guard.Run(CallbackOwner, _onScroll, e.scrollDelta.y); }
        }
    }
}
