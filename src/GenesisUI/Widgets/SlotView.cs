using GenesisUI.HudModel;
using GenesisUI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// One item cell (docs/ART-DIRECTION.md C1): frame, icon, key index top-left, amount or
    /// time bottom-right, a thin bar at the bottom (durability or food left), and a gold
    /// overlay for the active state. Every setter only touches Unity when the value changed.
    /// </summary>
    internal sealed class SlotView
    {
        public readonly RectTransform Root;
        private readonly Image _icon;
        private readonly Image _active;
        private readonly Image _barBack;
        private readonly Image _bar;
        private readonly TextMeshProUGUI _index;
        private readonly TextMeshProUGUI _corner;
        private readonly Color _barColor;
        private readonly Color _dangerColor;

        private Sprite _shownSprite;
        private int _shownCorner = int.MinValue;
        private char _shownUnit;
        private float _shownBar = -1f;
        private bool _shownActive;
        private bool _hasIcon;

        public SlotView(RectTransform parent, string name, ThemeRuntime theme, Vector2 anchor, Vector2 position, float size, string indexLabel)
        {
            var t = theme.Tokens;
            Root = Ui.Place(Ui.Child(parent, name), anchor, position, new Vector2(size, size));

            var frame = theme.Sprite("slot");
            Ui.Image(Ui.Fill(Ui.Child(Root, "Frame")), frame, frame != null ? Color.white : ThemeRuntime.ToUnity(t.PanelBackground));

            float pad = size * 0.14f;
            _icon = Ui.Image(Ui.Fill(Ui.Child(Root, "Icon"), pad, pad, pad, pad), null, Color.white);
            _icon.preserveAspect = true;
            _icon.enabled = false;

            float barH = Mathf.Max(2f, size * 0.05f);
            _barBack = Ui.Image(Ui.Place(Ui.Child(Root, "BarBack"), new Vector2(0f, 0f), new Vector2(size * 0.14f, size * 0.08f), new Vector2(size * 0.72f, barH)),
                                null, new Color(0f, 0f, 0f, 0.6f));
            _barColor = ThemeRuntime.ToUnity(t.StatePositive);
            _dangerColor = ThemeRuntime.ToUnity(t.StateDanger);
            _bar = Ui.Image(Ui.Fill(Ui.Child((RectTransform)_barBack.transform, "Bar")), theme.Sprite("bar_fill"), _barColor);
            _bar.type = Image.Type.Filled;
            _bar.fillMethod = Image.FillMethod.Horizontal;
            _bar.fillOrigin = (int)Image.OriginHorizontal.Left;
            _barBack.gameObject.SetActive(false);

            _active = Ui.Image(Ui.Fill(Ui.Child(Root, "Active"), -3f, -3f, -3f, -3f), theme.Sprite("slot_active"),
                               theme.Sprite("slot_active") != null ? Color.white : ThemeRuntime.ToUnity(t.AccentGold).WithA(0.35f));
            _active.enabled = false;

            if (indexLabel != null)
            {
                _index = Ui.Text(Root, "Index", theme, FontRole.Label, size * 0.24f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.TopLeft, outlined: true);
                Ui.Fill((RectTransform)_index.transform, size * 0.1f, 0f, 0f, size * 0.05f);
                _index.text = indexLabel;
            }

            _corner = Ui.Fit(Ui.Text(Root, "Corner", theme, FontRole.Display, size * 0.26f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.BottomRight, outlined: true), 10f);
            Ui.Fill((RectTransform)_corner.transform, size * 0.12f, size * 0.1f, size * 0.1f, size * 0.55f);
            _corner.text = "";
        }

        public void SetIcon(Sprite sprite, float alpha = 1f)
        {
            if (sprite != _shownSprite)
            {
                _shownSprite = sprite;
                _icon.sprite = sprite;
                _hasIcon = sprite != null;
                _icon.enabled = _hasIcon;
            }
            if (_hasIcon && !Mathf.Approximately(_icon.color.a, alpha)) _icon.color = new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>Stack amount; hidden when 1 or less, like vanilla.</summary>
        public void SetAmount(int amount)
        {
            int shown = amount > 1 ? amount : int.MinValue;
            if (shown == _shownCorner && _shownUnit == '#') return;
            _shownCorner = shown;
            _shownUnit = '#';
            if (shown == int.MinValue) _corner.text = "";
            else _corner.SetText("{0}", amount);
        }

        /// <summary>A remaining time ("12m", "45s", "4:28"); urgent times blink.</summary>
        public void SetTime(TimeText time, float blink)
        {
            if (time.Value != _shownCorner || time.Unit != _shownUnit)
            {
                _shownCorner = time.Value;
                _shownUnit = time.Unit;
                if (time.Unit == ':') _corner.SetText("{0}:{1:00}", time.Minutes, time.Seconds);
                else if (time.Unit == 'm') _corner.SetText("{0}m", time.Value);
                else _corner.SetText("{0}s", time.Value);
            }
            float a = time.Urgent ? 0.4f + 0.6f * blink : 1f;
            if (!Mathf.Approximately(_corner.alpha, a)) _corner.alpha = a;
        }

        public void ClearCorner()
        {
            if (_shownCorner == int.MinValue && _shownUnit == '\0') return;
            _shownCorner = int.MinValue;
            _shownUnit = '\0';
            _corner.text = "";
        }

        /// <param name="value">0..1, or a negative number to hide the bar.</param>
        public void SetBar(float value, bool danger = false)
        {
            bool show = value >= 0f;
            if (_barBack.gameObject.activeSelf != show) _barBack.gameObject.SetActive(show);
            if (!show) { _shownBar = -1f; return; }
            if (Mathf.Abs(value - _shownBar) > 0.002f)
            {
                _shownBar = value;
                _bar.fillAmount = value;
            }
            var c = danger ? _dangerColor : _barColor;
            if (_bar.color != c) _bar.color = c;
        }

        public void SetActive(bool active)
        {
            if (active == _shownActive) return;
            _shownActive = active;
            _active.enabled = active;
        }
    }

    internal static class ColorExtensions
    {
        public static Color WithA(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
