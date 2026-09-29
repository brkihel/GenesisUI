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
    [GameContract("assembly_valheim", "Minimap", "m_mode")]
    [GameContract("assembly_valheim", "Minimap", "m_largeRoot")]
    [GameContract("assembly_valheim", "Minimap", "m_biomeNameLarge")]
    [GameContract("assembly_valheim", "Minimap", "m_publicPosition")]
    [GameContract("assembly_valheim", "Minimap", "m_hints")]
    [GameContract("assembly_valheim", "Minimap", "m_icons")]
    [GameContract("assembly_valheim", "Minimap", "m_selectedIcon0")]
    [GameContract("assembly_valheim", "Minimap", "m_selectedIcon1")]
    [GameContract("assembly_valheim", "Minimap", "m_selectedIcon2")]
    [GameContract("assembly_valheim", "Minimap", "m_selectedIcon3")]
    [GameContract("assembly_valheim", "Minimap", "m_selectedIcon4")]
    [GameContract("assembly_valheim", "Minimap", "m_selectedIconPing")]
    [GameContract("assembly_valheim", "Minimap", "m_minZoom")]
    [GameContract("assembly_valheim", "Minimap", "m_maxZoom")]
    [GameContract("assembly_valheim", "Minimap", "m_visibleIconTypes")]
    [GameContract("assembly_valheim", "Minimap", "m_showSharedMapData")]
    [GameContract("assembly_valheim", "Minimap", "m_mapOffset")]
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
        private Image _sharedDot;
        private RectTransform _publicKnob;
        private bool _applied;
        private float _slowIn;

        private AccessTools.FieldRef<global::Minimap, bool[]> _visible;
        private AccessTools.FieldRef<global::Minimap, bool> _shared;
        private AccessTools.FieldRef<global::Minimap, Vector3> _offset;

        /// <summary>Whether the map chrome is on (the window shell then frames the open map with its bars).</summary>
        internal static bool Active { get; private set; }

        public string Id => "win.map";
        public string NameToken => "$genesisui_module_map_window";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f;

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _parts = new WindowParts(_theme);
            _visible = AccessTools.FieldRefAccess<global::Minimap, bool[]>("m_visibleIconTypes");
            _shared = AccessTools.FieldRefAccess<global::Minimap, bool>("m_showSharedMapData");
            _offset = AccessTools.FieldRefAccess<global::Minimap, Vector3>("m_mapOffset");
            _applied = false;
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
            WindowCanvas.Fit(_board);

            string biome = map.m_biomeNameLarge != null ? map.m_biomeNameLarge.text : "";
            if (_biome.text != biome) _biome.text = biome;
            foreach (var (type, mark) in _palette)
            {
                bool on = _vanillaSelected.TryGetValue(type, out var img) && img != null && img.enabled;
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
            _sharedDot.color = ThemeRuntime.ToUnity(_shared(map) ? _theme.Tokens.AccentGoldBright : _theme.Tokens.TextFlavor).WithA(_shared(map) ? 1f : 0.35f);
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
            _palette.Clear();
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
            _root.gameObject.SetActive(true);
            _slowIn = 0f;
            GenesisLog.Info(Owner, "map chrome shown; vanilla chrome hidden (" + _skin.Count + " change(s))");
        }

        private void Hide(GameObject go)
        {
            var g = _skin.Group(go);
            g.alpha = 0f;
            g.blocksRaycasts = false;
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

            // The map's frame: the thin metal frame over the whole map area, no background (the map shows).
            var frame = WindowCanvas.At(_board, "Frame", 0f, 102f, WindowCanvas.Design.x, 673f);
            var img = Ui.Image(frame, _theme.Sprite("window_panel"), Color.white);
            img.pixelsPerUnitMultiplier = Frame.CanvasScale(frame);

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
            var shared = WindowCanvas.At(_board, "Shared", WindowCanvas.Design.x - 16f - 190f, y + 52f, 190f, 36f);
            Ui.Image(shared, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(shared, _theme, "keycap_wide", "Windows", 36f);
            _parts.Label(shared, "Label", FontRole.Body, 14f, t.TextTitle, 12f, 0f, 150f, 36f, TextAlignmentOptions.MidlineLeft).text = WindowParts.Localize("$genesisui_map_shared");
            _sharedDot = Ui.Image(WindowCanvas.At(shared, "On", 170f, 12f, 10f, 12f), _theme.Sprite("tab_knot"), Color.white);
            _parts.Clickable(shared, "map shared", () => map.OnToggleSharedMapData());

            // Marker palette on the right, like the concept.
            float px = WindowCanvas.Design.x - 16f - 64f, py = 214f;
            var palette = WindowCanvas.At(_board, "Palette", px, py, 64f, 40f + Palette.Length * 66f);
            Ui.Image(palette, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(palette, _theme, "card", "Windows");
            var ptitle = _parts.Label(palette, "Title", FontRole.Label, 10f, t.AccentGoldBright, 2f, 10f, 60f, 16f, TextAlignmentOptions.Center);
            ptitle.text = WindowParts.Localize("$genesisui_map_marker").ToUpperInvariant();
            for (int i = 0; i < Palette.Length; i++)
            {
                var type = Palette[i];
                var cell = WindowCanvas.At(palette, "Pin " + type, 6f, 32f + i * 66f, 52f, 52f);
                Frame.Dress(cell, _theme, "hotslot", "Windows", 52f);
                var icon = Ui.Image(WindowCanvas.At(cell, "Icon", 10f, 10f, 32f, 32f), SpriteFor(map, type), Color.white);
                icon.preserveAspect = true;
                var mark = Ui.Image(Ui.Place(Ui.Child(cell, "Selected"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f) * (64f / 56f)), _theme.Sprite("hotslot_selected"), Color.white);
                mark.type = Image.Type.Simple;
                mark.enabled = false;
                _parts.Clickable(cell, "map pin type", () => SelectPin(type));
                _palette.Add((type, mark));
            }

            // Bottom right: visible to others, zoom and back to the player.
            var card = WindowCanvas.At(_board, "Controls", WindowCanvas.Design.x - 16f - 300f, 775f - 16f - 112f, 300f, 112f);
            Ui.Image(card, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(card, _theme, "card", "Windows");
            var sw = WindowCanvas.At(card, "Switch", 16f, 14f, 56f, 24f);
            Frame.Dress(sw, _theme, "keycap_wide", "Windows", 24f);
            _publicKnob = WindowCanvas.At(sw, "Knob", 3f, 3f, 18f, 18f);
            Frame.Dress(_publicKnob, _theme, "keycap", "Windows", 18f);
            _publicLabel = _parts.Label(card, "Public", FontRole.Body, 15f, t.TextTitle, 82f, 10f, 206f, 32f, TextAlignmentOptions.MidlineLeft);
            _publicLabel.text = WindowParts.Localize("$genesisui_map_public");
            _parts.Clickable(sw, "map public", () => { if (map.m_publicPosition != null) map.m_publicPosition.isOn = !map.m_publicPosition.isOn; });
            _parts.Button(card, "ZoomOut", 16f, 56f, 44f, 40f, null, 20f, "map zoom out", () => Zoom(map, 1.5f), out var minus);
            minus.text = "−";
            _parts.Button(card, "ZoomIn", 68f, 56f, 44f, 40f, null, 20f, "map zoom in", () => Zoom(map, 1f / 1.5f), out var plus);
            plus.text = "+";
            _parts.Button(card, "Center", 122f, 56f, 162f, 40f, "$genesisui_map_center", 15f, "map center", () => _offset(map) = Vector3.zero, out _);
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

        private static void SelectPin(global::Minimap.PinType type)
        {
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
