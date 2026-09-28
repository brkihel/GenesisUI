using GenesisUI.Theme;
using GenesisUI.Vitals;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Vitals
{
    /// <summary>How a bar's "living" liquid moves (docs/ART-DIRECTION.md §5: quiet motion).</summary>
    internal struct BarMotion
    {
        /// <summary>Upward scroll of the bubbles, in tile heights per second.</summary>
        public float Speed;
        /// <summary>Kept low: the bubbles are a subtle detail (Diego, R-030).</summary>
        public float PatternAlpha;
        /// <summary>A second, slower bubble layer for parallax (eitr); 0 = none.</summary>
        public float CounterSpeed;
    }

    /// <summary>
    /// One framed vertical bar, v2: slim ornate frame, the trail of the last loss, and a
    /// liquid that rises and falls inside a mask. The flow texture scrolls up through the
    /// liquid and is never squashed, because the liquid keeps the full bar height and only
    /// the mask moves. Only draws what a BarAnimator says.
    /// </summary>
    internal sealed class VitalBarView
    {
        // Fill area inside bar_frame v2, in design units (see art/src/bar_frame.svg).
        private const float InsetSide = 9.5f;
        private const float InsetBottom = 24f;
        private const float InsetTop = 32f;

        public readonly RectTransform Root;
        private readonly Image _frame;
        private readonly Image _trail;
        private readonly RectTransform _mask;
        private readonly RawImage _flow;
        private readonly RawImage _counterFlow;
        private readonly Image _surface;
        private readonly TextMeshProUGUI _number;
        private readonly Color _frameColor;
        private readonly Color _dangerColor;
        private readonly float _areaHeight;
        private readonly float _tileHeightUv;
        private readonly BarMotion _motion;
        private float _shownFast = -1f;

        public VitalBarView(RectTransform parent, string name, ThemeRuntime theme, ColorRgba barColor, Vector2 position, Vector2 size, BarMotion motion)
        {
            _motion = motion;
            Root = Ui.Place(Ui.Child(parent, name), new Vector2(0f, 0f), position, size);

            var frameSprite = theme.Sprite("bar_frame");
            _frameColor = frameSprite != null ? Color.white : ThemeRuntime.ToUnity(theme.Tokens.PanelBackground);
            _dangerColor = ThemeRuntime.ToUnity(theme.Tokens.StateDanger);
            _frame = Ui.Image(Ui.Fill(Ui.Child(Root, "Frame")), frameSprite, _frameColor);

            var color = ThemeRuntime.ToUnity(barColor);
            var light = Color.Lerp(color, Color.white, 0.55f);
            var area = Ui.Fill(Ui.Child(Root, "FillArea"), InsetSide, InsetBottom, InsetSide, InsetTop);
            _areaHeight = size.y - InsetTop - InsetBottom;
            float areaWidth = size.x - 2f * InsetSide;
            var fillSprite = theme.Sprite("bar_fill");

            _trail = Ui.Image(Ui.Fill(Ui.Child(area, "Trail")), fillSprite, new Color(color.r, color.g, color.b, theme.Tokens.BarTrailAlpha));
            _trail.type = Image.Type.Filled;
            _trail.fillMethod = Image.FillMethod.Vertical;
            _trail.fillOrigin = (int)Image.OriginVertical.Bottom;
            _trail.fillAmount = 0f;

            // The mask grows from the bottom; its children keep the full area height.
            _mask = Ui.Child(area, "Liquid");
            _mask.anchorMin = new Vector2(0f, 0f);
            _mask.anchorMax = new Vector2(1f, 0f);
            _mask.pivot = new Vector2(0.5f, 0f);
            _mask.anchoredPosition = Vector2.zero;
            _mask.sizeDelta = new Vector2(0f, 0f);
            _mask.gameObject.AddComponent<RectMask2D>();

            Ui.Image(FullHeight(Ui.Child(_mask, "Body")), fillSprite, color);

            // The flow tile is 1:2; one tile is as wide as the bar, so uv height = area / (2 * width).
            _tileHeightUv = areaWidth > 0f ? _areaHeight / (2f * areaWidth) : 1f;
            var flowTex = theme.Texture("bar_bubbles");
            if (flowTex != null)
            {
                _flow = FlowLayer(_mask, "Flow", flowTex, new Color(light.r, light.g, light.b, motion.PatternAlpha), 0f);
                if (motion.CounterSpeed != 0f)
                    _counterFlow = FlowLayer(_mask, "CounterFlow", flowTex, new Color(light.r, light.g, light.b, motion.PatternAlpha * 0.6f), 0.5f);
            }

            // Bright meniscus at the liquid's surface.
            var surfaceRt = Ui.Child(_mask, "Surface");
            surfaceRt.anchorMin = new Vector2(0f, 1f);
            surfaceRt.anchorMax = new Vector2(1f, 1f);
            surfaceRt.pivot = new Vector2(0.5f, 1f);
            surfaceRt.anchoredPosition = Vector2.zero;
            surfaceRt.sizeDelta = new Vector2(0f, 2f);
            _surface = Ui.Image(surfaceRt, null, new Color(light.r, light.g, light.b, 0.85f));

            _number = Ui.Fit(Ui.Text(Root, "Value", theme, FontRole.Display, 19f, ThemeRuntime.ToUnity(theme.Tokens.TextTitle),
                                     TextAlignmentOptions.Center, outlined: true), 12f);
            // The number lives inside the liquid's width, so it can never cross the frame.
            Ui.Fill((RectTransform)_number.transform, InsetSide - 1f, InsetBottom, InsetSide - 1f, InsetTop);
        }

        public void SetVisible(bool visible)
        {
            if (Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible);
        }

        /// <param name="danger">0..1 pulse strength of the low-value alert (0 = none).</param>
        public void Apply(BarAnimator bar, float danger, float deltaSeconds)
        {
            if (Mathf.Abs(bar.Fast - _shownFast) > 0.0005f)
            {
                _shownFast = bar.Fast;
                _mask.sizeDelta = new Vector2(0f, _areaHeight * bar.Fast);
                bool any = bar.Fast > 0.001f;
                if (_surface.enabled != any) _surface.enabled = any;
            }
            _trail.fillAmount = bar.Slow;
            if (bar.DisplayChanged) _number.SetText("{0}", bar.Display); // SetText with an int does not allocate
            var frame = danger > 0f ? Color.Lerp(_frameColor, _dangerColor, danger * 0.8f) : _frameColor;
            if (_frame.color != frame) _frame.color = frame;

            _time += deltaSeconds;
            Scroll(_flow, _motion.Speed, 0f, deltaSeconds);
            Scroll(_counterFlow, _motion.CounterSpeed, 0.5f, deltaSeconds);
        }

        private float _time;

        private void Scroll(RawImage layer, float speed, float baseX, float dt)
        {
            if (layer == null || speed == 0f) return;
            var uv = layer.uvRect;
            uv.y = Mathf.Repeat(uv.y - speed * dt, 1f); // lower v = the texture moves up
            // A slight side-to-side sway, as bubbles do while rising.
            uv.x = baseX + 0.018f * Mathf.Sin(_time * 1.3f + baseX * 6f);
            layer.uvRect = uv;
        }

        private RawImage FlowLayer(RectTransform parent, string name, Texture texture, Color color, float xOffset)
        {
            var rt = FullHeight(Ui.Child(parent, name));
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.color = color;
            raw.raycastTarget = false;
            raw.uvRect = new Rect(xOffset, 0f, 1f, _tileHeightUv);
            return raw;
        }

        private RectTransform FullHeight(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, _areaHeight);
            return rt;
        }
    }
}
