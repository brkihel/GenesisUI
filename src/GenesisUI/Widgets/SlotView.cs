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
        private bool _shownEquipped;
        private readonly Image _equipped;
        private bool _hasIcon;

        public SlotView(RectTransform parent, string name, ThemeRuntime theme, Vector2 anchor, Vector2 position, float size, string indexLabel)
            : this(parent, name, theme, anchor, position, new Vector2(size, size), indexLabel, "slot", null)
        {
        }

        /// <param name="frameSprite">
        /// The cell's own frame and background, or null when the cell is part of a larger piece that
        /// already draws it (the hotbar's eight cells, D-027).
        /// </param>
        /// <param name="panel">The background's key in [Backgrounds].</param>
        public SlotView(RectTransform parent, string name, ThemeRuntime theme, Vector2 anchor, Vector2 position, Vector2 cell,
                        string indexLabel, string frameSprite, string panel)
        {
            var t = theme.Tokens;
            float size = Mathf.Min(cell.x, cell.y);
            Root = Ui.Place(Ui.Child(parent, name), anchor, position, cell);

            if (frameSprite != null)
            {
                // Corners and diamonds scale with the cell's height; only plain rails stretch.
                Widgets.Frame.Dress(Root, theme, frameSprite, panel, cell.y);
            }

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

            // States drawn by Diego (R-042): the lit gold cell when selected, the green one when
            // equipped, laid over the cell with their window on the cell's. Else a soft inner glow.
            _active = State(theme, "slot_selected", cell);
            if (_active == null)
            {
                var glow = theme.Sprite("cell_glow");
                if (glow == null) glow = theme.Sprite("slot_active");
                _active = Ui.Image(Ui.Fill(Ui.Child(Root, "Active"), 1f, 1f, 1f, 1f), glow,
                                   glow != null ? Color.white : ThemeRuntime.ToUnity(t.AccentGold).WithA(0.35f));
                _active.type = Image.Type.Simple;
            }
            _equipped = State(theme, "slot_equipped", cell);
            if (_equipped == null) _equipped = _active;
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

        public void SetActive(bool active) => SetState(active, false);

        /// <summary>Selected wins over equipped when both apply (the focus is what the player acts on).</summary>
        public void SetState(bool selected, bool equipped)
        {
            if (selected == _shownActive && equipped == _shownEquipped) return;
            _shownActive = selected;
            _shownEquipped = equipped;
            _active.enabled = selected;
            if (_equipped != _active) _equipped.enabled = equipped && !selected;
            else _active.enabled = selected || equipped;
        }

        /// <summary>
        /// A state frame over the cell, uniformly scaled so its window matches the cell's width and
        /// centred on it; null when the art lacks it.
        /// </summary>
        private Image State(ThemeRuntime theme, string sprite, Vector2 cell)
        {
            var s = theme.Sprite(sprite);
            if (s == null) return null;
            var drawn = theme.Size(sprite);
            var c = theme.Content(sprite, Vector4.zero);
            float window = drawn.x - c.x - c.z;
            if (window <= 0f) return null;
            float k = cell.x / window;
            var rt = Ui.Place(Ui.Child(Root, sprite), new Vector2(0.5f, 0.5f), Vector2.zero, drawn * k);
            // The window's centre may sit off the sprite's centre by a pixel or two: follow the window.
            rt.anchoredPosition = new Vector2((c.x - c.z) * k / 2f, (c.y - c.w) * k / 2f);
            var img = Ui.Image(rt, s, Color.white);
            img.enabled = false;
            return img;
        }
    }

    internal static class ColorExtensions
    {
        public static Color WithA(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
