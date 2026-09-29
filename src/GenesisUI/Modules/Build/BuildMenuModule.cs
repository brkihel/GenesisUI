using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GenesisUI.Modules.Build
{
    /// <summary>
    /// The build menu of the hammer, hoe and cultivator (ConceptArt 12's Construção panel, D-032):
    /// GenesisUI draws the lists, tags, search and pieces that vanilla's BuildUi built, plus the details
    /// of the piece under the cursor (materials have/need, station) and how to build. Vanilla's BuildUi
    /// stays open and working behind it, invisible: it decides the lists (all, recent, favourites and
    /// custom tags, other mods' pieces), the search runs in its own input field, and every click is
    /// handed to its own buttons, so selecting, favouriting and placing stay vanilla's.
    /// </summary>
    [GameContract("assembly_valheim", "Hud", "instance")]
    [GameContract("assembly_valheim", "Hud", "m_buildUi")]
    [GameContract("assembly_valheim", "Hud", "CloseBuildUi")]
    [GameContract("assembly_valheim", "Hud", "m_buildHud")]
    [GameContract("assembly_valheim", "Hud", "m_buildSelection")]
    [GameContract("assembly_valheim", "Hud", "m_pieceDescription")]
    [GameContract("assembly_valheim", "Hud", "m_buildIcon")]
    [GameContract("assembly_valheim", "Hud", "m_requirementItems")]
    [GameContract("assembly_valheim", "BuildUi", "m_tabHandler")]
    [GameContract("assembly_valheim", "BuildUi", "m_tagButtons")]
    [GameContract("assembly_valheim", "BuildUi", "m_showAllTagsButton")]
    [GameContract("assembly_valheim", "BuildUi", "m_pieceButtons")]
    [GameContract("assembly_valheim", "BuildUi", "m_specialPieceButton")]
    [GameContract("assembly_valheim", "BuildUi", "m_searchField")]
    [GameContract("assembly_valheim", "BuildUi", "m_currentTagId")]
    [GameContract("assembly_valheim", "BuildUi", "OnHoverPiece")]
    [GameContract("assembly_valheim", "BuildUi", "ToggleFavorite")]
    [GameContract("assembly_valheim", "BuildUi", "IsFavoritePiece")]
    [GameContract("assembly_valheim", "BuildUiPieceButton", "get_Piece")]
    [GameContract("assembly_valheim", "BuildUiPieceButton", "m_button")]
    [GameContract("assembly_valheim", "BuildUiTagButton", "buttonComponent")]
    [GameContract("assembly_valheim", "BuildUiTagButton", "m_tagId")]
    [GameContract("assembly_valheim", "BuildUiTagButton", "get_CurrentText")]
    [GameContract("assembly_valheim", "TabHandler", "m_tabs")]
    [GameContract("assembly_valheim", "TabHandler", "GetActiveTab")]
    [GameContract("assembly_valheim", "TabHandler+Tab", "m_button")]
    [GameContract("assembly_valheim", "Player", "GetSelectedPiece")]
    [GameContract("assembly_valheim", "Player", "HaveRequirements", Parameters = new[] { "Piece", "Player+RequirementMode" })]
    [GameContract("assembly_valheim", "Piece", "m_name")]
    [GameContract("assembly_valheim", "Piece", "m_description")]
    [GameContract("assembly_valheim", "Piece", "m_icon")]
    [GameContract("assembly_valheim", "Piece", "m_resources")]
    [GameContract("assembly_valheim", "Piece", "m_craftingStation")]
    [GameContract("assembly_valheim", "Piece+Requirement", "m_amount")]
    [GameContract("assembly_valheim", "CraftingStation", "HaveBuildStationInRange")]
    internal sealed class BuildMenuModule : IUiModule
    {
        private const string Owner = "module:hud.build";
        private static readonly string[] NoRegions = new string[0];
        private const float Top = 40f, Height = 770f;
        private const float TagsX = 150f, TagsW = 280f, PiecesX = 434f, PiecesW = 640f, DetailsX = 1078f, DetailsW = 352f;
        private const float Cell = 72f, Pitch = 80f;
        private const int MaxTabs = 6, MaxTags = 24, MaxMaterials = 5;

        private sealed class PieceCell
        {
            public RectTransform Root;
            public Image Icon, Selected, Favorite;
            public BuildUiPieceButton Source;
        }

        private sealed class Material
        {
            public RectTransform Root;
            public Image Icon;
            public TextMeshProUGUI Name, Amount;
        }

        private readonly VanillaSkin _skin = new VanillaSkin(Owner);
        private readonly List<PieceCell> _cells = new List<PieceCell>(160);
        private readonly List<BuildUiPieceButton> _shown = new List<BuildUiPieceButton>(160);
        private readonly List<(Button Button, TextMeshProUGUI Label, Image Mark)> _tabs = new List<(Button, TextMeshProUGUI, Image)>();
        private readonly List<(RectTransform Root, TextMeshProUGUI Label, Image Mark)> _tags = new List<(RectTransform, TextMeshProUGUI, Image)>();
        private readonly List<BuildUiTagButton> _tagSources = new List<BuildUiTagButton>(MaxTags);
        private readonly List<Material> _materials = new List<Material>(MaxMaterials);

        private ThemeRuntime _theme;
        private WindowParts _parts;
        private RectTransform _root, _board;
        private ScrollArea _grid;
        private TextMeshProUGUI _search, _empty, _name, _description, _station, _favoriteLabel;
        private Image _icon;
        private GameObject _detailBody;
        private Button _favorite;
        private bool _applied;
        private BuildUi _ui;
        private TMP_InputField _field;
        private Piece _detailPiece;
        private int _signature = int.MinValue;
        private float _statusIn;

        // The placement card (the concept's "Posicionar"): vanilla's build HUD mirrored while placing.
        private readonly VanillaSkin _hudSkin = new VanillaSkin(Owner + ":placement");
        private RectTransform _placeRoot, _placeBoard;
        private Image _placeIcon;
        private TextMeshProUGUI _placeName, _placeDescription;
        private readonly List<(RectTransform Root, Image Icon, TextMeshProUGUI Amount)> _placeReqs = new List<(RectTransform, Image, TextMeshProUGUI)>();
        private bool _placing;

        private AccessTools.FieldRef<BuildUi, TabHandler> _tabHandler;
        private AccessTools.FieldRef<BuildUi, List<BuildUiTagButton>> _tagButtons;
        private AccessTools.FieldRef<BuildUi, BuildUiTagButton> _showAll;
        private AccessTools.FieldRef<BuildUi, List<BuildUiPieceButton>> _pieceButtons;
        private AccessTools.FieldRef<BuildUi, BuildUiPieceButton> _special;
        private AccessTools.FieldRef<BuildUi, int> _currentTag;
        private AccessTools.FieldRef<BuildUiPieceButton, Button> _pieceButton;
        private AccessTools.FieldRef<BuildUiTagButton, Button> _tagButton;

        public string Id => "hud.build";
        public string NameToken => "$genesisui_module_build_menu";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f;

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _parts = new WindowParts(_theme);
            _tabHandler = AccessTools.FieldRefAccess<BuildUi, TabHandler>("m_tabHandler");
            _tagButtons = AccessTools.FieldRefAccess<BuildUi, List<BuildUiTagButton>>("m_tagButtons");
            _showAll = AccessTools.FieldRefAccess<BuildUi, BuildUiTagButton>("m_showAllTagsButton");
            _pieceButtons = AccessTools.FieldRefAccess<BuildUi, List<BuildUiPieceButton>>("m_pieceButtons");
            _special = AccessTools.FieldRefAccess<BuildUi, BuildUiPieceButton>("m_specialPieceButton");
            _currentTag = AccessTools.FieldRefAccess<BuildUi, int>("m_currentTagId");
            _pieceButton = AccessTools.FieldRefAccess<BuildUiPieceButton, Button>("m_button");
            _tagButton = AccessTools.FieldRefAccess<BuildUiTagButton, Button>("buttonComponent");
            _applied = false;
            _signature = int.MinValue;
        }

        public void Refresh(float deltaSeconds)
        {
            var hud = Hud.instance;
            var player = Player.m_localPlayer;
            UpdatePlacement(hud, player);
            var ui = hud != null ? hud.m_buildUi : null;
            bool open = ui != null && player != null && ui.gameObject.activeSelf;
            if (!open)
            {
                if (_applied) Unapply();
                return;
            }
            if (!EnsureBuilt(ui)) return;
            if (!_applied) Apply(ui);
            WindowCanvas.Fit(_board);

            int signature = Signature();
            if (signature != _signature) Rebuild(signature);
            UpdateSearch();
            _statusIn -= deltaSeconds;
            bool status = _statusIn <= 0f;
            if (status)
            {
                _statusIn = 0.5f;
                UpdateTabs();
                UpdateTags();
            }
            UpdateCells(player, status);
            if (status) UpdateMaterials(player);
        }

        public void Teardown()
        {
            if (_applied) Unapply();
            if (_placing) { _placing = false; _hudSkin.Restore(); }
            if (_root != null) Object.Destroy(_root.gameObject);
            if (_placeRoot != null) Object.Destroy(_placeRoot.gameObject);
            _root = null;
            _placeRoot = null;
            _placeReqs.Clear();
            _cells.Clear();
            _tabs.Clear();
            _tags.Clear();
            _materials.Clear();
            _shown.Clear();
            _signature = int.MinValue;
        }

        // ------------------------------------------------------------------ vanilla

        private bool EnsureBuilt(BuildUi ui)
        {
            if (_root != null) return true;
            _root = WindowCanvas.CreateRoot(ui, "GenesisUI.BuildMenu", behind: false);
            if (_root == null) return false;
            _board = WindowCanvas.Area(_root, "Board");
            Draw();
            _root.gameObject.SetActive(false);
            return true;
        }

        private void Apply(BuildUi ui)
        {
            _applied = true;
            _ui = ui;
            _field = AccessTools.Field(typeof(BuildUi), "m_searchField").GetValue(ui) as TMP_InputField;
            // Vanilla's menu keeps working, unseen: its buttons are pressed through ours and its search
            // field takes the typing. Interactable stays on so that field keeps receiving keys.
            var group = _skin.Group(ui.gameObject);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            _root.gameObject.SetActive(true);
            _signature = int.MinValue;
            _detailPiece = null;
            ShowDetails(Player.m_localPlayer.GetSelectedPiece());
            GenesisLog.Info(Owner, "build menu shown over vanilla's (hidden)");
        }

        private void Unapply()
        {
            _applied = false;
            _skin.Restore();
            if (_root != null) _root.gameObject.SetActive(false);
            _ui = null;
            _field = null;
        }

        /// <summary>What vanilla shows now: the active piece buttons in order, the tab and the tag.</summary>
        private int Signature()
        {
            unchecked
            {
                int h = 17;
                var list = _pieceButtons(_ui);
                if (list != null)
                    for (int i = 0; i < list.Count; i++)
                        if (list[i] != null && list[i].gameObject.activeSelf) h = h * 31 + list[i].GetInstanceID();
                var special = _special(_ui);
                if (special != null) h = h * 31 + special.GetInstanceID();
                var tags = _tagButtons(_ui);
                h = h * 31 + (tags != null ? tags.Count : 0);
                return h;
            }
        }

        private void Rebuild(int signature)
        {
            _signature = signature;
            _shown.Clear();
            var special = _special(_ui);
            if (special != null && special.gameObject.activeInHierarchy) _shown.Add(special);
            var list = _pieceButtons(_ui);
            if (list != null)
                foreach (var b in list)
                    if (b != null && b.gameObject.activeSelf && b.Piece != null) _shown.Add(b);
            int perRow = Mathf.Max(1, Mathf.FloorToInt((_grid.Viewport.sizeDelta.x + Pitch - Cell) / Pitch));
            while (_cells.Count < _shown.Count) _cells.Add(MakeCell(_cells.Count));
            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                bool show = i < _shown.Count;
                cell.Root.gameObject.SetActive(show);
                if (!show) { cell.Source = null; continue; }
                cell.Source = _shown[i];
                cell.Root.anchoredPosition = new Vector2((i % perRow) * Pitch, -(i / perRow) * Pitch);
                cell.Icon.sprite = _shown[i].Piece.m_icon;
            }
            _grid.ContentHeight = ((_shown.Count + perRow - 1) / perRow) * Pitch;
            _empty.gameObject.SetActive(_shown.Count == 0);
            _statusIn = 0f;
        }

        private void UpdateTabs()
        {
            var handler = _tabHandler(_ui);
            int count = handler != null && handler.m_tabs != null ? handler.m_tabs.Count : 0;
            int active = handler != null ? handler.GetActiveTab() : -1;
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool show = i < count && handler.m_tabs[i].m_button != null && handler.m_tabs[i].m_button.gameObject.activeInHierarchy;
                var tab = _tabs[i];
                if (tab.Button.gameObject.activeSelf != show) tab.Button.gameObject.SetActive(show);
                if (!show) continue;
                var text = handler.m_tabs[i].m_button.GetComponentInChildren<TMP_Text>();
                string label = text != null ? text.text : (i + 1).ToString();
                if (tab.Label.text != label) tab.Label.text = label;
                bool on = i == active;
                if (tab.Mark.enabled != on) tab.Mark.enabled = on;
            }
        }

        private void UpdateTags()
        {
            _tagSources.Clear();
            var all = _showAll(_ui);
            if (all != null && all.gameObject.activeInHierarchy) _tagSources.Add(all);
            var tags = _tagButtons(_ui);
            if (tags != null)
                foreach (var t in tags)
                    if (t != null && t.gameObject.activeInHierarchy && _tagSources.Count < MaxTags) _tagSources.Add(t);
            int current = _currentTag(_ui);
            for (int i = 0; i < _tags.Count; i++)
            {
                var row = _tags[i];
                bool show = i < _tagSources.Count;
                if (row.Root.gameObject.activeSelf != show) row.Root.gameObject.SetActive(show);
                if (!show) continue;
                string label = _tagSources[i].CurrentText ?? "";
                if (row.Label.text != label) row.Label.text = label;
                bool on = _tagSources[i].m_tagId == current;
                if (row.Mark.enabled != on) row.Mark.enabled = on;
            }
        }

        private void UpdateSearch()
        {
            if (_field == null) return;
            string text = _field.text ?? "";
            string shown = text.Length == 0 && !_field.isFocused ? Localize("$genesisui_build_search")
                : text + (_field.isFocused && Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f ? "|" : "");
            if (_search.text != shown) _search.text = shown;
        }

        private void UpdateCells(Player player, bool status)
        {
            var selected = player.GetSelectedPiece();
            for (int i = 0; i < _shown.Count && i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var piece = cell.Source != null ? cell.Source.Piece : null;
                if (piece == null) continue;
                bool sel = selected != null && selected.gameObject.name == piece.gameObject.name;
                if (cell.Selected.enabled != sel) cell.Selected.enabled = sel;
                if (!status) continue;
                bool fav = _ui.IsFavoritePiece(piece);
                if (cell.Favorite.enabled != fav) cell.Favorite.enabled = fav;
                bool can = player.HaveRequirements(piece, Player.RequirementMode.CanBuild);
                var color = can ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                if (cell.Icon.color != color) cell.Icon.color = color;
            }
        }

        // ------------------------------------------------------------------ placement card

        /// <summary>
        /// While placing (vanilla shows its build HUD) our card shows the same things, read from vanilla's
        /// own texts and requirement items, so hovered pieces, missing materials (vanilla's red) and the
        /// station row stay exactly vanilla's. Vanilla's HUD is hidden, never disabled.
        /// </summary>
        private void UpdatePlacement(Hud hud, Player player)
        {
            bool show = hud != null && player != null && hud.m_buildHud != null && hud.m_buildHud.activeSelf;
            if (!show)
            {
                if (_placing)
                {
                    _placing = false;
                    _hudSkin.Restore();
                    if (_placeRoot != null) _placeRoot.gameObject.SetActive(false);
                }
                return;
            }
            if (_placeRoot == null)
            {
                _placeRoot = WindowCanvas.CreateRoot(hud, "GenesisUI.BuildPlacement", behind: false);
                if (_placeRoot == null) return;
                _placeBoard = WindowCanvas.Area(_placeRoot, "Board");
                DrawPlacement();
            }
            if (!_placing)
            {
                _placing = true;
                var group = _hudSkin.Group(hud.m_buildHud);
                group.alpha = 0f;
                group.blocksRaycasts = false; // interactable stays: vanilla's build menu search lives under it
                _placeRoot.gameObject.SetActive(true);
            }
            WindowCanvas.Fit(_placeBoard);
            bool menu = _applied;
            if (_placeBoard.gameObject.activeSelf == menu) _placeBoard.gameObject.SetActive(!menu);
            if (menu) return;

            string name = hud.m_buildSelection != null ? hud.m_buildSelection.text : "";
            if (_placeName.text != name) _placeName.text = name.ToUpperInvariant();
            string description = hud.m_pieceDescription != null ? hud.m_pieceDescription.text : "";
            if (_placeDescription.text != description) _placeDescription.text = description;
            bool icon = hud.m_buildIcon != null && hud.m_buildIcon.enabled;
            _placeIcon.enabled = icon;
            if (icon && _placeIcon.sprite != hud.m_buildIcon.sprite) _placeIcon.sprite = hud.m_buildIcon.sprite;
            var items = hud.m_requirementItems;
            for (int i = 0; i < _placeReqs.Count; i++)
            {
                var r = _placeReqs[i];
                var item = items != null && i < items.Length ? items[i] : null;
                bool on = item != null && item.activeSelf;
                if (r.Root.gameObject.activeSelf != on) r.Root.gameObject.SetActive(on);
                if (!on) continue;
                var ic = item.transform.Find("res_icon");
                var am = item.transform.Find("res_amount");
                if (ic != null) { var sp = ic.GetComponent<Image>().sprite; if (r.Icon.sprite != sp) r.Icon.sprite = sp; }
                if (am != null)
                {
                    var tx = am.GetComponent<TMP_Text>();
                    if (r.Amount.text != tx.text) r.Amount.text = tx.text;
                    var c = tx.color == Color.red ? ThemeRuntime.ToUnity(_theme.Tokens.StateDanger) : ThemeRuntime.ToUnity(_theme.Tokens.TextTitle);
                    if (r.Amount.color != c) r.Amount.color = c;
                }
            }
        }

        private void DrawPlacement()
        {
            var t = _theme.Tokens;
            const float x = 1180f, y = 560f, w = 400f, h = 190f;
            var card = WindowCanvas.At(_placeBoard, "Card", x, y, w, h);
            Frame.Dress(card, _theme, "card", "Windows");
            var title = _parts.Label(card, "Title", FontRole.Label, 13f, t.AccentGoldBright, 18f, 10f, w - 36f, 18f, TextAlignmentOptions.Left);
            title.text = Localize("$genesisui_build_placing").ToUpperInvariant();
            title.characterSpacing = 3f;
            _placeIcon = Ui.Image(WindowCanvas.At(card, "Icon", 16f, 34f, 56f, 56f), null, Color.white);
            _placeIcon.preserveAspect = true;
            _placeName = _parts.Label(card, "Name", FontRole.Display, 17f, t.TextTitle, 82f, 34f, w - 100f, 24f, TextAlignmentOptions.Left);
            _placeDescription = _parts.Label(card, "Description", FontRole.Body, 14f, t.TextBody, 82f, 58f, w - 100f, 36f, TextAlignmentOptions.TopLeft);
            _placeDescription.textWrappingMode = TextWrappingModes.Normal;
            _placeDescription.overflowMode = TextOverflowModes.Ellipsis;
            _placeDescription.enableAutoSizing = false;
            for (int i = 0; i < 6; i++)
            {
                var root = WindowCanvas.At(card, "Req " + i, 16f + i * 62f, 100f, 58f, 44f);
                var icon = Ui.Image(WindowCanvas.At(root, "Icon", 0f, 6f, 30f, 30f), null, Color.white);
                icon.preserveAspect = true;
                var amount = _parts.Label(root, "Amount", FontRole.Display, 14f, t.TextTitle, 30f, 0f, 30f, 44f, TextAlignmentOptions.MidlineLeft);
                root.gameObject.SetActive(false);
                _placeReqs.Add((root, icon, amount));
            }
            var hints = _parts.Label(card, "Hints", FontRole.Body, 13f, t.TextFlavor, 18f, 152f, w - 36f, 30f, TextAlignmentOptions.TopLeft);
            hints.textWrappingMode = TextWrappingModes.Normal;
            hints.enableAutoSizing = false;
            hints.text = Localize("$genesisui_build_placing_hints");
        }

        // ------------------------------------------------------------------ details

        private void ShowDetails(Piece piece)
        {
            _detailPiece = piece;
            bool any = piece != null;
            _detailBody.SetActive(any);
            if (!any) return;
            _icon.sprite = piece.m_icon;
            _name.text = Localize(piece.m_name).ToUpperInvariant();
            _description.text = Localize(piece.m_description);
            UpdateMaterials(Player.m_localPlayer);
        }

        private void UpdateMaterials(Player player)
        {
            if (_detailPiece == null || player == null) return;
            var inventory = player.GetInventory();
            var reqs = _detailPiece.m_resources;
            for (int i = 0; i < _materials.Count; i++)
            {
                var m = _materials[i];
                bool show = reqs != null && i < reqs.Length && reqs[i] != null && reqs[i].m_resItem != null;
                if (m.Root.gameObject.activeSelf != show) m.Root.gameObject.SetActive(show);
                if (!show) continue;
                var data = reqs[i].m_resItem.m_itemData;
                m.Icon.sprite = data.GetIcon();
                m.Name.text = Localize(data.m_shared.m_name);
                int have = inventory.CountItems(data.m_shared.m_name), need = reqs[i].m_amount;
                m.Amount.SetText("{0} / {1}", have, need);
                m.Amount.color = ThemeRuntime.ToUnity(have >= need ? _theme.Tokens.StatePositive : _theme.Tokens.StateDanger);
            }
            var station = _detailPiece.m_craftingStation;
            if (station == null) _station.text = "";
            else
            {
                bool near = CraftingStation.HaveBuildStationInRange(station.m_name, player.transform.position) != null;
                _station.text = Localize("$genesisui_build_needs") + " " + Localize(station.m_name) + (near ? "" : "  —  " + Localize("$genesisui_build_not_near"));
                _station.color = ThemeRuntime.ToUnity(near ? _theme.Tokens.TextFlavor : _theme.Tokens.StateDanger);
            }
            bool fav = _ui != null && _ui.IsFavoritePiece(_detailPiece);
            _favoriteLabel.text = Localize(fav ? "$genesisui_build_unfavorite" : "$genesisui_build_favorite");
        }

        // ------------------------------------------------------------------ actions

        private void PressPiece(PieceCell cell)
        {
            if (cell.Source == null) return;
            var button = _pieceButton(cell.Source);
            if (button != null) button.onClick.Invoke(); // vanilla selects the piece and closes the menu
        }

        private void HoverPiece(PieceCell cell)
        {
            if (cell.Source == null || _ui == null) return;
            _ui.OnHoverPiece(cell.Source); // vanilla's build HUD follows the hovered piece too
            ShowDetails(cell.Source.Piece);
        }

        private void ToggleFavorite(Piece piece)
        {
            if (_ui == null || piece == null) return;
            _ui.ToggleFavorite(piece, -1);
            _statusIn = 0f;
            if (piece == _detailPiece) UpdateMaterials(Player.m_localPlayer);
        }

        private void PressTag(int index)
        {
            if (index >= _tagSources.Count) return;
            var button = _tagButton(_tagSources[index]);
            if (button != null) button.onClick.Invoke();
            _statusIn = 0f;
        }

        private void PressTab(int index)
        {
            var handler = _tabHandler(_ui);
            if (handler == null || index >= handler.m_tabs.Count) return;
            var button = handler.m_tabs[index].m_button;
            if (button != null) button.onClick.Invoke();
            _statusIn = 0f;
        }

        /// <summary>Typing goes to vanilla's own search field (its shortcuts step aside while it is focused).</summary>
        private void FocusSearch()
        {
            if (_field == null) return;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_field.gameObject);
            _field.ActivateInputField();
        }

        // ------------------------------------------------------------------ building

        private void Draw()
        {
            var t = _theme.Tokens;
            var panels = WindowCanvas.At(_board, "Panels", 0f, Top, WindowCanvas.Design.x, Height);
            var tags = _parts.Panel(panels, "Tags", TagsX, 0f, TagsW, Height, "$genesisui_build_tags", 0f, 19f, TextAlignmentOptions.Center);
            var pieces = _parts.Panel(panels, "Pieces", PiecesX, 0f, PiecesW, Height, "$genesisui_build_title", 54f, 24f, TextAlignmentOptions.Left);
            var details = _parts.Panel(panels, "Details", DetailsX, 0f, DetailsW, Height, "$genesisui_panel_details", 0f, 19f, TextAlignmentOptions.Center);
            ((RectTransform)_board.transform).SetAsLastSibling();

            _parts.Button(pieces, "Close", PiecesW - 24f - 110f, 16f, 110f, 32f, "$genesisui_hint_close", 16f, "build close", Hud.CloseBuildUi, out _);

            for (int i = 0; i < MaxTabs; i++)
            {
                int index = i;
                var b = _parts.Button(pieces, "Tab " + i, 22f + i * 124f, 64f, 116f, 34f, null, 16f, "build list", () => PressTab(index), out var label);
                var mark = Ui.Image(Ui.Fill(Ui.Child(b.transform, "Selected"), 3f, 3f, 3f, 3f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.18f));
                mark.transform.SetSiblingIndex(1);
                b.gameObject.SetActive(false);
                _tabs.Add((b, label, mark));
            }
            var search = WindowCanvas.At(pieces, "Search", 22f, 108f, PiecesW - 44f, 36f);
            Frame.Dress(search, _theme, "keycap_wide", "Windows", 36f);
            _search = _parts.Label(search, "Text", FontRole.Body, 17f, t.TextFlavor, 16f, 0f, PiecesW - 76f, 36f, TextAlignmentOptions.MidlineLeft);
            _parts.Clickable(search, "build search", FocusSearch);

            _grid = new ScrollArea(pieces, "Grid", 22f, 156f, PiecesW - 44f, Height - 180f, t, Pitch);
            _empty = _parts.Label(_grid.Content, "Empty", FontRole.Body, 17f, t.TextFlavor, 10f, 40f, PiecesW - 74f, 60f, TextAlignmentOptions.Center);
            _empty.textWrappingMode = TextWrappingModes.Normal;
            _empty.text = Localize("$genesisui_build_empty");

            var tagList = new ScrollArea(tags, "List", 20f, 64f, TagsW - 40f, Height - 88f, t, 46f);
            for (int i = 0; i < MaxTags; i++)
            {
                int index = i;
                var row = WindowCanvas.At(tagList.Content, "Tag " + i, 0f, i * 46f, TagsW - 50f, 42f);
                Frame.Dress(row, _theme, "keycap_wide", "Windows", 42f);
                var mark = Ui.Image(Ui.Fill(Ui.Child(row, "Selected"), 3f, 3f, 3f, 3f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.18f));
                var label = _parts.Label(row, "Label", FontRole.Body, 16f, t.TextTitle, 14f, 0f, TagsW - 78f, 42f, TextAlignmentOptions.MidlineLeft);
                _parts.Clickable(row, "build tag", () => PressTag(index));
                row.gameObject.SetActive(false);
                _tags.Add((row, label, mark));
            }
            tagList.ContentHeight = MaxTags * 46f;

            const float pad = 24f, w = DetailsW - 2f * pad;
            _detailBody = WindowCanvas.At(details, "Body", 0f, 0f, DetailsW, Height).gameObject;
            var body = (RectTransform)_detailBody.transform;
            _icon = Ui.Image(WindowCanvas.At(body, "Icon", pad, 66f, w, 110f), null, Color.white);
            _icon.preserveAspect = true;
            _name = _parts.Label(body, "Name", FontRole.Display, 20f, t.AccentGoldBright, pad, 186f, w, 28f, TextAlignmentOptions.Left);
            _description = _parts.Label(body, "Description", FontRole.Body, 15f, t.TextBody, pad, 216f, w, 60f, TextAlignmentOptions.TopLeft);
            _description.textWrappingMode = TextWrappingModes.Normal;
            _description.enableAutoSizing = false;
            var title = _parts.Label(body, "MaterialsTitle", FontRole.Label, 14f, t.AccentGoldBright, pad, 286f, w, 20f, TextAlignmentOptions.Left);
            title.text = Localize("$genesisui_materials_needed").ToUpperInvariant();
            title.characterSpacing = 3f;
            for (int i = 0; i < MaxMaterials; i++)
            {
                var m = new Material { Root = WindowCanvas.At(body, "Material " + i, pad, 312f + i * 40f, w, 38f) };
                m.Icon = Ui.Image(WindowCanvas.At(m.Root, "Icon", 0f, 3f, 32f, 32f), null, Color.white);
                m.Icon.preserveAspect = true;
                m.Name = _parts.Label(m.Root, "Name", FontRole.Body, 16f, t.TextTitle, 42f, 0f, w - 130f, 38f, TextAlignmentOptions.MidlineLeft);
                m.Amount = _parts.Label(m.Root, "Amount", FontRole.Display, 15f, t.StatePositive, w - 90f, 0f, 90f, 38f, TextAlignmentOptions.MidlineRight);
                m.Root.gameObject.SetActive(false);
                _materials.Add(m);
            }
            _station = _parts.Label(body, "Station", FontRole.Body, 15f, t.TextFlavor, pad, 516f, w, 40f, TextAlignmentOptions.TopLeft);
            _station.textWrappingMode = TextWrappingModes.Normal;
            _favorite = _parts.Button(body, "Favorite", pad, 562f, w, 38f, null, 16f, "build favorite", () => ToggleFavorite(_detailPiece), out _favoriteLabel);
            _detailBody.SetActive(false);

            // How to build, like the concept's "Posicionar" box.
            var help = _parts.Label(details, "Help", FontRole.Body, 15f, t.TextFlavor, pad, 616f, w, 140f, TextAlignmentOptions.TopLeft);
            help.textWrappingMode = TextWrappingModes.Normal;
            help.enableAutoSizing = false;
            help.text = Localize("$genesisui_build_help");
        }

        private PieceCell MakeCell(int index)
        {
            var t = _theme.Tokens;
            var cell = new PieceCell { Root = WindowCanvas.At(_grid.Content, "Piece " + index, 0f, 0f, Cell, Cell) };
            Frame.Dress(cell.Root, _theme, "hotslot", "Windows", Cell);
            cell.Icon = Ui.Image(WindowCanvas.At(cell.Root, "Icon", 10f, 10f, Cell - 20f, Cell - 20f), null, Color.white);
            cell.Icon.preserveAspect = true;
            var sel = _theme.Sprite("hotslot_selected");
            cell.Selected = Ui.Image(Ui.Place(Ui.Child(cell.Root, "Selected"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Cell, Cell) * (64f / 56f)), sel, Color.white);
            cell.Selected.type = Image.Type.Simple;
            cell.Selected.enabled = false;
            var knot = _theme.Sprite("tab_knot");
            cell.Favorite = Ui.Image(WindowCanvas.At(cell.Root, "Favorite", Cell - 16f, 5f, 10f, 14f), knot, ThemeRuntime.ToUnity(t.AccentGoldBright));
            cell.Favorite.enabled = false;
            var hit = Ui.Image(cell.Root, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            cell.Root.gameObject.AddComponent<CellInput>().Init(this, cell);
            return cell;
        }

        private static string Localize(string text) => WindowParts.Localize(text);

        /// <summary>Left click selects (vanilla's button), middle click favourites, hover shows details.</summary>
        private sealed class CellInput : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
        {
            private BuildMenuModule _owner;
            private PieceCell _cell;
            internal void Init(BuildMenuModule owner, PieceCell cell) { _owner = owner; _cell = cell; }

            public void OnPointerClick(PointerEventData e)
            {
                if (e.button == PointerEventData.InputButton.Left) Guard.Try("build piece", () => _owner.PressPiece(_cell));
                // Middle click favourites, like vanilla's menu (right click toggles the build menu itself).
                else if (e.button == PointerEventData.InputButton.Middle) Guard.Try("build favourite", () => _owner.ToggleFavorite(_cell.Source != null ? _cell.Source.Piece : null));
            }

            public void OnPointerEnter(PointerEventData e) => Guard.Try("build hover", () => _owner.HoverPiece(_cell));
        }
    }
}
