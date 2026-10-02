using GenesisUI.HudModel;
using GenesisUI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// Status tile: the same quiet slot frame as the active food buffs (Diego, R-049), the
    /// effect name below, the time under the name. Setters only touch Unity on change.
    /// </summary>
    [GenesisUI.Foundation.Contracts.GameContract("assembly_guiutils", "Localization", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Localization")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_guiutils", "Localization", "Localize", Parameters = new string[] { "System.String" }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Widgets.Frame))]
    internal sealed class TileView
    {
        public const float CellWidth = 70f;          // tiles close together (Diego, R-046); names shrink to fit
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

            string frameName = theme.Sprite("hotslot") != null ? "hotslot" : "slot";
            var sprite = theme.Sprite(frameName);
            _frameColor = sprite != null ? Color.white : ThemeRuntime.ToUnity(t.PanelBackground);
            _readyColor = ThemeRuntime.ToUnity(t.AccentGoldBright);
            _dangerColor = ThemeRuntime.ToUnity(t.StateDanger);
            var frameRt = Ui.Place(Ui.Child(Root, "Tile"), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(TileSize, TileSize));
            _frame = Frame.Dress(frameRt, theme, frameName, "Status", TileSize);

            // The icon sits inside the frame's declared content (scaled like the frame).
            var drawn = theme.Size(frameName);
            var c = theme.Content(frameName, new Vector4(8f, 8f, 8f, 8f)) * (drawn.y > 0f ? TileSize / drawn.y : 1f);
            _icon = Ui.Image(Ui.Fill(Ui.Child(frameRt, "Icon"), c.x + 2f, c.y + 2f, c.z + 2f, c.w + 2f), null, Color.white);
            _icon.preserveAspect = true;

            // Small corner badge (the guardian power's cooldown): top-right of the tile.
            var badgeSprite = theme.Sprite("badge_cooldown");
            _badge = Ui.Image(Ui.Place(Ui.Child(frameRt, "Badge"), new Vector2(1f, 1f), new Vector2(4f, -2f), new Vector2(20f, 20f)),
                              badgeSprite, badgeSprite != null ? Color.white : _dangerColor);
            _badge.rectTransform.pivot = new Vector2(1f, 1f);
            _badge.enabled = false;

            _name = Ui.Fit(Ui.Text(Root, "Name", theme, FontRole.BodyStrong, 15f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.Top, outlined: true), 11f);
            Ui.Place((RectTransform)_name.transform, new Vector2(0.5f, 1f), new Vector2(0f, -TileSize - 1f), new Vector2(CellWidth - 2f, 20f));

            _time = Ui.Text(Root, "Time", theme, FontRole.Label, 13f, ThemeRuntime.ToUnity(t.TextBody), TextAlignmentOptions.Top, outlined: true);
            Ui.Place((RectTransform)_time.transform, new Vector2(0.5f, 1f), new Vector2(0f, -TileSize - 20f), new Vector2(CellWidth, 18f));
        }

        /// <summary>The framed square (without the name and time under it).</summary>
        public RectTransform TileRect => _frame.rectTransform;

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

        // Marks "the text shows a clock", so a later SetTimeText(null) really clears it (R-030:
        // resetting the power's cooldown left the last m:ss frozen on a ready tile).
        private const string ClockShown = "\u0001clock";

        public void SetClock(TimeText clock)
        {
            _shownTime = ClockShown;
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
