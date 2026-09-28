using GenesisUI.HudModel;
using GenesisUI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// Status tile (docs/ART-DIRECTION.md C8): framed icon with a diamond on top, the
    /// effect name below, the time under the name. Setters only touch Unity on change.
    /// </summary>
    internal sealed class TileView
    {
        public const float CellWidth = 84f;
        public const float CellHeight = 104f;
        private const float TileSize = 60f;

        public readonly RectTransform Root;
        private readonly Image _frame;
        private readonly Image _icon;
        private readonly TextMeshProUGUI _name;
        private readonly TextMeshProUGUI _time;
        private readonly Image _badge;
        private readonly Color _frameColor;
        private readonly Color _readyColor;
        private readonly Color _dangerColor;

        private Sprite _shownSprite;
        private string _shownNameSource;
        private string _shownTime;
        private int _shownClock = int.MinValue;

        public TileView(RectTransform parent, string name, ThemeRuntime theme)
        {
            var t = theme.Tokens;
            Root = Ui.Place(Ui.Child(parent, name), new Vector2(1f, 1f), Vector2.zero, new Vector2(CellWidth, CellHeight));

            var sprite = theme.Sprite("tile");
            _frameColor = sprite != null ? Color.white : ThemeRuntime.ToUnity(t.PanelBackground);
            _readyColor = ThemeRuntime.ToUnity(t.AccentGoldBright);
            _dangerColor = ThemeRuntime.ToUnity(t.StateDanger);
            var frameRt = Ui.Place(Ui.Child(Root, "Frame"), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(TileSize, TileSize));
            _frame = Ui.Image(frameRt, sprite, _frameColor);

            _icon = Ui.Image(Ui.Fill(Ui.Child(frameRt, "Icon"), 13f, 10f, 13f, 16f), null, Color.white);
            _icon.preserveAspect = true;

            // Small corner badge (the guardian power's cooldown): top-right of the tile.
            var badgeSprite = theme.Sprite("badge_cooldown");
            _badge = Ui.Image(Ui.Place(Ui.Child(frameRt, "Badge"), new Vector2(1f, 1f), new Vector2(4f, -2f), new Vector2(20f, 20f)),
                              badgeSprite, badgeSprite != null ? Color.white : _dangerColor);
            _badge.rectTransform.pivot = new Vector2(1f, 1f);
            _badge.enabled = false;

            _name = Ui.Text(Root, "Name", theme, FontRole.BodyStrong, 15f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.Top, outlined: true);
            Ui.Place((RectTransform)_name.transform, new Vector2(0.5f, 1f), new Vector2(0f, -TileSize - 1f), new Vector2(CellWidth + 16f, 20f));

            _time = Ui.Text(Root, "Time", theme, FontRole.Label, 13f, ThemeRuntime.ToUnity(t.TextBody), TextAlignmentOptions.Top, outlined: true);
            Ui.Place((RectTransform)_time.transform, new Vector2(0.5f, 1f), new Vector2(0f, -TileSize - 20f), new Vector2(CellWidth, 18f));
        }

        public void SetVisible(bool visible)
        {
            if (Root.gameObject.activeSelf != visible) Root.gameObject.SetActive(visible);
        }

        public void SetPosition(Vector2 position)
        {
            if (Root.anchoredPosition != position) Root.anchoredPosition = position;
        }

        public void SetIcon(Sprite sprite)
        {
            if (sprite == _shownSprite) return;
            _shownSprite = sprite;
            _icon.sprite = sprite;
            _icon.enabled = sprite != null;
        }

        /// <param name="source">The raw (token) name; only re-localized when it changes.</param>
        public void SetName(string source)
        {
            if (source == _shownNameSource) return;
            _shownNameSource = source;
            _name.text = source == null ? "" : Localization.instance != null ? Localization.instance.Localize(source) : source;
        }

        /// <summary>Vanilla's own icon text (time or stacks); replaced only when it changes.</summary>
        public void SetTimeText(string text)
        {
            _shownClock = int.MinValue;
            if (text == _shownTime) return;
            _shownTime = text;
            _time.text = text ?? "";
        }

        public void SetClock(TimeText clock)
        {
            _shownTime = null;
            if (clock.Value == _shownClock) return;
            _shownClock = clock.Value;
            if (clock.Value <= 0) _time.text = "";
            else _time.SetText("{0}:{1:00}", clock.Minutes, clock.Seconds);
        }

        public void SetBadge(bool visible)
        {
            if (_badge.enabled != visible) _badge.enabled = visible;
        }

        /// <param name="glow">0..1 gold of the frame (1 = ready, pulsing while active, 0 = normal).</param>
        /// <param name="flash">0..1 red flash of the icon (vanilla m_flashIcon), 0 for none.</param>
        public void SetState(float glow, float flash, bool dimmed)
        {
            var frame = glow > 0f ? Color.Lerp(_frameColor, _readyColor, glow) : _frameColor;
            if (_frame.color != frame) _frame.color = frame;
            var icon = flash > 0f ? Color.Lerp(Color.white, _dangerColor, flash) : Color.white;
            if (dimmed) icon = new Color(icon.r * 0.55f, icon.g * 0.55f, icon.b * 0.55f, 1f);
            if (_icon.color != icon) _icon.color = icon;
        }
    }
}
