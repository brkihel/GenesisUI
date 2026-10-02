using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;

namespace GenesisUI.Modules.Hover
{
    /// <summary>
    /// The interaction card next to the crosshair (concepts 8, 10): the name of what the player
    /// looks at as a title, and its actions below ("[E] Abrir"). It mirrors vanilla's hover text,
    /// so every object and every mod's hover text shows up without special cases.
    /// </summary>
    [GameContract("assembly_valheim", "Hud", "instance")]
    [GameContract("assembly_valheim", "Hud", "m_hoverName", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "TMPro.TextMeshProUGUI")]
    [GameContract("assembly_valheim", "Minimap", "instance")]
    [GameContract("assembly_valheim", "Minimap", "IsOpen")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Host.ModuleContext), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Widgets.Frame), typeof(GenesisUI.Theme.ThemeTokens))]
    internal sealed class HoverModule : IUiModule
    {
        private const float MaxWidth = 340f;
        private const float Pad = 14f;
        private const float Gap = 3f;
        private Vector2 _minSize;

        private static readonly string[] OwnedRegions = { "hud.hover" };

        private readonly ConfigEntry<int> _offsetX;
        private readonly ConfigEntry<int> _offsetY;
        private RectTransform _card;
        private CanvasGroup _opacity;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
        private Vector4 _content;
        private string _shown;
        private Vector2 _appliedOffset = new Vector2(float.NaN, float.NaN);

        public HoverModule(ConfigFile config)
        {
            _offsetX = config.Bind("Hover", "OffsetX", 60,
                new ConfigDescription("Distância do cartão de interação à direita da mira.", new AcceptableValueRange<int>(-900, 900)));
            _offsetY = config.Bind("Hover", "OffsetY", 30,
                new ConfigDescription("Altura do cartão de interação em relação à mira.", new AcceptableValueRange<int>(-500, 500)));
        }

        public string Id => "hud.hover";
        public string NameToken => "$genesisui_module_hover";
        public IReadOnlyList<string> Regions => OwnedRegions;
        public float RefreshRate => 20f;

        public void Build(ModuleContext context)
        {
            var theme = context.Theme;
            var t = theme.Tokens;
            _card = Ui.Place(Ui.Child(context.Root, "Hover"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 60f));
            _card.pivot = new Vector2(0f, 0.5f);
            _opacity = _card.gameObject.AddComponent<CanvasGroup>();
            _opacity.blocksRaycasts = false;
            var frame = Ui.Fill(Ui.Child(_card, "Card"));
            Frame.Dress(frame, theme, "card", "Hover");
            // The side diamonds are separate pieces so they never stretch with the card (D-027).
            Frame.Ornament(frame, theme, "card_knot_left", Edge.Left);
            Frame.Ornament(frame, theme, "card_knot_right", Edge.Right);
            _content = theme.Content("card", new Vector4(12f, 12f, 12f, 12f));
            var border = theme.Border("card");
            // The knotted corners must never overlap: the card is at least their size.
            _minSize = new Vector2(border.x + border.z + 24f, border.y + border.w + 8f);

            _title = Ui.Text(_card, "Title", theme, FontRole.Label, 17f, ThemeRuntime.ToUnity(t.AccentGoldBright), TextAlignmentOptions.TopLeft, outlined: true);
            _body = Ui.Text(_card, "Actions", theme, FontRole.BodyStrong, 17f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.TopLeft, outlined: true);
            _body.textWrappingMode = TextWrappingModes.Normal;
            _title.textWrappingMode = TextWrappingModes.Normal;
            _shown = null;
            _appliedOffset = new Vector2(float.NaN, float.NaN);
            _card.gameObject.SetActive(false);
        }

        public void Refresh(float deltaSeconds)
        {
            if (_card == null) return;
            var offset = new Vector2(_offsetX.Value, _offsetY.Value);
            if (offset != _appliedOffset) { _card.anchoredPosition = offset; _appliedOffset = offset; }

            var source = Hud.instance != null ? Hud.instance.m_hoverName : null;
            // Vanilla keeps the hover text while the large map is open (a Vegvísir opens it) and
            // lets the map cover it; the card hides instead (R-040).
            bool mapOpen = global::Minimap.instance != null && global::Minimap.IsOpen();
            string text = !mapOpen && source != null && source.gameObject.activeInHierarchy ? source.text : null;
            bool show = !string.IsNullOrEmpty(text);
            if (_card.gameObject.activeSelf != show) _card.gameObject.SetActive(show);
            if (!show) { _shown = null; return; }

            float alpha = Mathf.Clamp01(source.canvasRenderer.GetAlpha() * source.color.a);
            if (!Mathf.Approximately(_opacity.alpha, alpha)) _opacity.alpha = alpha;
            if (text == _shown) return;
            _shown = text;
            Layout(text);
        }

        public void Teardown()
        {
            if (_card != null) Object.Destroy(_card.gameObject);
            _card = null;
        }

        /// <summary>First line is the title; the rest are actions. Runs only when the text changes.</summary>
        private void Layout(string text)
        {
            int newline = text.IndexOf('\n');
            string title = newline < 0 ? text : text.Substring(0, newline);
            string body = newline < 0 ? "" : text.Substring(newline + 1).Trim('\n');
            // Vanilla marks keys in yellow; our gold keeps them readable and on theme.
            body = body.Replace("<color=yellow>", "<color=#F7E283>");

            _title.text = title;
            _body.text = body;
            float inner = MaxWidth - _content.x - _content.z - 2f * Pad;
            Vector2 ts = _title.GetPreferredValues(title, inner, 0f);
            Vector2 bs = body.Length > 0 ? _body.GetPreferredValues(body, inner, 0f) : Vector2.zero;
            float width = Mathf.Min(inner, Mathf.Max(ts.x, bs.x)) + _content.x + _content.z + 2f * Pad;
            float height = ts.y + (body.Length > 0 ? Gap + bs.y : 0f) + _content.y + _content.w + 2f * Pad * 0.6f;
            width = Mathf.Max(width, _minSize.x);
            height = Mathf.Max(height, _minSize.y);
            _card.sizeDelta = new Vector2(width, height);

            // Centred vertically: a short card may be taller than its text (the corners' minimum).
            float textHeight = ts.y + (body.Length > 0 ? Gap + bs.y : 0f);
            float left = _content.x + Pad, top = (height - textHeight) / 2f;
            Place(_title.rectTransform, left, top, width - left - _content.z - Pad, ts.y);
            Place(_body.rectTransform, left, top + ts.y + Gap, width - left - _content.z - Pad, bs.y);
            _body.gameObject.SetActive(body.Length > 0);
        }

        private static void Place(RectTransform rt, float left, float top, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(left, -top);
            rt.sizeDelta = new Vector2(width, height);
        }
    }
}
