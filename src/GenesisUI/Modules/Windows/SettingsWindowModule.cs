using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using GenesisUI.Foundation;
using GenesisUI.Gameplay;
using GenesisUI.Patches;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GenesisUI.Modules.Windows
{
    /// <summary>
    /// The Configurações tab, ConceptArt (2): GenesisUI's own settings (the BepInEx config file), by
    /// category on the left, the options of the category in the middle (switches, sliders, keys) and,
    /// on the right, what the option under the cursor does, its default and a button to restore it.
    /// Changes apply at once and are saved by BepInEx; inventory options synced from a locking server
    /// are shown read-only.
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.WindowParts), typeof(GenesisUI.Widgets.WindowCanvas), typeof(GenesisUI.Widgets.Frame), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Widgets.ColorExtensions), typeof(GenesisUI.Widgets.ScrollArea), typeof(GenesisUI.Gameplay.InventorySettings), typeof(GenesisUI.Widgets.KeyText), typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.Patches.TextInputFocus), typeof(GenesisUI.Foundation.InputLeases), typeof(GenesisUI.Foundation.Guard))]
    internal sealed class SettingsWindowModule : WindowModuleBase
    {
        private const float NavW = 336f, CenterX = 340f, CenterW = 664f, InfoX = 1008f, InfoW = 572f;
        private const float RowH = 44f;

        private enum Category { General, Hud, Windows, Sound, Inventory, Modules }

        private static readonly (Category Cat, string Token)[] Categories =
        {
            (Category.General, "$genesisui_cfgcat_general"),
            (Category.Hud, "$genesisui_cfgcat_hud"),
            (Category.Windows, "$genesisui_cfgcat_windows"),
            (Category.Sound, "$genesisui_cfgcat_sound"),
            (Category.Inventory, "$genesisui_cfgcat_inventory"),
            (Category.Modules, "$genesisui_cfgcat_modules"),
        };

        /// <summary>Sections in the order they show, and the category each belongs to.</summary>
        private static readonly (string Section, Category Cat)[] Sections =
        {
            ("General", Category.General), ("Theme", Category.General), ("Backgrounds", Category.General), ("Diagnostics", Category.General),
            ("Vitals", Category.Hud), ("Food", Category.Hud), ("Hotbar", Category.Hud), ("Sprint", Category.Hud), ("Minimap", Category.Hud),
            ("Status", Category.Hud), ("KeyHints", Category.Hud), ("Notice", Category.Hud), ("Hover", Category.Hud), ("Boss", Category.Hud), ("Enemy", Category.Hud),
            ("Windows", Category.Windows), ("Sound", Category.Sound), ("Inventory", Category.Inventory), ("Hotkeys", Category.Inventory), ("Modules", Category.Modules),
        };

        private static readonly HashSet<string> Hidden = new HashSet<string> { "Windows/ViewportDefaultsVersion" };

        private sealed class Row
        {
            public ConfigEntryBase Entry;
            public string Label;
            public TextMeshProUGUI Value;
            public Slider Slider;
            public RectTransform Knob;
            public Image Track;
            public TextMeshProUGUI Key;
            public bool ReadOnly;
            public string Shown;
            public object Last;
        }

        private readonly ConfigFile _config;
        private readonly List<Row> _rows = new List<Row>(64);
        private readonly List<GameObject> _built = new List<GameObject>(128);
        private readonly List<TextMeshProUGUI> _navLabels = new List<TextMeshProUGUI>();
        private readonly List<Image> _navMarks = new List<Image>();
        private static KeyCode[] _keys;

        private Category _category = Category.General;
        private ScrollArea _scroll;
        private TextMeshProUGUI _title, _subtitle, _infoName, _infoText, _infoDefault, _infoLocked;
        private GameObject _infoBody, _infoEmpty;
        private Button _restoreOne;
        private Row _info, _capturing;
        private IDisposable _captureLease;
        private float _syncIn;

        public SettingsWindowModule(ConfigFile config)
        {
            _config = config;
        }

        protected override WindowShellModule.Tab Tab => WindowShellModule.Tab.Settings;
        public override string Id => "win.settings";
        public override string NameToken => "$genesisui_module_settings_window";

        protected override void Forget()
        {
            _rows.Clear();
            _built.Clear();
            _navLabels.Clear();
            _navMarks.Clear();
            _info = null;
            EndCapture();
        }

        protected override void Opened(InventoryGui gui, Player player) => ShowCategory(_category);

        protected override void Closed() => EndCapture();

        protected override void Tick(InventoryGui gui, Player player, float deltaSeconds)
        {
            if (_capturing != null) Capture();
            // Values may change from the config file or the server; four checks a second are plenty.
            _syncIn -= deltaSeconds;
            if (_syncIn > 0f) return;
            _syncIn = 0.25f;
            foreach (var row in _rows) Sync(row);
        }

        // ------------------------------------------------------------------ building

        protected override void Draw()
        {
            var t = Theme.Tokens;
            var nav = Parts.Panel(Panels, "Nav", 0f, 0f, NavW, PanelsHeight, "$genesisui_panel_settings", 54f, 22f, TextAlignmentOptions.Left);
            var center = Parts.Panel(Panels, "Options", CenterX, 0f, CenterW, PanelsHeight, null, 0f, 0f, TextAlignmentOptions.Left);
            var info = Parts.Panel(Panels, "Info", InfoX, 0f, InfoW, PanelsHeight, "$genesisui_panel_setting_info", 54f, 20f, TextAlignmentOptions.Left);

            for (int i = 0; i < Categories.Length; i++)
            {
                var cat = Categories[i].Cat;
                var rt = WindowCanvas.At(nav, "Nav " + cat, 22f, 70f + i * 58f, NavW - 44f, 50f);
                Frame.Dress(rt, Theme, "keycap_wide", "Windows", 50f);
                var mark = Ui.Image(Ui.Fill(Ui.Child(rt, "Selected"), 3f, 3f, 3f, 3f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.14f));
                var label = Parts.Label(rt, "Label", FontRole.Body, 19f, t.TextTitle, 20f, 0f, NavW - 84f, 50f, TextAlignmentOptions.MidlineLeft);
                label.text = Localize(Categories[i].Token);
                Parts.Clickable(rt, "settings category", () => ShowCategory(cat));
                _navLabels.Add(label);
                _navMarks.Add(mark);
            }
            var tip = Parts.Label(nav, "Tip", FontRole.Body, 15f, t.TextFlavor, 24f, 70f + Categories.Length * 58f + 22f, NavW - 48f, 230f, TextAlignmentOptions.TopLeft);
            tip.textWrappingMode = TextWrappingModes.Normal;
            tip.enableAutoSizing = false;
            tip.text = Localize("$genesisui_settings_tip");

            _title = Parts.Label(center, "Title", FontRole.Display, 24f, t.AccentGoldBright, 40f, 12f, CenterW - 80f, 36f, TextAlignmentOptions.Left);
            _title.characterSpacing = 6f;
            Parts.Rule(center, 34f, 50f, 300f);
            _subtitle = Parts.Label(center, "Subtitle", FontRole.Body, 16f, t.TextFlavor, 40f, 58f, CenterW - 80f, 22f, TextAlignmentOptions.Left);
            _scroll = new ScrollArea(center, "List", 24f, 90f, CenterW - 48f, 520f, t, RowH);
            Parts.Button(center, "RestorePage", 24f, 620f, 300f, 40f, "$genesisui_settings_restore_page", 17f, "settings restore page", RestorePage, out _);

            const float pad = 24f, w = InfoW - 2f * pad;
            _infoEmpty = Parts.Label(info, "Empty", FontRole.Body, 17f, t.TextFlavor, pad, 260f, w, 80f, TextAlignmentOptions.Center).gameObject;
            var empty = _infoEmpty.GetComponent<TextMeshProUGUI>();
            empty.textWrappingMode = TextWrappingModes.Normal;
            empty.text = Localize("$genesisui_settings_pick");
            _infoBody = WindowCanvas.At(info, "Body", 0f, 0f, InfoW, PanelsHeight).gameObject;
            var body = (RectTransform)_infoBody.transform;
            _infoName = Parts.Label(body, "Name", FontRole.Display, 21f, t.AccentGoldBright, pad, 70f, w, 30f, TextAlignmentOptions.Left);
            _infoText = Parts.Label(body, "Text", FontRole.Body, 17f, t.TextBody, pad, 110f, w, 240f, TextAlignmentOptions.TopLeft);
            _infoText.textWrappingMode = TextWrappingModes.Normal;
            _infoText.enableAutoSizing = false;
            _infoDefault = Parts.Label(body, "Default", FontRole.Body, 16f, t.TextFlavor, pad, 360f, w, 24f, TextAlignmentOptions.Left);
            _infoLocked = Parts.Label(body, "Locked", FontRole.Body, 16f, t.StateDanger, pad, 390f, w, 44f, TextAlignmentOptions.TopLeft);
            _infoLocked.textWrappingMode = TextWrappingModes.Normal;
            _restoreOne = Parts.Button(body, "RestoreOne", pad, 450f, 260f, 40f, "$genesisui_settings_restore_one", 17f, "settings restore one", RestoreOne, out _);
            _infoBody.SetActive(false);
        }

        private void ShowCategory(Category category)
        {
            _category = category;
            EndCapture();
            for (int i = 0; i < Categories.Length; i++)
            {
                bool on = Categories[i].Cat == category;
                _navMarks[i].enabled = on;
                _navLabels[i].color = ThemeRuntime.ToUnity(on ? Theme.Tokens.AccentGoldBright : Theme.Tokens.TextTitle);
            }
            string token = Categories.First(c => c.Cat == category).Token;
            _title.text = Localize(token).ToUpperInvariant();
            _subtitle.text = Localize(token + "_about");

            foreach (var go in _built) UnityEngine.Object.Destroy(go);
            _built.Clear();
            _rows.Clear();
            var content = _scroll.Content;
            float y = 0f;
            foreach (var (section, cat) in Sections)
            {
                if (cat != category) continue;
                var entries = _config.Keys.Where(d => d.Section == section && !Hidden.Contains(d.Section + "/" + d.Key)).ToList();
                if (entries.Count == 0) continue;
                var header = Parts.Label(content, "Section " + section, FontRole.Label, 14f, Theme.Tokens.AccentGoldBright, 4f, y + 8f, 500f, 20f, TextAlignmentOptions.Left);
                header.text = Localize("$genesisui_cfgsec_" + section).ToUpperInvariant();
                header.characterSpacing = 3f;
                _built.Add(header.gameObject);
                y += 34f;
                foreach (var def in entries)
                {
                    var row = MakeRow(content, _config[def], y);
                    if (row != null) { _rows.Add(row); y += RowH; }
                }
                y += 6f;
            }
            _scroll.ContentHeight = y;
            _scroll.ToTop();
            _info = null;
            _infoBody.SetActive(false);
            _infoEmpty.SetActive(true);
        }

        private Row MakeRow(RectTransform content, ConfigEntryBase entry, float y)
        {
            var t = Theme.Tokens;
            float w = content.sizeDelta.x;
            var rt = WindowCanvas.At(content, "Row " + entry.Definition.Key, 0f, y, w, RowH);
            _built.Add(rt.gameObject);
            Ui.Image(rt, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Ui.Image(WindowCanvas.At(rt, "Line", 0f, RowH - 1f, w, 1f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.12f));
            var row = new Row
            {
                Entry = entry,
                Label = LabelFor(entry.Definition),
                ReadOnly = InventorySettings.IsSynced(entry) && InventorySettings.LockedHere,
            };
            Parts.Label(rt, "Label", FontRole.Body, 17f, t.TextTitle, 8f, 0f, w * 0.52f, RowH, TextAlignmentOptions.MidlineLeft).text = row.Label;
            rt.gameObject.AddComponent<RowHover>().Init(() => ShowInfo(row));

            float cx = w * 0.55f, cw = w * 0.45f - 8f;
            var type = entry.SettingType;
            if (type == typeof(bool))
            {
                // A switch: a small track with a knob that slides to the right when on.
                var track = WindowCanvas.At(rt, "Switch", w - 64f, 10f, 56f, 24f);
                Frame.Dress(track, Theme, "keycap_wide", "Windows", 24f);
                row.Track = Ui.Image(Ui.Fill(Ui.Child(track, "Fill"), 3f, 3f, 3f, 3f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.35f));
                row.Knob = WindowCanvas.At(track, "Knob", 3f, 3f, 18f, 18f);
                Frame.Dress(row.Knob, Theme, "keycap", "Windows", 18f);
                row.Value = Parts.Label(rt, "Value", FontRole.Body, 15f, t.TextFlavor, cx, 0f, cw - 72f, RowH, TextAlignmentOptions.MidlineRight);
                if (!row.ReadOnly) Parts.Clickable(track, "setting switch", () => { entry.BoxedValue = !(bool)entry.BoxedValue; ShowInfo(row); });
            }
            else if ((type == typeof(int) || type == typeof(float)) && entry.Description.AcceptableValues is AcceptableValueBase range && Range(range, out float min, out float max))
            {
                row.Slider = MakeSlider(rt, cx, cw - 76f, min, max, type == typeof(int));
                row.Value = Parts.Label(rt, "Value", FontRole.Body, 16f, t.TextTitle, w - 72f, 0f, 64f, RowH, TextAlignmentOptions.MidlineRight);
                row.Slider.interactable = !row.ReadOnly;
                row.Slider.onValueChanged.AddListener(v => Guard.Run("module:win.settings", () =>
                {
                    object value = type == typeof(int) ? (object)Mathf.RoundToInt(v) : (object)(float)Math.Round(v, 2);
                    if (!Equals(entry.BoxedValue, value)) entry.BoxedValue = value;
                }));
            }
            else if (type == typeof(KeyboardShortcut))
            {
                var btn = Parts.Button(rt, "Key", w - 170f, 6f, 162f, 32f, null, 16f, "setting key", () => BeginCapture(row), out row.Key);
                btn.interactable = !row.ReadOnly;
            }
            else
            {
                row.Value = Parts.Label(rt, "Value", FontRole.Body, 16f, t.TextFlavor, cx, 0f, cw, RowH, TextAlignmentOptions.MidlineRight);
            }
            Sync(row);
            return row;
        }

        private Slider MakeSlider(RectTransform parent, float x, float width, float min, float max, bool whole)
        {
            var t = Theme.Tokens;
            var rt = WindowCanvas.At(parent, "Slider", x, 12f, width, 20f);
            var track = Ui.Image(WindowCanvas.At(rt, "Track", 0f, 8f, width, 4f), null, new Color(0f, 0f, 0f, 0.6f));
            var fillArea = WindowCanvas.At(rt, "FillArea", 0f, 8f, width, 4f);
            var fill = Ui.Fill(Ui.Child(fillArea, "Fill"));
            Ui.Image(fill, Theme.Sprite("bar_fill"), ThemeRuntime.ToUnity(t.AccentGold));
            var handleArea = WindowCanvas.At(rt, "HandleArea", 8f, 0f, width - 16f, 20f);
            var handle = Ui.Child(handleArea, "Handle");
            handle.sizeDelta = new Vector2(16f, 20f);
            Frame.Dress(handle, Theme, "keycap", "Windows", 20f);
            var hit = Ui.Image(rt, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var slider = rt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = hit;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = whole;
            return slider;
        }

        private static bool Range(AcceptableValueBase range, out float min, out float max)
        {
            min = max = 0f;
            var type = range.GetType();
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(AcceptableValueRange<>)) return false;
            min = Convert.ToSingle(type.GetProperty("MinValue").GetValue(range), CultureInfo.InvariantCulture);
            max = Convert.ToSingle(type.GetProperty("MaxValue").GetValue(range), CultureInfo.InvariantCulture);
            return max > min;
        }

        /// <summary>Shows the entry's current value (it may change from the config file or the server).</summary>
        private void Sync(Row row)
        {
            var value = row.Entry.BoxedValue;
            if (row.Shown != null && Equals(value, row.Last)) return;
            row.Last = value;
            string shown = Format(row.Entry, value);
            if (shown == row.Shown) return;
            row.Shown = shown;
            if (row.Value != null) row.Value.text = shown;
            if (row.Knob != null)
            {
                bool on = (bool)value;
                row.Knob.anchoredPosition = new Vector2(on ? 35f : 3f, -3f);
                row.Track.enabled = on;
            }
            if (row.Slider != null)
            {
                float v = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                if (!Mathf.Approximately(row.Slider.value, v)) row.Slider.SetValueWithoutNotify(v);
            }
            if (row.Key != null && row != _capturing) row.Key.text = shown;
            if (row == _info) ShowInfo(row);
        }

        private static string Format(ConfigEntryBase entry, object value)
        {
            if (value is bool b) return Localize(b ? "$genesisui_on" : "$genesisui_off");
            if (value is KeyboardShortcut k) return KeyText.Of(k);
            if (value is float f)
            {
                if (entry.Definition.Section == "Backgrounds" && f < 0f) return Localize("$genesisui_settings_use_default");
                if (entry.Definition.Section == "Backgrounds" || entry.Definition.Section == "Sound" || entry.Definition.Key == "Width" || entry.Definition.Key == "Height")
                    return Mathf.RoundToInt(f * 100f) + "%";
                return f.ToString("0.0#", CultureInfo.CurrentCulture) + (entry.Definition.Key == "Scale" ? "x" : "");
            }
            return Convert.ToString(value, CultureInfo.CurrentCulture);
        }

        private static string LabelFor(ConfigDefinition def)
        {
            string token = "$genesisui_cfg_" + def.Section + "_" + def.Key;
            string label = Localize(token);
            if (!string.IsNullOrEmpty(label) && label != token && !label.StartsWith("[", StringComparison.Ordinal)) return label;
            // Unknown entry (a newer option): its key, spaced ("KeyHintsLift" -> "Key Hints Lift").
            var sb = new System.Text.StringBuilder();
            foreach (char c in def.Key) { if (char.IsUpper(c) && sb.Length > 0) sb.Append(' '); sb.Append(c); }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ info and actions

        private void ShowInfo(Row row)
        {
            _info = row;
            _infoEmpty.SetActive(false);
            _infoBody.SetActive(true);
            _infoName.text = row.Label;
            _infoText.text = row.Entry.Description.Description;
            _infoDefault.text = Localize("$genesisui_settings_default") + " " + Format(row.Entry, row.Entry.DefaultValue);
            _infoLocked.text = row.ReadOnly ? Localize("$genesisui_settings_locked") : "";
            _restoreOne.interactable = !row.ReadOnly && !Equals(row.Entry.BoxedValue, row.Entry.DefaultValue);
        }

        private void RestoreOne()
        {
            if (_info == null || _info.ReadOnly) return;
            _info.Entry.BoxedValue = _info.Entry.DefaultValue;
            ShowInfo(_info);
        }

        private void RestorePage()
        {
            foreach (var row in _rows)
                if (!row.ReadOnly && !Equals(row.Entry.BoxedValue, row.Entry.DefaultValue)) row.Entry.BoxedValue = row.Entry.DefaultValue;
            GenesisLog.Info(Owner, "restored defaults of " + _category);
        }

        private void BeginCapture(Row row)
        {
            EndCapture();
            _capturing = row;
            row.Key.text = Localize("$genesisui_settings_press_key");
            TextInputFocus.Active = true; // Tab/E must not close the window while a key is being chosen
            _captureLease = InputLeases.Acquire(Owner);
        }

        private void EndCapture()
        {
            if (_capturing != null) { _capturing.Shown = null; }
            _capturing = null;
            TextInputFocus.Active = false;
            _captureLease?.Dispose();
            _captureLease = null;
        }

        /// <summary>The next key pressed becomes the shortcut; Esc cancels.</summary>
        private void Capture()
        {
            var input = BepInEx.UnityInput.Current;
            if (_keys == null) _keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
            if (input.GetKeyDown(KeyCode.Escape)) { EndCapture(); return; }
            foreach (var key in _keys)
            {
                if (key == KeyCode.None || key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6 || key >= KeyCode.JoystickButton0) continue;
                // Alt, Ctrl and Shift alone do not end the capture: they are taken with the next key (Alt+Z).
                if (KeyText.IsModifier(key) || !input.GetKeyDown(key)) continue;
                var row = _capturing;
                EndCapture();
                var held = new List<KeyCode>(3);
                if (input.GetKey(KeyCode.LeftControl) || input.GetKey(KeyCode.RightControl)) held.Add(KeyCode.LeftControl);
                if (input.GetKey(KeyCode.LeftAlt) || input.GetKey(KeyCode.RightAlt)) held.Add(KeyCode.LeftAlt);
                if (input.GetKey(KeyCode.LeftShift) || input.GetKey(KeyCode.RightShift)) held.Add(KeyCode.LeftShift);
                var shortcut = new KeyboardShortcut(key, held.ToArray());
                row.Entry.BoxedValue = shortcut;
                GenesisLog.Info(Owner, row.Entry.Definition + " set to " + KeyText.Of(shortcut));
                return;
            }
        }

        /// <summary>Pointer-enter on an option row: the info panel shows it.</summary>
        private sealed class RowHover : MonoBehaviour, IPointerEnterHandler
        {
            private Action _onEnter;
            internal void Init(Action onEnter) => _onEnter = onEnter;
            public void OnPointerEnter(PointerEventData e) => Guard.Run("module:win.settings", _onEnter);
        }
    }
}
