using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Windows
{
    /// <summary>
    /// The window shell around vanilla's inventory (F4.1, ConceptArt 9 and 12): the top bar with
    /// the GenesisUI title and six tabs (Q/E), and the key-hint bar at the bottom. It appears while
    /// vanilla's inventory is open and only calls vanilla's own entry points (open skills,
    /// achievements, the large map); vanilla's windows stay as they are until F4.2.
    /// </summary>
    [GameContract("assembly_valheim", "InventoryGui", "IsVisible")]
    [GameContract("assembly_valheim", "InventoryGui", "instance")]
    [GameContract("assembly_valheim", "InventoryGui", "Hide")]
    [GameContract("assembly_valheim", "InventoryGui", "OnOpenSkills")]
    [GameContract("assembly_valheim", "InventoryGui", "OnOpenAchievements")]
    [GameContract("assembly_valheim", "InventoryGui", "OnCloseAchievements")]
    [GameContract("assembly_valheim", "InventoryGui", "OnCloseTrophies")]
    [GameContract("assembly_valheim", "InventoryGui", "m_skillsDialog")]
    [GameContract("assembly_valheim", "InventoryGui", "m_textsDialog")]
    [GameContract("assembly_valheim", "InventoryGui", "m_trophiesPanel")]
    [GameContract("assembly_valheim", "InventoryGui", "m_achievementsPanel")]
    [GameContract("assembly_valheim", "SkillsDialog", "OnClose")]
    [GameContract("assembly_valheim", "Minimap", "SetMapMode")]
    [GameContract("assembly_valheim", "Minimap", "IsOpen")]
    [GameContract("assembly_valheim", "InventoryGui", "Show")]
    [GameContract("assembly_valheim", "KeyHints", "instance")]
    internal sealed class WindowShellModule : IUiModule
    {
        // ConceptArt (9) in design-board units (WindowCanvas.Design): tab bar 90, hint bar 62.
        private const float BarHeight = 90f;
        private const float HintHeight = 62f;
        private const float CapSize = 30f;
        private const float FadeSpeed = 8f;

        private static readonly string[] NoRegions = new string[0];

        internal enum Tab { Inventory, Skills, Map, Crafting, Achievements, Settings }

        private static readonly (Tab Tab, string Icon, string Token)[] Tabs =
        {
            (Tab.Inventory, "icon_inventory", "$genesisui_tab_inventory"),
            (Tab.Skills, "icon_skills", "$genesisui_tab_skills"),
            (Tab.Map, "icon_map", "$genesisui_tab_map"),
            (Tab.Crafting, "icon_crafting", "$genesisui_tab_crafting"),
            (Tab.Achievements, "icon_achievements", "$genesisui_tab_achievements"),
            (Tab.Settings, "icon_settings", "$genesisui_tab_settings"),
        };

        private readonly ConfigEntry<KeyboardShortcut> _previousKey;
        private readonly ConfigEntry<KeyboardShortcut> _nextKey;
        private readonly TabView[] _tabs = new TabView[Tabs.Length];

        private ThemeRuntime _theme;
        private RectTransform _root;
        private CanvasGroup _fade;
        private RectTransform _settingsPage;
        private RectTransform _area;
        private Tab _active = Tab.Inventory;
        private bool _wasVisible;
        private bool _mapFromTab;
        private float _hudAlpha = 1f;
        private CanvasGroup _keyHints;
        private bool _keyHintsOwn;

        private sealed class TabView
        {
            public TextMeshProUGUI Label;
            public Image Icon;
            public Image Marker;
        }

        public WindowShellModule(ConfigFile config)
        {
            _previousKey = config.Bind("Windows", "PreviousTabKey", new KeyboardShortcut(KeyCode.Q),
                "Tecla da aba anterior nas janelas (inventário, criação...).");
            _nextKey = config.Bind("Windows", "NextTabKey", new KeyboardShortcut(KeyCode.E),
                "Tecla da próxima aba nas janelas. Com a janela aberta, ela troca de aba em vez de fechar a janela.");
        }

        public string Id => "win.shell";
        public string NameToken => "$genesisui_module_windows";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f; // every frame: tab keys

        /// <summary>Whether the shell is showing a window, and which tab: read by the window modules.</summary>
        internal static bool Showing { get; private set; }
        internal static Tab ActiveTab { get; private set; } = Tab.Inventory;
        /// <summary>The shared window fade, also used by the inventory panel during close.</summary>
        internal static float Opacity { get; private set; }

        /// <summary>The next-tab key, while the shell is showing; read by the Use guard patch.</summary>
        internal static KeyCode ActiveNextKey { get; private set; } = KeyCode.None;

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _wasVisible = false;
            // The bars are built on vanilla's inventory canvas as soon as it exists (EnsureBuilt).
        }

        /// <summary>
        /// On vanilla's own inventory canvas, in front of InventoryGui (R-048: Jötunn's canvas drew the
        /// 9-slice ends at another size, and one shared canvas keeps clicks and scale consistent).
        /// Rebuilt if a scene change destroyed it.
        /// </summary>
        private bool EnsureBuilt(InventoryGui gui)
        {
            if (_root != null) return true;
            if (gui == null) return false;
            _root = WindowCanvas.CreateRoot(gui, "GenesisUI.WindowShell", behind: false);
            if (_root == null) return false;
            _fade = _root.gameObject.AddComponent<CanvasGroup>();
            _fade.alpha = 0f;
            Opacity = 0f;
            _area = WindowCanvas.Area(_root, "Area");
            BuildTopBar();
            BuildHintBar();
            BuildSettingsPage();
            _root.gameObject.SetActive(false);
            return true;
        }

        public void Refresh(float deltaSeconds)
        {
            var gui = InventoryGui.instance;
            if (!EnsureBuilt(gui)) return;
            bool mapOpen = global::Minimap.IsOpen();
            bool visible = gui != null && InventoryGui.IsVisible() && Player.m_localPlayer != null && !mapOpen;

            // The map opened from the Mapa tab is part of the windows: Q/E lead back to the tabs.
            if (_mapFromTab && !mapOpen) _mapFromTab = false;
            if (_mapFromTab && gui != null)
            {
                int step = _previousKey.Value.IsDown() ? -1 : _nextKey.Value.IsDown() ? 1 : 0;
                if (step != 0)
                {
                    _mapFromTab = false;
                    global::Minimap.instance.SetMapMode(global::Minimap.MapMode.Small);
                    gui.Show(null);
                    _active = Tab.Map;
                    Select(Step(step), callVanilla: true);
                    _wasVisible = true;
                    return;
                }
            }

            // While a window is open the HUD (bars, hotbar, vanilla's key hints) fades out, and back in
            // when it closes (Diego, R-046): nothing draws over the windows.
            _hudAlpha = Mathf.MoveTowards(_hudAlpha, visible ? 0f : 1f, deltaSeconds * FadeSpeed);
            ModuleHost.SetHudAlpha(_hudAlpha);
            KeyHintsAlpha(_hudAlpha);
            if (visible && !_wasVisible) Select(Tab.Inventory, callVanilla: false);
            _wasVisible = visible;
            ActiveNextKey = visible ? _nextKey.Value.MainKey : KeyCode.None;
            Showing = visible;

            float target = visible ? 1f : 0f;
            float alpha = Mathf.MoveTowards(_fade.alpha, target, deltaSeconds * FadeSpeed);
            if (!Mathf.Approximately(_fade.alpha, alpha)) _fade.alpha = alpha;
            Opacity = alpha;
            bool show = alpha > 0f;
            if (_root.gameObject.activeSelf != show) _root.gameObject.SetActive(show);
            _fade.blocksRaycasts = visible;
            if (!visible) return;

            if (_previousKey.Value.IsDown()) Select(Step(-1), callVanilla: true);
            else if (_nextKey.Value.IsDown()) Select(Step(+1), callVanilla: true);
            WindowCanvas.Fit(_area);
            FollowVanilla(gui);
        }

        public void Teardown()
        {
            ActiveNextKey = KeyCode.None;
            Showing = false;
            Opacity = 0f;
            ModuleHost.SetHudAlpha(1f);
            _hudAlpha = 1f;
            if (_keyHints != null)
            {
                if (_keyHintsOwn) Object.Destroy(_keyHints);
                else _keyHints.alpha = 1f;
            }
            _keyHints = null;
            _mapFromTab = false;
            if (_root != null) Object.Destroy(_root.gameObject);
            _root = null;
            _area = null;
            _settingsPage = null;
        }

        /// <summary>Vanilla's key hints fade with the HUD; our own group on them, removed on teardown.</summary>
        private void KeyHintsAlpha(float alpha)
        {
            if (_keyHints == null)
            {
                if (alpha >= 1f || KeyHints.instance == null) return;
                // Add a group only if vanilla has none, and remember whether it is ours to remove.
                _keyHints = KeyHints.instance.gameObject.GetComponent<CanvasGroup>();
                _keyHintsOwn = _keyHints == null;
                if (_keyHintsOwn) _keyHints = KeyHints.instance.gameObject.AddComponent<CanvasGroup>();
            }
            if (!Mathf.Approximately(_keyHints.alpha, alpha)) _keyHints.alpha = alpha;
        }

        // ------------------------------------------------------------------ tabs

        private Tab Step(int direction)
        {
            int n = Tabs.Length;
            return (Tab)(((int)_active + direction + n) % n);
        }

        /// <summary>Vanilla's own dialogs may be closed with Esc: the highlighted tab follows them.</summary>
        private void FollowVanilla(InventoryGui gui)
        {
            if (_active == Tab.Skills && !gui.m_skillsDialog.gameObject.activeSelf) Show(Tab.Inventory);
            else if (_active == Tab.Achievements && !gui.m_achievementsPanel.gameObject.activeSelf) Show(Tab.Inventory);
        }

        private void Select(Tab tab, bool callVanilla)
        {
            var gui = InventoryGui.instance;
            if (callVanilla && gui != null)
            {
                CloseVanillaDialogs(gui);
                switch (tab)
                {
                    case Tab.Skills: gui.OnOpenSkills(); break;
                    case Tab.Achievements: gui.OnOpenAchievements(); break;
                    case Tab.Map:
                        // The map is its own screen: leave the inventory and open vanilla's large map.
                        gui.Hide();
                        if (global::Minimap.instance != null)
                        {
                            global::Minimap.instance.SetMapMode(global::Minimap.MapMode.Large);
                            _mapFromTab = true;
                        }
                        tab = Tab.Inventory;
                        break;
                }
            }
            Show(tab);
        }

        private static void CloseVanillaDialogs(InventoryGui gui)
        {
            if (gui.m_skillsDialog.gameObject.activeSelf) gui.m_skillsDialog.OnClose();
            if (gui.m_textsDialog.gameObject.activeSelf) gui.m_textsDialog.gameObject.SetActive(false);
            if (gui.m_trophiesPanel.activeSelf) gui.OnCloseTrophies();
            if (gui.m_achievementsPanel.gameObject.activeSelf) gui.OnCloseAchievements();
        }

        private void Show(Tab tab)
        {
            _active = tab;
            ActiveTab = tab;
            var gold = ThemeRuntime.ToUnity(_theme.Tokens.AccentGoldBright);
            var muted = ThemeRuntime.ToUnity(_theme.Tokens.TextFlavor);
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool on = Tabs[i].Tab == tab;
                var t = _tabs[i];
                if (t.Label.color != (on ? gold : muted)) t.Label.color = on ? gold : muted;
                if (t.Icon != null) t.Icon.color = on ? Color.white : new Color(0.75f, 0.72f, 0.66f, 0.85f);
                if (t.Marker != null && t.Marker.gameObject.activeSelf != on) t.Marker.gameObject.SetActive(on);
            }
            if (_craftingHints != null)
            {
                bool crafting = tab == Tab.Crafting;
                if (_craftingHints.gameObject.activeSelf != crafting) _craftingHints.gameObject.SetActive(crafting);
                if (_inventoryHints.gameObject.activeSelf == crafting) _inventoryHints.gameObject.SetActive(!crafting);
            }
            bool settings = tab == Tab.Settings;
            if (_settingsPage != null && _settingsPage.gameObject.activeSelf != settings) _settingsPage.gameObject.SetActive(settings);
        }

        // ------------------------------------------------------------------ building

        private void BuildTopBar()
        {
            var t = _theme.Tokens;
            var bar = Ui.Child(_area, "TopBar");
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(0f, -BarHeight);
            bar.offsetMax = new Vector2(0f, 0f);
            Ui.Image(bar, null, new Color(0f, 0f, 0f, 0f), raycast: true); // a held item is not dropped by a click on the bar
            Frame.Dress(bar, _theme, "window_topbar", "Windows", BarHeight);

            var drawn = _theme.Size("window_topbar");
            float k = drawn.y > 0f ? BarHeight / drawn.y : 1f;
            var c = _theme.Content("window_topbar", new Vector4(150f, 12f, 150f, 12f)) * k;

            // No title or logo in the bar (Diego, R-046): the previous-tab key, then the tabs.
            KeyCap(bar, new Vector2(0f, 0.5f), new Vector2(c.x + 4f, 0f), KeyName(_previousKey.Value.MainKey));

            // Tabs share the space between the title and the next-tab key, divided by the sheet's dividers.
            var strip = Ui.Child(bar, "Tabs");
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(1f, 1f);
            strip.offsetMin = new Vector2(c.x + 50f, c.y);
            strip.offsetMax = new Vector2(-c.z - 50f, -c.w);
            KeyCap(bar, new Vector2(1f, 0.5f), new Vector2(-c.z + 4f, 0f), KeyName(_nextKey.Value.MainKey));

            for (int i = 0; i < Tabs.Length; i++)
            {
                float x0 = (float)i / Tabs.Length, x1 = (float)(i + 1) / Tabs.Length;
                var cell = Ui.Child(strip, "Tab " + Tabs[i].Tab);
                cell.anchorMin = new Vector2(x0, 0f);
                cell.anchorMax = new Vector2(x1, 1f);
                cell.offsetMin = cell.offsetMax = Vector2.zero;
                _tabs[i] = TabCell(cell, Tabs[i].Icon, Tabs[i].Token, Tabs[i].Tab);

                // Between tabs: the tab marker's own knot, small as a dot (Diego, R-048: the tall divider
                // looked stretched). None before the first tab.
                var knot = _theme.Sprite("tab_knot");
                if (knot != null && i > 0)
                {
                    var d = _theme.Size("tab_knot");
                    float h = 10f;
                    var drt = Ui.Place(Ui.Child(strip, "Knot" + i), new Vector2(x0, 0.5f), Vector2.zero, new Vector2(d.x * h / Mathf.Max(1f, d.y), h));
                    drt.pivot = new Vector2(0.5f, 0.5f);
                    Ui.Image(drt, knot, Color.white);
                }
            }
            Show(Tab.Inventory);
        }

        private TabView TabCell(RectTransform cell, string icon, string token, Tab tab)
        {
            var view = new TabView();
            var hit = Ui.Image(cell, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var button = cell.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Guard.Try("window tab " + tab, () => Select(tab, callVanilla: true)));

            var iconSprite = _theme.Sprite(icon);
            if (iconSprite != null)
            {
                var s = _theme.Size(icon);
                float h = 32f;
                var irt = Ui.Place(Ui.Child(cell, "Icon"), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(s.x * h / Mathf.Max(1f, s.y), h));
                irt.pivot = new Vector2(0.5f, 1f);
                view.Icon = Ui.Image(irt, iconSprite, Color.white);
            }
            view.Label = Ui.Fit(Ui.Text(cell, "Label", _theme, FontRole.Label, 16f, ThemeRuntime.ToUnity(_theme.Tokens.TextFlavor),
                TextAlignmentOptions.Bottom, outlined: true), 10f);
            view.Label.characterSpacing = 6f;
            var lrt = (RectTransform)view.Label.transform;
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0f, 8f);
            lrt.sizeDelta = new Vector2(-8f, 22f);
            view.Label.text = Localize(token).ToUpperInvariant();

            var marker = _theme.Sprite("tab_marker");
            if (marker != null)
            {
                var mrt = Ui.Child(cell, "Marker");
                mrt.anchorMin = new Vector2(0.12f, 0f);
                mrt.anchorMax = new Vector2(0.88f, 0f);
                mrt.pivot = new Vector2(0.5f, 0.5f);
                mrt.anchoredPosition = new Vector2(0f, 2f);
                mrt.sizeDelta = new Vector2(0f, 10f);
                view.Marker = Ui.Image(mrt, marker, Color.white);
                view.Marker.pixelsPerUnitMultiplier = _theme.Size("tab_marker").y / 10f * Frame.CanvasScale(mrt);
                var knot = _theme.Sprite("tab_knot");
                if (knot != null)
                {
                    var d = _theme.Size("tab_knot");
                    Ui.Image(Ui.Place(Ui.Child(mrt, "Knot"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(d.x * 12f / Mathf.Max(1f, d.y), 12f)), knot, Color.white);
                }
                mrt.gameObject.SetActive(false);
            }
            return view;
        }

        private void BuildHintBar()
        {
            var bar = Ui.Child(_area, "HintBar");
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = new Vector2(0f, 0f);
            bar.offsetMax = new Vector2(0f, HintHeight);
            Ui.Image(bar, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Frame.Dress(bar, _theme, "window_hintbar", "Windows", HintHeight);

            var drawn = _theme.Size("window_hintbar");
            float k = drawn.y > 0f ? HintHeight / drawn.y : 1f;
            var c = _theme.Content("window_hintbar", new Vector4(70f, 10f, 70f, 10f)) * k;
            string tabs = KeyName(_previousKey.Value.MainKey) + "/" + KeyName(_nextKey.Value.MainKey);
            // One row per kind of window; the shell shows the active tab's (Show).
            _inventoryHints = HintRow(bar, c, "Inventory", new[]
            {
                ("Esc", (string)null, "$genesisui_hint_close"),
                (null, "icon_mouse_right", "$genesisui_hint_use"),
                (null, "icon_mouse_left", "$genesisui_hint_move"),
                ("Shift", "icon_mouse_left", "$genesisui_hint_split"),
                ("Ctrl", "icon_mouse_left", "$genesisui_hint_transfer"),
                ("R", null, "$genesisui_hint_sort"),
                (tabs, null, "$genesisui_hint_tabs"),
            });
            _craftingHints = HintRow(bar, c, "Crafting", new[]
            {
                ("Esc", (string)null, "$genesisui_hint_close"),
                (null, "icon_mouse_left", "$genesisui_hint_select"),
                ("Shift", "icon_mouse_left", "$genesisui_hint_craft_many"),
                (tabs, null, "$genesisui_hint_tabs"),
            });
            _craftingHints.gameObject.SetActive(false);
        }

        private RectTransform _inventoryHints, _craftingHints;

        /// <summary>
        /// A row of hints laid out by hand, left to right, from measured widths: layout groups resolved a
        /// frame late and piled every hint in the middle on the first open (R-052 print).
        /// </summary>
        private RectTransform HintRow(RectTransform bar, Vector4 c, string name, (string Key, string Mouse, string Token)[] items)
        {
            var row = Ui.Child(bar, "Hints " + name);
            row.anchorMin = Vector2.zero;
            row.anchorMax = Vector2.one;
            row.offsetMin = new Vector2(c.x + 10f, c.y);
            row.offsetMax = new Vector2(-c.z - 10f, -c.w);
            var hints = new List<RectTransform>();
            foreach (var item in items) hints.Add(Hint(row, item.Key, item.Mouse, item.Token));
            float total = 0f;
            foreach (var h in hints) total += h.sizeDelta.x;
            float width = WindowCanvas.Design.x - c.x - c.z - 20f;
            float gap = Mathf.Max(16f, Mathf.Min(90f, (width - total) / hints.Count));
            float x = (width - total - gap * (hints.Count - 1)) / 2f;
            foreach (var h in hints)
            {
                h.anchoredPosition = new Vector2(x, 0f);
                x += h.sizeDelta.x + gap;
            }
            return row;
        }

        /// <summary>One hint: key cap and/or mouse icon, then its label; returns its row, sized to its content.</summary>
        private RectTransform Hint(RectTransform row, string key, string mouse, string token)
        {
            var group = Ui.Child(row, "Hint " + token);
            group.anchorMin = new Vector2(0f, 0f);
            group.anchorMax = new Vector2(0f, 1f);
            group.pivot = new Vector2(0f, 0.5f);
            float x = 0f;
            if (key != null)
            {
                var cap = KeyCap(group, new Vector2(0f, 0.5f), new Vector2(x, 0f), key);
                x += cap.sizeDelta.x + 6f;
            }
            if (key != null && mouse != null)
            {
                var plus = Ui.Text(group, "Plus", _theme, FontRole.Body, 18f, ThemeRuntime.ToUnity(_theme.Tokens.TextFlavor), TextAlignmentOptions.Center);
                Ui.Place((RectTransform)plus.transform, new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(12f, 26f));
                plus.text = "+";
                x += 18f;
            }
            if (mouse != null && _theme.Sprite(mouse) != null)
            {
                var s = _theme.Size(mouse);
                var size = new Vector2(s.x * 28f / Mathf.Max(1f, s.y), 28f);
                Ui.Image(Ui.Place(Ui.Child(group, "Mouse"), new Vector2(0f, 0.5f), new Vector2(x, 0f), size), _theme.Sprite(mouse), Color.white);
                x += size.x + 8f;
            }
            var label = Ui.Text(group, "Label", _theme, FontRole.Body, 19f, ThemeRuntime.ToUnity(_theme.Tokens.TextTitle), TextAlignmentOptions.MidlineLeft, outlined: true);
            label.text = Localize(token);
            float w = label.GetPreferredValues(label.text).x + 4f;
            Ui.Place((RectTransform)label.transform, new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(w, 30f));
            group.sizeDelta = new Vector2(x + w, 0f);
            return group;
        }

        /// <summary>A blank key cap from the sheet with the key's name written on it (rebinding keeps working).</summary>
        private RectTransform KeyCap(RectTransform parent, Vector2 anchor, Vector2 position, string key)
        {
            bool wide = key.Length > 2;
            var rt = Ui.Child(parent, "Key " + key);
            // The cap first, the text after it: in uGUI later siblings draw on top.
            var cap = Ui.Fill(Ui.Child(rt, "Cap"));
            Frame.Dress(cap, _theme, wide ? "keycap_wide" : "keycap", "Windows", CapSize);
            var label = Ui.Text(rt, "Text", _theme, FontRole.Label, 15f, ThemeRuntime.ToUnity(_theme.Tokens.TextTitle), TextAlignmentOptions.Center);
            label.text = key;
            Ui.Fill((RectTransform)label.transform);
            float w = wide ? Mathf.Max(46f, label.GetPreferredValues(key).x + 20f) : CapSize;
            Ui.Place(rt, anchor, position, new Vector2(w, CapSize));
            rt.pivot = new Vector2(anchor.x, 0.5f);
            return rt;
        }

        private void BuildSettingsPage()
        {
            // GenesisUI's own settings live here from F4.4; until then the tab says so.
            _settingsPage = Ui.Place(Ui.Child(_area, "Settings"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 180f));
            Frame.Dress(_settingsPage, _theme, "card", "Windows");
            var text = Ui.Text(_settingsPage, "Soon", _theme, FontRole.Body, 20f, ThemeRuntime.ToUnity(_theme.Tokens.TextTitle), TextAlignmentOptions.Center, outlined: true);
            Ui.Fill((RectTransform)text.transform, 30f, 20f, 30f, 20f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.text = Localize("$genesisui_settings_soon");
            _settingsPage.gameObject.SetActive(false);
        }

        private static string KeyName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.None: return "—";
                case KeyCode.Escape: return "Esc";
                case KeyCode.LeftShift: case KeyCode.RightShift: return "Shift";
                case KeyCode.LeftControl: case KeyCode.RightControl: return "Ctrl";
                case KeyCode.Tab: return "Tab";
                default:
                    string s = key.ToString();
                    return s.StartsWith("Alpha") ? s.Substring(5) : s;
            }
        }

        private static string Localize(string token) => Localization.instance != null ? Localization.instance.Localize(token) : token;
    }
}
