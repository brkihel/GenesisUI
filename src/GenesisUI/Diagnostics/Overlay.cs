#if GENESIS_DIAGNOSTICS
using System;
using System.Collections.Generic;
using System.Text;
using GenesisUI.Foundation;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Diagnostics
{
    /// <summary>
    /// The debug bed (docs/DIAGNOSTICS.md §3), in every non-Release build. F8 opens it.
    /// Shows modules, regions, veils and faults; lets the tester inject a fault, retry a
    /// module, look at vanilla under GenesisUI and write the report. It holds an input
    /// lease while open, so the mouse is free and the character does not move.
    /// </summary>
    internal static class Overlay
    {
        private const string Owner = "diag:overlay";
        private const float RefreshSeconds = 0.5f;

        private static ThemeRuntime _theme;
        private static Func<string> _writeReport;
        private static RectTransform _panel;
        private static TextMeshProUGUI _body;
        private static TextMeshProUGUI _veilLabel;
        private static readonly List<(ModuleEntry entry, Button inject, Button retry)> Rows = new List<(ModuleEntry, Button, Button)>();
        private static IDisposable _lease;
        private static float _sinceRefresh;
        private static string _status = "";

        public static bool IsOpen => _panel != null && _panel.gameObject.activeSelf;

        public static void Init(ThemeRuntime theme, Func<string> writeReport)
        {
            _theme = theme;
            _writeReport = writeReport;
        }

        public static void Toggle()
        {
            if (IsOpen) { Close(); return; }
            if (_panel == null) BuildPanel();
            if (_panel == null) return;
            _panel.gameObject.SetActive(true);
            _lease = InputLeases.Acquire(Owner);
            _sinceRefresh = RefreshSeconds;
        }

        public static void Close()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
            _lease?.Dispose();
            _lease = null;
        }

        /// <summary>The GUI root is recreated with each scene; the panel goes with it.</summary>
        public static void OnGuiAvailable()
        {
            _lease?.Dispose();
            _lease = null;
            _panel = null;
            Rows.Clear();
        }

        public static void Tick(float dt)
        {
            if (!IsOpen) return;
            _sinceRefresh += dt;
            if (_sinceRefresh < RefreshSeconds) return;
            _sinceRefresh = 0f;
            Refresh();
        }

        private static void BuildPanel()
        {
            var root = GUIManager.CustomGUIFront;
            if (root == null || _theme == null) return;
            var t = _theme.Tokens;

            _panel = Ui.Place(Ui.Child(root.transform, "GenesisUI_Diagnostics"), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(560f, 620f));
            var bg = Ui.Image(_panel, null, ThemeRuntime.ToUnity(t.PanelBackground), raycast: true);
            var outline = _panel.gameObject.AddComponent<Outline>();
            outline.effectColor = ThemeRuntime.ToUnity(t.LineFrame);
            outline.effectDistance = new Vector2(2f, -2f);

            var title = Ui.Text(_panel, "Title", _theme, FontRole.Display, 22f, ThemeRuntime.ToUnity(t.AccentGoldBright), TextAlignmentOptions.TopLeft);
            Ui.Place((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(520f, 30f));
            title.text = L("$genesisui_diag_title") + "  <size=14><color=#BA995C>" + GenesisUI.Build.FullVersion + "</color></size>";

            float y = -56f;
            foreach (var entry in ModuleHost.Modules)
            {
                var e = entry;
                var label = Ui.Text(_panel, "Row " + e.Module.Id, _theme, FontRole.BodyStrong, 18f, ThemeRuntime.ToUnity(t.TextBody), TextAlignmentOptions.Left);
                Ui.Place((RectTransform)label.transform, new Vector2(0f, 1f), new Vector2(18f, y), new Vector2(280f, 28f));
                label.text = L(e.Module.NameToken) + "  <size=13><color=#BA995C>" + e.Module.Id + "</color></size>";
                var inject = Button(_panel, L("$genesisui_diag_inject"), new Vector2(300f, y), 118f, () => ModuleHost.InjectFault(e));
                var retry = Button(_panel, L("$genesisui_diag_retry"), new Vector2(424f, y), 118f, () => ModuleHost.Retry(e));
                Rows.Add((e, inject, retry));
                y -= 34f;
            }

            y -= 8f;
            var veil = Button(_panel, "", new Vector2(18f, y), 200f, ToggleVeil);
            _veilLabel = veil.GetComponentInChildren<TextMeshProUGUI>();
            Button(_panel, L("$genesisui_diag_report"), new Vector2(226f, y), 160f, () =>
            {
                string path = _writeReport();
                _status = path != null ? L("$genesisui_diag_report_done") : L("$genesisui_report_failed");
                Refresh();
            });
            Button(_panel, L("$genesisui_diag_close"), new Vector2(394f, y), 148f, Close);
            y -= 44f;

            _body = Ui.Text(_panel, "Body", _theme, FontRole.Body, 16f, ThemeRuntime.ToUnity(t.TextBody), TextAlignmentOptions.TopLeft);
            Ui.Place((RectTransform)_body.transform, new Vector2(0f, 1f), new Vector2(18f, y), new Vector2(524f, 620f + y - 16f));
            _body.textWrappingMode = TextWrappingModes.Normal;
            _body.overflowMode = TextOverflowModes.Truncate;

            _panel.gameObject.SetActive(false);
        }

        private static void Refresh()
        {
            if (_body == null) return;
            foreach (var (entry, inject, retry) in Rows)
            {
                inject.interactable = entry.State == ModuleState.Active;
                retry.interactable = entry.State == ModuleState.Faulted;
            }
            _veilLabel.text = L(VanillaVeil.Lifted ? "$genesisui_diag_hide_vanilla" : "$genesisui_diag_show_vanilla");

            var sb = new StringBuilder(1024);
            sb.Append("<color=#F7E283>").Append(L("$genesisui_diag_modules")).Append("</color>\n");
            foreach (var e in ModuleHost.Modules)
            {
                double avgMs = e.RefreshCount > 0 ? e.RefreshSecondsTotal / e.RefreshCount * 1000.0 : 0.0;
                sb.Append("• ").Append(e.Module.Id).Append(": <b>").Append(e.State).Append("</b>");
                if (e.RefreshCount > 0) sb.Append("  ").Append(avgMs.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)).Append(" ms/refresh");
                if (e.Reason != null) sb.Append("\n   <color=#E0643C>").Append(e.Reason).Append("</color>");
                sb.Append('\n');
            }

            sb.Append("\n<color=#F7E283>").Append(L("$genesisui_diag_regions")).Append("</color>\n");
            foreach (var r in RegionRegistry.Snapshot()) sb.Append("• ").Append(r.Key).Append(" → ").Append(r.Value).Append('\n');

            sb.Append("\n<color=#F7E283>").Append(L("$genesisui_diag_veils")).Append("</color>\n");
            if (VanillaVeil.Handles.Count == 0) sb.Append("—\n");
            foreach (var h in VanillaVeil.Handles)
                sb.Append("• ").Append(h.Label).Append(h.AddedGroup ? " (own group)" : " (vanilla group)")
                  .Append(h.Fought > 0 ? "  fought " + h.Fought + "x" : "").Append('\n');

            var faults = Guard.Faults.Snapshot();
            sb.Append("\n<color=#F7E283>").Append(L("$genesisui_diag_faults")).Append("</color> ").Append(faults.Count).Append('\n');
            foreach (var f in faults) sb.Append("• ").Append(f.Owner).Append(" x").Append(f.Count).Append(": ").Append(f.FirstMessage).Append('\n');

            sb.Append("\n").Append(L("$genesisui_diag_input")).Append(' ').Append(InputLeases.ActiveCount);
            if (_status.Length > 0) sb.Append("\n\n<color=#A2DC88>").Append(_status).Append("</color>");
            _body.text = sb.ToString();
        }

        private static void ToggleVeil()
        {
            VanillaVeil.SetLifted(!VanillaVeil.Lifted);
            Refresh();
        }

        private static Button Button(RectTransform parent, string text, Vector2 position, float width, Action onClick)
        {
            var t = _theme.Tokens;
            var rt = Ui.Place(Ui.Child(parent, "Button " + text), new Vector2(0f, 1f), position, new Vector2(width, 30f));
            var img = Ui.Image(rt, null, new Color(0.10f, 0.11f, 0.10f, 0.95f), raycast: true);
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = ThemeRuntime.ToUnity(t.AccentGold) * new Color(1f, 1f, 1f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.4f, 1.3f, 1.0f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;
            button.onClick.AddListener(() => Guard.Try("overlay button " + text, onClick));

            var label = Ui.Text(rt, "Label", _theme, FontRole.Label, 14f, ThemeRuntime.ToUnity(t.AccentGoldBright), TextAlignmentOptions.Center);
            Ui.Fill((RectTransform)label.transform);
            label.text = text;
            return button;
        }

        private static string L(string token) => Localization.instance != null ? Localization.instance.Localize(token) : token;
    }
}
#endif
