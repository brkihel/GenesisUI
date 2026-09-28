using GenesisUI.Theme;
using GenesisUI.Vitals;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Vitals
{
    /// <summary>
    /// One framed vertical bar (docs/ART-DIRECTION.md O6): arched frame, trail of the
    /// recent loss, the value fill and the number. Only draws what a BarAnimator says.
    /// </summary>
    internal sealed class VitalBarView
    {
        // Fill area inside bar_frame, in design units (see art/src/bar_frame.svg).
        private const float InsetSide = 9.5f;
        private const float InsetBottom = 9f;
        private const float InsetTop = 30f;

        public readonly RectTransform Root;
        private readonly Image _frame;
        private readonly Image _trail;
        private readonly Image _fill;
        private readonly TextMeshProUGUI _number;
        private readonly Color _frameColor;
        private readonly Color _dangerColor;

        public VitalBarView(RectTransform parent, string name, ThemeRuntime theme, ColorRgba barColor, Vector2 position, Vector2 size)
        {
            Root = Ui.Place(Ui.Child(parent, name), new Vector2(0f, 0f), position, size);

            var frameSprite = theme.Sprite("bar_frame");
            _frameColor = frameSprite != null ? Color.white : ThemeRuntime.ToUnity(theme.Tokens.PanelBackground);
            _dangerColor = ThemeRuntime.ToUnity(theme.Tokens.StateDanger);
            _frame = Ui.Image(Ui.Fill(Ui.Child(Root, "Frame")), frameSprite, _frameColor);

            var area = Ui.Fill(Ui.Child(Root, "FillArea"), InsetSide, InsetBottom, InsetSide, InsetTop);
            var fillSprite = theme.Sprite("bar_fill");
            var color = ThemeRuntime.ToUnity(barColor);

            _trail = Ui.Image(Ui.Fill(Ui.Child(area, "Trail")), fillSprite, new Color(color.r, color.g, color.b, theme.Tokens.BarTrailAlpha));
            MakeVerticalFill(_trail);
            _fill = Ui.Image(Ui.Fill(Ui.Child(area, "Fill")), fillSprite, color);
            MakeVerticalFill(_fill);

            _number = Ui.Text(Root, "Value", theme, FontRole.Display, 19f, ThemeRuntime.ToUnity(theme.Tokens.TextTitle),
                              TextAlignmentOptions.Center, outlined: true);
            Ui.Fill((RectTransform)_number.transform, 0f, InsetBottom, 0f, InsetTop);
        }

        public void SetVisible(bool visible)
        {
            if (Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible);
        }

        /// <param name="danger">0..1 pulse strength of the low-value alert (0 = none).</param>
        public void Apply(BarAnimator bar, float danger)
        {
            _fill.fillAmount = bar.Fast;
            _trail.fillAmount = bar.Slow;
            if (bar.DisplayChanged) _number.SetText("{0}", bar.Display); // SetText with an int does not allocate
            _frame.color = danger > 0f ? Color.Lerp(_frameColor, _dangerColor, danger * 0.8f) : _frameColor;
        }

        private static void MakeVerticalFill(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Vertical;
            image.fillOrigin = (int)Image.OriginVertical.Bottom;
            image.fillAmount = 0f;
        }
    }
}
