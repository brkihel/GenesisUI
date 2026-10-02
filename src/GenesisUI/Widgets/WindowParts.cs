using System;
using GenesisUI.Foundation;
using GenesisUI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// The pieces every GenesisUI window is assembled from (D-032/D-033), in design-board units with
    /// y growing downwards like the concept: panels (thin metal frame, title on its rule), labels,
    /// buttons and chips, the rule knot. One place, so the inventory, crafting and later windows
    /// share one finish.
    /// </summary>
    [GenesisUI.Foundation.Contracts.GameContract("assembly_guiutils", "Localization", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Localization")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_guiutils", "Localization", "Localize", Parameters = new string[] { "System.String" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Widgets.WindowCanvas), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Widgets.Frame), typeof(GenesisUI.Widgets.OneShotLight), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Widgets.RuneTitle), typeof(GenesisUI.Foundation.Guard), typeof(GenesisUI.Widgets.PressFeedback), typeof(GenesisUI.Foundation.GuardedBehaviour))]
    internal sealed class WindowParts
    {
        private readonly ThemeRuntime _theme;

        public WindowParts(ThemeRuntime theme)
        {
            _theme = theme;
        }

        public ThemeRuntime Theme => _theme;

        /// <summary>
        /// A panel: the frame, a raycast blocker (a click on its empty parts must not reach vanilla's
        /// drop-outside button behind the window), the title and the tab marker as its rule.
        /// </summary>
        public RectTransform Panel(RectTransform parent, string name, float x, float y, float width, float height,
                                   string titleToken, float titleX, float titleSize, TextAlignmentOptions align)
        {
            var rt = WindowCanvas.At(parent, name, x, y, width, height);
            Ui.Image(rt, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var frame = Frame.Dress(rt, _theme, "window_panel", "Windows");
            // When its window opens, light runs once along the frame's lines.
            OneShotLight.Reveal(rt, frame, _theme);
            if (titleToken == null) return rt;
            bool centred = align == TextAlignmentOptions.Center;
            var title = Label(rt, "Title", FontRole.Display, titleSize, _theme.Tokens.AccentGoldBright,
                centred ? 60f : titleX, 12f, centred ? width - 120f : width - titleX - 70f, 36f, align);
            title.characterSpacing = centred ? 5f : 8f;
            title.text = Localize(titleToken).ToUpperInvariant();
            RuneTitle.Attach(title, _theme); // written in runes first, then in letters (D-036)
            float mw = centred ? width * 0.6f : Mathf.Min(300f, width * 0.5f);
            Rule(rt, centred ? (width - mw) / 2f : titleX - 6f, 48f, mw);
            return rt;
        }

        /// <summary>The fine rule with its knot, never stretched.</summary>
        public RectTransform Rule(RectTransform parent, float x, float y, float width)
        {
            var marker = _theme.Sprite("tab_marker");
            var rt = WindowCanvas.At(parent, "Rule", x, y - 4f, width, 8f);
            if (marker == null) return rt;
            var img = Ui.Image(rt, marker, Color.white);
            img.pixelsPerUnitMultiplier = _theme.Size("tab_marker").y / 8f * Frame.CanvasScale(rt);
            Knot(rt, 12f);
            return rt;
        }

        public void Knot(RectTransform parent, float height)
        {
            var knot = _theme.Sprite("tab_knot");
            if (knot == null) return;
            var d = _theme.Size("tab_knot");
            var krt = Ui.Place(Ui.Child(parent, "Knot"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(d.x * height / Mathf.Max(1f, d.y), height));
            Ui.Image(krt, knot, Color.white);
        }

        /// <summary>A text placed by its top-left corner in design units.</summary>
        public TextMeshProUGUI Label(RectTransform parent, string name, FontRole role, float size, ColorRgba color,
                                     float x, float y, float width, float height, TextAlignmentOptions alignment)
        {
            var text = Ui.Fit(Ui.Text(parent, name, _theme, role, size, ThemeRuntime.ToUnity(color), alignment, outlined: true), Mathf.Min(10f, size));
            text.text = ""; // never null (R-054)
            var rt = (RectTransform)text.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
            return text;
        }

        /// <summary>A key-cap framed button with a label; returns the button (its interactable state dims it).</summary>
        public Button Button(RectTransform parent, string name, float x, float y, float width, float height, string token,
                             float textSize, string what, Action onClick, out TextMeshProUGUI label)
        {
            var rt = WindowCanvas.At(parent, name, x, y, width, height);
            Frame.Dress(rt, _theme, "keycap_wide", "Windows", height);
            label = Label(rt, "Text", FontRole.Body, textSize, _theme.Tokens.TextTitle, 4f, 0f, width - 8f, height, TextAlignmentOptions.Center);
            if (token != null) label.text = Localize(token);
            return Clickable(rt, what, onClick);
        }

        public Button Clickable(RectTransform rt, string what, Action onClick)
        {
            var hit = Ui.Image(Ui.Fill(Ui.Child(rt, "Hit")), null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            string owner = Guard.CurrentOwner ?? "ui:" + what;
            button.onClick.AddListener(() => Guard.Run(owner, onClick));
            // Reuse the piece's own group (a crafting row fades with it): a second one cannot be added.
            var group = rt.gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = rt.gameObject.AddComponent<CanvasGroup>();
            // Unity's Button has no dimmed look without a transition: follow interactable ourselves.
            rt.gameObject.AddComponent<DimWhenDisabled>().Init(button, group);
            PressFeedback.Attach(rt, button, _theme);
            return button;
        }

        public static string Localize(string text) => Localization.instance != null ? Localization.instance.Localize(text) : text;

        /// <summary>Fades a button's whole piece while it is not interactable.</summary>
        private sealed class DimWhenDisabled : GenesisUI.Foundation.GuardedBehaviour
        {
            private Button _button;
            private CanvasGroup _group;
            private bool _shown = true;

            public void Init(Button button, CanvasGroup group)
            {
                _button = button;
                _group = group;
            }

            protected override void OnOwnerLateUpdate()
            {
                if (_button == null) return;
                bool on = _button.interactable;
                if (on == _shown) return;
                _shown = on;
                _group.alpha = on ? 1f : 0.4f;
            }
        }
    }
}
