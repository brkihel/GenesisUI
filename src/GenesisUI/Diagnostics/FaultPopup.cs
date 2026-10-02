using System;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Faults;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Diagnostics
{
    /// <summary>
    /// Told to the player when a part of GenesisUI fails (Diego, after R-058): an apology, what happened
    /// to that part (rebuilt at once, or left off after repeated failures), the exact error in a box,
    /// Copiar (to the clipboard) and Reportar (disabled until the Discord report system exists).
    /// It sits on Jötunn's front GUI canvas, holds an input lease (cursor free, game input blocked) while
    /// open, and shows the latest fault with a count when several arrive.
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Build), typeof(GenesisUI.Foundation.Faults.FaultRecord), typeof(GenesisUI.Widgets.ScrollArea), typeof(GenesisUI.Foundation.InputLeases), typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.WindowParts), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Widgets.WindowCanvas), typeof(GenesisUI.Widgets.Frame), typeof(GenesisUI.Theme.ThemeTokens))]
    internal static class FaultPopup
    {
        private const string Owner = "diagnostics:fault-popup";
        private const float W = 760f, H = 470f;

        private static RectTransform _root;
        private static TextMeshProUGUI _text, _error, _count;
        private static ScrollArea _errorScroll;
        private static IDisposable _lease;
        private static string _detail = "";
        private static int _faults;

        public static void Show(string moduleToken, FaultRecord record, bool restarted)
        {
            if (!EnsureBuilt()) return;
            _faults++;
            string module = Localize(moduleToken);
            string state = Localize(restarted ? "$genesisui_fault_restarted" : "$genesisui_fault_left_off");
            _text.text = Localize("$genesisui_fault_text").Replace("{0}", module) + "\n\n" + state;
            _detail = "GenesisUI " + GenesisUI.Build.FullVersion + "\n" + record.Owner + ": " + record.FirstMessage + "\n\n" + (record.FirstDetail ?? "");
            _error.text = _detail;
            _errorScroll.ContentHeight = _error.GetPreferredValues(_detail, _errorScroll.Content.sizeDelta.x - 8f, 0f).y + 12f;
            _errorScroll.ToTop();
            _count.text = _faults > 1 ? Localize("$genesisui_fault_count").Replace("{0}", _faults.ToString()) : "";
            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
                _lease = InputLeases.Acquire(Owner);
            }
            _root.SetAsLastSibling();
        }

        /// <summary>Host.LateTick: a scene change destroyed the popup while open; never keep input blocked.</summary>
        public static void Tick()
        {
            if (_lease != null && (_root == null || !_root.gameObject.activeInHierarchy)) Close();
        }

        private static void Close()
        {
            _faults = 0;
            if (_root != null) _root.gameObject.SetActive(false);
            _lease?.Dispose();
            _lease = null;
        }
        internal static void Shutdown()
        {
            Close();
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            _root = null; _text = _error = _count = null; _errorScroll = null; _detail = "";
        }

        private static void Copy()
        {
            GUIUtility.systemCopyBuffer = _detail;
            GenesisLog.Info("Diag", "fault details copied to the clipboard");
        }

        private static bool EnsureBuilt()
        {
            if (_root != null) return true;
            if (_lease != null) { _lease.Dispose(); _lease = null; }
            var theme = ThemeRuntime.Current;
            var front = GUIManager.CustomGUIFront;
            if (theme == null || front == null) return false;
            var parts = new WindowParts(theme);
            var t = theme.Tokens;

            _root = Ui.Fill(Ui.Child(front.transform, "GenesisUI.FaultPopup"));
            Ui.Image(_root, null, new Color(0f, 0f, 0f, 0.55f), raycast: true);
            var board = WindowCanvas.Area(_root, "Board");
            var card = WindowCanvas.At(board, "Card", (WindowCanvas.Design.x - W) / 2f, (WindowCanvas.Design.y - H) / 2f, W, H);
            Frame.Dress(card, theme, "window_panel", "Windows");
            var title = parts.Label(card, "Title", FontRole.Display, 23f, t.AccentGoldBright, 50f, 16f, W - 100f, 34f, TextAlignmentOptions.Center);
            title.text = Localize("$genesisui_fault_title").ToUpperInvariant();
            title.characterSpacing = 5f;
            parts.Rule(card, W * 0.2f, 58f, W * 0.6f);
            _text = parts.Label(card, "Text", FontRole.Body, 18f, t.TextBody, 44f, 72f, W - 88f, 110f, TextAlignmentOptions.TopLeft);
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.enableAutoSizing = true;
            _text.fontSizeMin = 13f;

            var box = WindowCanvas.At(card, "ErrorBox", 40f, 190f, W - 80f, 180f);
            Ui.Image(box, null, new Color(0f, 0f, 0f, 0.55f));
            _errorScroll = new ScrollArea(box, "Error", 10f, 8f, W - 100f, 164f, t, 40f);
            _error = parts.Label(_errorScroll.Content, "Text", FontRole.Body, 13f, t.TextFlavor, 2f, 0f, W - 120f, 164f, TextAlignmentOptions.TopLeft);
            _error.textWrappingMode = TextWrappingModes.Normal;
            _error.enableAutoSizing = false;
            var ert = (RectTransform)_error.transform;
            ert.anchorMin = Vector2.zero;
            ert.anchorMax = Vector2.one;
            ert.offsetMin = new Vector2(2f, 0f);
            ert.offsetMax = new Vector2(-2f, 0f);

            _count = parts.Label(card, "Count", FontRole.Body, 14f, t.TextFlavor, 44f, 376f, W - 88f, 20f, TextAlignmentOptions.Left);
            parts.Button(card, "Copy", 40f, H - 70f, 200f, 44f, "$genesisui_fault_copy", 17f, "fault copy", Copy, out _);
            var report = parts.Button(card, "Report", 250f, H - 70f, 260f, 44f, "$genesisui_fault_report", 17f, "fault report", () => { }, out _);
            report.interactable = false; // the Discord report system comes later
            parts.Button(card, "Close", W - 40f - 200f, H - 70f, 200f, 44f, "$genesisui_hint_close", 17f, "fault close", Close, out var close);
            close.font = theme.Font(FontRole.Display);
            _root.gameObject.SetActive(false);
            return true;
        }

        private static string Localize(string text) => WindowParts.Localize(text);
    }
}
