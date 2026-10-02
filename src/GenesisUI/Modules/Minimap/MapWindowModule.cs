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

namespace GenesisUI.Modules.Minimap
{
    /// <summary>
    /// The large map, ConceptArt (11) (D-032): vanilla's map stays exactly vanilla (drawing, pins,
    /// zoom, drag, double click to pin, right click to remove, middle click to ping); GenesisUI draws
    /// the chrome around it — the title plate with the day and the biome under the cursor, the pin
    /// filters, the marker palette, the "visible to others" switch, zoom and "to the player" — and
    /// hides vanilla's own chrome. Every button calls vanilla's public map methods.
    /// </summary>
    [GameContract("assembly_valheim", "Minimap", "SetMapMode")]
    [GameContract("assembly_valheim", "Minimap", "get_instance")]
    [GameContract("assembly_valheim", "Minimap", "IsOpen")]
    [GameContract("assembly_valheim", "Minimap", "m_mode", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Minimap\u002BMapMode")]
    [GameContract("assembly_valheim", "Minimap", "m_largeRoot", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.GameObject")]
    [GameContract("assembly_valheim", "Minimap", "m_biomeNameLarge", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "TMPro.TMP_Text")]
    [GameContract("assembly_valheim", "Minimap", "m_publicPosition", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Toggle")]
    [GameContract("assembly_valheim", "Minimap", "m_hints", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.List\u00601[[UnityEngine.GameObject, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_valheim", "Minimap", "m_icons", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.List\u00601[[Minimap\u002BSpriteData, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_valheim", "Minimap", "m_selectedIcon0", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Image")]
    [GameContract("assembly_valheim", "Minimap", "m_selectedIcon1", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Image")]
    [GameContract("assembly_valheim", "Minimap", "m_selectedIcon2", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Image")]
    [GameContract("assembly_valheim", "Minimap", "m_selectedIcon3", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Image")]
    [GameContract("assembly_valheim", "Minimap", "m_selectedIcon4", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Image")]
    [GameContract("assembly_valheim", "Minimap", "m_selectedIconPing", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Image")]
    [GameContract("assembly_valheim", "Minimap", "m_minZoom", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GameContract("assembly_valheim", "Minimap", "m_maxZoom", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GameContract("assembly_valheim", "Minimap", "m_visibleIconTypes", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean[]")]
    [GameContract("assembly_valheim", "Minimap", "m_showSharedMapData", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Boolean")]
    [GameContract("assembly_valheim", "Minimap", "m_mapOffset", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Vector3")]
    [GameContract("assembly_valheim", "Minimap", "get_LargeZoom")]
    [GameContract("assembly_valheim", "Minimap", "set_LargeZoom")]
    [GameContract("assembly_valheim", "Minimap", "OnPressedIcon0")]
    [GameContract("assembly_valheim", "Minimap", "OnPressedIcon1")]
    [GameContract("assembly_valheim", "Minimap", "OnPressedIcon2")]
    [GameContract("assembly_valheim", "Minimap", "OnPressedIcon3")]
    [GameContract("assembly_valheim", "Minimap", "OnPressedIcon4")]
    [GameContract("assembly_valheim", "Minimap", "OnPressedPingIcon")]
    [GameContract("assembly_valheim", "Minimap", "OnAltPressedIcon0")]
    [GameContract("assembly_valheim", "Minimap", "OnAltPressedIcon1")]
    [GameContract("assembly_valheim", "Minimap", "OnAltPressedIcon2")]
    [GameContract("assembly_valheim", "Minimap", "OnAltPressedIcon3")]
    [GameContract("assembly_valheim", "Minimap", "OnAltPressedIcon4")]
    [GameContract("assembly_valheim", "Minimap", "OnAltPressedIconBoss")]
    [GameContract("assembly_valheim", "Minimap", "OnAltPressedIconDeath")]
    [GameContract("assembly_valheim", "Minimap", "OnToggleSharedMapData")]
    [GameContract("assembly_valheim", "EnvMan", "GetDayFraction")]
    [GameContract("assembly_valheim", "Minimap", "m_namePin", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Minimap\u002BPinData")]
    [GameContract("assembly_valheim", "Minimap", "m_mapImageLarge", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.RawImage")]
    [GameContract("assembly_valheim", "Minimap", "m_pinRootLarge", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "Minimap", "m_pinNameRootLarge", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "Minimap", "m_largeMarker", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "Minimap", "m_largeShipMarker", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "Minimap", "m_gamepadCrosshair", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GameContract("assembly_valheim", "Minimap", "m_nameInput", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "GUIFramework.GuiInputField")]
    [GameContract("assembly_valheim", "Minimap", "m_pins", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.List\u00601[[Minimap\u002BPinData, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_valheim", "Minimap+PinData", "m_uiElement", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.RectTransform")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Player")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "EnvMan", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "EnvMan")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "EnvMan", "GetDay", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Minimap\u002BPinData", "m_type", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Minimap\u002BPinType")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Minimap\u002BPinData", "m_name", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Minimap\u002BSpriteData", "m_name", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Minimap\u002BPinType")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Minimap\u002BSpriteData", "m_icon", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Sprite")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Host.ModuleContext), typeof(GenesisUI.Widgets.WindowParts), typeof(GenesisUI.Widgets.WindowCanvas), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Widgets.ColorExtensions), typeof(GenesisUI.Widgets.OneShotLight), typeof(GenesisUI.Foundation.VanillaSkin), typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Widgets.Frame))]
    internal sealed class MapWindowModule : IUiModule, IRecoverable
    {
        /// <summary>IRecoverable: on a fault the large map closes back to the minimap.</summary>
        public void CloseVanillaWindow()
        {
            var map = global::Minimap.instance;
            if (map != null && global::Minimap.IsOpen()) map.SetMapMode(global::Minimap.MapMode.Small);
        }

        private const string Owner = "module:win.map";
        private static readonly string[] NoRegions = new string[0];

        private static readonly global::Minimap.PinType[] Palette =
        {
            global::Minimap.PinType.Icon0, global::Minimap.PinType.Icon1, global::Minimap.PinType.Icon2,
            global::Minimap.PinType.Icon3, global::Minimap.PinType.Icon4, global::Minimap.PinType.Ping,
        };

        private static readonly (global::Minimap.PinType Type, string Token)[] Filters =
        {
            (global::Minimap.PinType.Icon0, "$genesisui_map_pin_fire"), (global::Minimap.PinType.Icon1, "$genesisui_map_pin_home"),
            (global::Minimap.PinType.Icon2, "$genesisui_map_pin_hammer"), (global::Minimap.PinType.Icon3, "$genesisui_map_pin_point"),
            (global::Minimap.PinType.Icon4, "$genesisui_map_pin_rune"), (global::Minimap.PinType.Boss, "$genesisui_map_pin_boss"),
            (global::Minimap.PinType.Death, "$genesisui_map_pin_death"),
        };

        private readonly VanillaSkin _skin = new VanillaSkin(Owner);
        private readonly List<(global::Minimap.PinType Type, Image Mark)> _palette = new List<(global::Minimap.PinType, Image)>();
        private readonly List<(global::Minimap.PinType Type, Image Dot)> _filters = new List<(global::Minimap.PinType, Image)>();
        private readonly Dictionary<global::Minimap.PinType, Image> _vanillaSelected = new Dictionary<global::Minimap.PinType, Image>();

        private ThemeRuntime _theme;
        private WindowParts _parts;
        private RectTransform _root, _board;
        private TextMeshProUGUI _day, _biome, _publicLabel;
        private RectTransform _publicKnob;
        private bool _applied;
        private float _slowIn;

        private readonly List<(MapMarkersModule.Kind Kind, Image Mark)> _custom = new List<(MapMarkersModule.Kind, Image)>();
        private MapMarkersModule.Kind _armed;
        private global::Minimap.PinData _tagged;
        private RectTransform _mapArea;
        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _sharedKnob;
        private AccessTools.FieldRef<global::Minimap, global::Minimap.PinData> _namePin;
        private AccessTools.FieldRef<global::Minimap, bool[]> _visible;
        private AccessTools.FieldRef<global::Minimap, bool> _shared;
        private AccessTools.FieldRef<global::Minimap, Vector3> _offset;
        private AccessTools.FieldRef<global::Minimap, List<global::Minimap.PinData>> _pins;
        // A pin placed while the map is open: a ring of light spreads from it once.
        private OneShotLight _ring;
        private int _pinCount = -1;
        private global::Minimap.PinData _newPin;

        /// <summary>Whether the map chrome is on (the window shell then frames the open map with its bars).</summary>
        internal static bool Active { get; private set; }

        public string Id => "win.map";
        public string NameToken => "$genesisui_module_map_window";
        public IReadOnlyList<string> Regions => new[] { Id };
        public float RefreshRate => 0f;

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _parts = new WindowParts(_theme);
            _visible = AccessTools.FieldRefAccess<global::Minimap, bool[]>("m_visibleIconTypes");
            _shared = AccessTools.FieldRefAccess<global::Minimap, bool>("m_showSharedMapData");
            _offset = AccessTools.FieldRefAccess<global::Minimap, Vector3>("m_mapOffset");
            _namePin = AccessTools.FieldRefAccess<global::Minimap, global::Minimap.PinData>("m_namePin");
            _pins = AccessTools.FieldRefAccess<global::Minimap, List<global::Minimap.PinData>>("m_pins");
            _applied = false;
            _armed = null;
            _tagged = null;
            Active = true;
        }

        public void Refresh(float deltaSeconds)
        {
            var map = global::Minimap.instance;
            bool open = map != null && Player.m_localPlayer != null && map.m_mode == global::Minimap.MapMode.Large && map.m_largeRoot != null && map.m_largeRoot.activeInHierarchy;
            if (!open)
            {
                if (_applied) Unapply();
                return;
            }
            if (!EnsureBuilt(map)) return;
            if (!_applied) Apply(map);
            WindowCanvas.Fit(_board, 0f); // the map under the chrome does not move: neither does the chrome

            FitMap(map);
            TagNewPin(map);
            RingNewPin(map);
            string biome = map.m_biomeNameLarge != null ? map.m_biomeNameLarge.text : "";
            if (!string.IsNullOrEmpty(_hint)) biome = _hint;
            if (_biome.text != biome) _biome.text = biome;
            foreach (var (type, mark) in _palette)
            {
                bool on = _armed == null && _vanillaSelected.TryGetValue(type, out var img) && img != null && img.enabled;
                if (mark.enabled != on) mark.enabled = on;
            }
            foreach (var (kind, mark) in _custom)
            {
                bool on = kind == _armed;
                if (mark.enabled != on) mark.enabled = on;
            }
            _slowIn -= deltaSeconds;
            if (_slowIn > 0f) return;
            _slowIn = 0.25f;
            var visible = _visible(map);
            foreach (var (type, dot) in _filters)
            {
                bool on = visible != null && (int)type < visible.Length && visible[(int)type];
                dot.color = ThemeRuntime.ToUnity(on ? _theme.Tokens.AccentGoldBright : _theme.Tokens.TextFlavor).WithA(on ? 1f : 0.35f);
            }
            _sharedKnob.anchoredPosition = new Vector2(_shared(map) ? 35f : 3f, -3f);
            bool pub = map.m_publicPosition != null && map.m_publicPosition.isOn;
            _publicKnob.anchoredPosition = new Vector2(pub ? 35f : 3f, -3f);
            _publicLabel.color = ThemeRuntime.ToUnity(pub ? _theme.Tokens.TextTitle : _theme.Tokens.TextFlavor);
            if (EnvMan.instance != null)
            {
                float f = EnvMan.instance.GetDayFraction();
                int minutes = Mathf.FloorToInt(f * 24f * 60f);
                _day.text = WindowParts.Localize("$genesisui_day") + " " + EnvMan.instance.GetDay() + "  ·  " + (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00");
            }
        }

        public void Teardown()
        {
            Active = false;
            if (_applied) Unapply();
            if (_root != null) Object.Destroy(_root.gameObject);
            _root = null;
            _ring = null;
            _palette.Clear();
            _custom.Clear();
            _filters.Clear();
            _vanillaSelected.Clear();
        }

        private bool EnsureBuilt(global::Minimap map)
        {
            if (_root != null) return true;
            _root = WindowCanvas.CreateRoot(map.m_largeRoot.transform, "GenesisUI.Map", behind: false);
            if (_root == null) return false;
            _board = WindowCanvas.Area(_root, "Board");
            Draw(map);
            _ring = OneShotLight.Ring(_root, _theme, 150f);
            _root.gameObject.SetActive(false);
            return true;
        }

        private void Apply(global::Minimap map)
        {
            _applied = true;
            // Vanilla's chrome over the map (pin palette, public switch, biome text, key hints) hides;
            // the map, its pins, markers and name input stay.
            if (map.m_selectedIcon0 != null && map.m_selectedIcon0.transform.parent != null && map.m_selectedIcon0.transform.parent.parent != null)
                Hide(map.m_selectedIcon0.transform.parent.parent.gameObject);
            if (map.m_publicPosition != null) Hide(map.m_publicPosition.gameObject);
            if (map.m_biomeNameLarge != null) Hide(map.m_biomeNameLarge.gameObject);
            if (map.m_hints != null) foreach (var h in map.m_hints) if (h != null) Hide(h);
            // And everything else vanilla draws in the large map that is not the map itself: the boss
            // and death filters and a dark hint panel still showed under our chrome (R-060 print).
            // Kept: the map image, pins, pin names, the player and ship markers, the name input.
            _keep.Clear();
            Keep(map.m_mapImageLarge);
            Keep(map.m_pinRootLarge);
            Keep(map.m_pinNameRootLarge);
            Keep(map.m_largeMarker);
            Keep(map.m_largeShipMarker);
            Keep(map.m_gamepadCrosshair);
            Keep(map.m_nameInput);
            HideAllBut(map.m_largeRoot.transform);
            // The map itself goes inside our frame (vanilla lays the map, pins and markers out from this rect).
            _skin.Rect((RectTransform)map.m_largeRoot.transform);
            _armed = null;
            _tagged = null;
            _pinCount = -1;
            _newPin = null;
            _root.gameObject.SetActive(true);
            _slowIn = 0f;
            GenesisLog.Info(Owner, "map chrome shown; vanilla chrome hidden (" + _skin.Count + " change(s))");
        }

        private void Hide(GameObject go)
        {
            _skin.Hidden(go, interactable: null);
        }

        private readonly HashSet<Transform> _keep = new HashSet<Transform>();

        private void Keep(Component c)
        {
            if (c != null) _keep.Add(c.transform);
        }

        /// <summary>Hides each child that neither is a kept object nor holds one; walks into those that hold one.</summary>
        private void HideAllBut(Transform node)
        {
            for (int i = 0; i < node.childCount; i++)
            {
                var child = node.GetChild(i);
                if (child == _root || _keep.Contains(child)) continue;
                if (HoldsKept(child)) HideAllBut(child);
                else Hide(child.gameObject); // inactive ones too: vanilla shows some later (shared-map hint)
            }
        }

        private bool HoldsKept(Transform t)
        {
            foreach (var k in _keep)
                if (k != null && k.IsChildOf(t)) return true;
            return false;
        }

        private void Unapply()
        {
            _applied = false;
            _skin.Restore();
            if (_root != null) _root.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ building

        private void Draw(global::Minimap map)
        {
            var t = _theme.Tokens;
            _vanillaSelected.Clear();
            Map(map.m_selectedIcon0, global::Minimap.PinType.Icon0);
            Map(map.m_selectedIcon1, global::Minimap.PinType.Icon1);
            Map(map.m_selectedIcon2, global::Minimap.PinType.Icon2);
            Map(map.m_selectedIcon3, global::Minimap.PinType.Icon3);
            Map(map.m_selectedIcon4, global::Minimap.PinType.Icon4);
            Map(map.m_selectedIconPing, global::Minimap.PinType.Ping);

            // The window: a panel like every other, the map inside it (FitMap), the chrome on its header.
            var panel = _parts.Panel(_board, "Map", 0f, 102f, WindowCanvas.Design.x, 673f, null, 0f, 0f, TextAlignmentOptions.Left);
            // The panel draws in front of the map: its click blocker would take every click from
            // vanilla's map image (drag, pins, ping), which is the map's only input (R-059). Only the
            // chrome on it takes clicks.
            var blocker = panel.GetComponent<Image>();
            if (blocker != null) blocker.raycastTarget = false;
            _mapArea = WindowCanvas.At(panel, "MapArea", 14f, 62f, WindowCanvas.Design.x - 28f, 673f - 76f);

            // Title plate: map name, day and time, the biome under the cursor.
            float x = 16f, y = 114f;
            var plate = WindowCanvas.At(_board, "Title", x, y, 470f, 44f);
            Ui.Image(plate, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(plate, _theme, "keycap_wide", "Windows", 44f);
            var title = _parts.Label(plate, "Name", FontRole.Display, 19f, t.AccentGoldBright, 18f, 0f, 190f, 44f, TextAlignmentOptions.MidlineLeft);
            title.text = WindowParts.Localize("$genesisui_map_title").ToUpperInvariant();
            title.characterSpacing = 4f;
            _day = _parts.Label(plate, "Day", FontRole.Body, 16f, t.TextFlavor, 210f, 0f, 250f, 44f, TextAlignmentOptions.MidlineLeft);
            var biomePlate = WindowCanvas.At(_board, "Biome", x + 480f, y, 270f, 44f);
            Ui.Image(biomePlate, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(biomePlate, _theme, "keycap_wide", "Windows", 44f);
            var under = _parts.Label(biomePlate, "Under", FontRole.Label, 12f, t.TextFlavor, 16f, 2f, 238f, 18f, TextAlignmentOptions.Left);
            under.text = WindowParts.Localize("$genesisui_map_under_cursor").ToUpperInvariant();
            _biome = _parts.Label(biomePlate, "Name", FontRole.Body, 17f, t.TextTitle, 16f, 18f, 238f, 24f, TextAlignmentOptions.Left);

            // Pin filters: vanilla's own filter toggles (the "alt" press of its icons).
            float fx = x + 760f;
            for (int i = 0; i < Filters.Length; i++)
            {
                var (type, token) = Filters[i];
                var rt = WindowCanvas.At(_board, "Filter " + type, fx, y + 4f, 104f, 36f);
                Ui.Image(rt, null, new Color(0f, 0f, 0f, 0f), raycast: true);
                Frame.Dress(rt, _theme, "keycap_wide", "Windows", 36f);
                var icon = Ui.Image(WindowCanvas.At(rt, "Icon", 8f, 6f, 24f, 24f), SpriteFor(map, type), Color.white);
                icon.preserveAspect = true;
                var label = _parts.Label(rt, "Label", FontRole.Body, 14f, t.TextTitle, 36f, 0f, 52f, 36f, TextAlignmentOptions.MidlineLeft);
                label.text = WindowParts.Localize(token);
                var dot = Ui.Image(WindowCanvas.At(rt, "On", 88f, 12f, 10f, 12f), _theme.Sprite("tab_knot"), Color.white);
                _parts.Clickable(rt, "map filter", () => ToggleFilter(type));
                _filters.Add((type, dot));
                fx += 110f;
                if (fx > WindowCanvas.Design.x - 110f) break;
            }
            // Marker palette on the right, two columns: vanilla's markers, then GenesisUI's own.
            MapMarkersModule.ResolveIcons();
            float pw = 132f, px = WindowCanvas.Design.x - 26f - pw, py = 182f;
            int count = Palette.Length + MapMarkersModule.Kinds.Length;
            var palette = WindowCanvas.At(_board, "Palette", px, py, pw, 40f + ((count + 1) / 2) * 60f);
            Ui.Image(palette, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(palette, _theme, "card", "Windows");
            var ptitle = _parts.Label(palette, "Title", FontRole.Label, 11f, t.AccentGoldBright, 4f, 10f, pw - 8f, 16f, TextAlignmentOptions.Center);
            ptitle.text = WindowParts.Localize("$genesisui_map_marker").ToUpperInvariant();
            ptitle.characterSpacing = 3f;
            int slot = 0;
            foreach (var type in Palette)
            {
                var pinType = type;
                var mark = PaletteCell(palette, slot++, SpriteFor(map, type), null, () => SelectPin(pinType));
                _palette.Add((type, mark));
            }
            foreach (var kind in MapMarkersModule.Kinds)
            {
                var k = kind;
                var mark = PaletteCell(palette, slot++, kind.Icon, kind.Token, () => Arm(k));
                _custom.Add((kind, mark));
            }

            // Bottom right: visible to others, group markers, zoom and back to the player.
            var card = WindowCanvas.At(_board, "Controls", px - 330f - 10f, 775f - 22f - 150f, 330f, 150f);
            Ui.Image(card, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(card, _theme, "card", "Windows");
            _publicKnob = Switch(card, 14f, "$genesisui_map_public", () => { if (map.m_publicPosition != null) map.m_publicPosition.isOn = !map.m_publicPosition.isOn; }, out _publicLabel);
            _sharedKnob = Switch(card, 50f, "$genesisui_map_shared", () => map.OnToggleSharedMapData(), out _);
            _parts.Button(card, "ZoomOut", 16f, 94f, 44f, 40f, null, 20f, "map zoom out", () => Zoom(map, 1.5f), out var minus);
            minus.text = "−";
            _parts.Button(card, "ZoomIn", 68f, 94f, 44f, 40f, null, 20f, "map zoom in", () => Zoom(map, 1f / 1.5f), out var plus);
            plus.text = "+";
            _parts.Button(card, "Center", 122f, 94f, 192f, 40f, "$genesisui_map_center", 15f, "map center", () => _offset(map) = Vector3.zero, out _);
        }

        private RectTransform Switch(RectTransform card, float y, string token, System.Action onClick, out TextMeshProUGUI label)
        {
            var sw = WindowCanvas.At(card, "Switch " + token, 16f, y, 56f, 26f);
            Frame.Dress(sw, _theme, "keycap_wide", "Windows", 26f);
            var knob = WindowCanvas.At(sw, "Knob", 3f, 3f, 20f, 20f);
            Frame.Dress(knob, _theme, "keycap", "Windows", 20f);
            label = _parts.Label(card, "Label " + token, FontRole.Body, 15f, _theme.Tokens.TextTitle, 82f, y - 3f, 236f, 32f, TextAlignmentOptions.MidlineLeft);
            label.text = WindowParts.Localize(token);
            _parts.Clickable(sw, "map switch", onClick);
            return knob;
        }

        private Image PaletteCell(RectTransform palette, int slot, Sprite sprite, string token, System.Action onClick)
        {
            var cell = WindowCanvas.At(palette, "Pin " + slot, 8f + (slot % 2) * 60f, 32f + (slot / 2) * 60f, 52f, 52f);
            Frame.Dress(cell, _theme, "hotslot", "Windows", 52f);
            var icon = Ui.Image(WindowCanvas.At(cell, "Icon", 10f, 10f, 32f, 32f), sprite, Color.white);
            icon.preserveAspect = true;
            var mark = Ui.Image(Ui.Place(Ui.Child(cell, "Selected"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f) * (64f / 56f)), _theme.Sprite("hotslot_selected"), Color.white);
            mark.type = Image.Type.Simple;
            mark.enabled = false;
            _parts.Clickable(cell, "map pin type", onClick);
            if (token != null) cell.gameObject.AddComponent<PaletteHint>().Init(this, token);
            return mark;
        }

        /// <summary>Hovering a custom marker names it in the biome plate's place (a short, clear hint).</summary>
        private sealed class PaletteHint : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
        {
            private MapWindowModule _owner;
            private string _token;
            internal void Init(MapWindowModule owner, string token) { _owner = owner; _token = token; }
            public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) => Guard.Run("module:win.map", () => _owner._hint = WindowParts.Localize(_token));
            public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) => _owner._hint = null;
        }

        private string _hint;

        /// <summary>A custom marker picked: vanilla places a "point" pin; the next new pin gets the tag.</summary>
        private void Arm(MapMarkersModule.Kind kind)
        {
            var map = global::Minimap.instance;
            if (map == null) return;
            map.OnPressedIcon3();
            _armed = kind;
            _tagged = null;
        }

        /// <summary>Vanilla created the pin being named (double click): give it the armed kind's tag, and keep
        /// the tag when the player types its name.</summary>
        private void TagNewPin(global::Minimap map)
        {
            if (_armed == null) { _tagged = null; return; }
            var pin = _namePin(map);
            if (pin != null && pin != _tagged && pin.m_type == global::Minimap.PinType.Icon3 && string.IsNullOrEmpty(pin.m_name))
            {
                _tagged = pin;
                pin.m_name = _armed.Tag;
            }
            if (_tagged != null && (_tagged.m_name == null || !_tagged.m_name.StartsWith(_armed.Tag, System.StringComparison.Ordinal)))
                _tagged.m_name = _armed.Tag + " " + (_tagged.m_name ?? "");
            if (pin == null && _tagged != null) _tagged = null; // naming finished
        }

        /// <summary>
        /// A pin added while the map is open (by the player, or a ping) gets a ring of light once its
        /// marker exists (vanilla makes it on its next update). Many at once (shared map data) get none.
        /// </summary>
        private void RingNewPin(global::Minimap map)
        {
            if (_ring == null) return;
            var pins = _pins(map);
            int count = pins != null ? pins.Count : 0;
            if (_pinCount >= 0 && count > _pinCount && count - _pinCount <= 2) _newPin = pins[count - 1];
            _pinCount = count;
            if (_newPin == null || _newPin.m_uiElement == null) return;
            _ring.Follow = _newPin.m_uiElement;
            _ring.Play();
            _newPin = null;
        }

        /// <summary>Vanilla's large map (root of the map, pins and markers) inside our frame, every frame.</summary>
        private void FitMap(global::Minimap map)
        {
            var rt = (RectTransform)map.m_largeRoot.transform;
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            _mapArea.GetWorldCorners(_corners);
            Vector2 min = parent.InverseTransformPoint(_corners[0]);
            Vector2 max = parent.InverseTransformPoint(_corners[2]);
            var size = max - min;
            var center = (min + max) / 2f - parent.rect.center;
            if (rt.anchorMin != new Vector2(0.5f, 0.5f) || rt.anchorMax != new Vector2(0.5f, 0.5f))
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
            }
            if ((rt.sizeDelta - size).sqrMagnitude > 0.25f) rt.sizeDelta = size;
            if ((rt.anchoredPosition - center).sqrMagnitude > 0.25f) rt.anchoredPosition = center;
        }

        private void Map(Image selected, global::Minimap.PinType type)
        {
            if (selected != null) _vanillaSelected[type] = selected;
        }

        private static Sprite SpriteFor(global::Minimap map, global::Minimap.PinType type)
        {
            if (map.m_icons == null) return null;
            foreach (var s in map.m_icons) if (s.m_name == type) return s.m_icon;
            return null;
        }

        private static void Zoom(global::Minimap map, float factor)
        {
            map.LargeZoom = Mathf.Clamp(map.LargeZoom * factor, map.m_minZoom, map.m_maxZoom);
        }

        private void SelectPin(global::Minimap.PinType type)
        {
            _armed = null;
            var map = global::Minimap.instance;
            if (map == null) return;
            switch (type)
            {
                case global::Minimap.PinType.Icon0: map.OnPressedIcon0(); break;
                case global::Minimap.PinType.Icon1: map.OnPressedIcon1(); break;
                case global::Minimap.PinType.Icon2: map.OnPressedIcon2(); break;
                case global::Minimap.PinType.Icon3: map.OnPressedIcon3(); break;
                case global::Minimap.PinType.Icon4: map.OnPressedIcon4(); break;
                case global::Minimap.PinType.Ping: map.OnPressedPingIcon(); break;
            }
        }

        private void ToggleFilter(global::Minimap.PinType type)
        {
            var map = global::Minimap.instance;
            if (map == null) return;
            switch (type)
            {
                case global::Minimap.PinType.Icon0: map.OnAltPressedIcon0(); break;
                case global::Minimap.PinType.Icon1: map.OnAltPressedIcon1(); break;
                case global::Minimap.PinType.Icon2: map.OnAltPressedIcon2(); break;
                case global::Minimap.PinType.Icon3: map.OnAltPressedIcon3(); break;
                case global::Minimap.PinType.Icon4: map.OnAltPressedIcon4(); break;
                case global::Minimap.PinType.Boss: map.OnAltPressedIconBoss(); break;
                case global::Minimap.PinType.Death: map.OnAltPressedIconDeath(); break;
            }
            _slowIn = 0f;
        }
    }
}
