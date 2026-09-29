using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Vitals;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Sprint
{
    /// <summary>
    /// A small stamina readout above the hotbar. It appears whenever stamina is below full,
    /// whatever spent it (running, attacking, jumping, dodging, blocking, swimming), and only
    /// fades once stamina is completely back (Diego, R-032).
    /// </summary>
    [GameContract("assembly_valheim", "Player", "m_localPlayer")]
    [GameContract("assembly_valheim", "Player", "GetStamina", Parameters = new string[0])]
    [GameContract("assembly_valheim", "Player", "GetMaxStamina", Parameters = new string[0])]
    internal sealed class SprintModule : IUiModule
    {
        private const float Width = 220f;
        private const float Height = 22f;             // the generated frame's height; Diego's frame keeps its own proportions
        private const float FullHoldSeconds = 0.8f;   // stays a moment after stamina is full...
        private const float FadeSeconds = 0.4f;       // ...then fades
        private static readonly string[] NoVanillaRegions = new string[0];

        private readonly ConfigEntry<int> _offsetY;
        private readonly ConfigEntry<float> _scale;
        private RectTransform _group;
        private CanvasGroup _opacity;
        private Image _trail;
        private Image _fill;
        private RectTransform _liquidMask;
        private RectTransform _burnRt;
        private RawImage _burn;
        private LiquidLayer _liquid;
        private BarAnimator _bar;
        private float _linger;
        private int _appliedY = int.MinValue;
        private float _appliedScale = float.NaN;

        public SprintModule(ConfigFile config)
        {
            _offsetY = config.Bind("Sprint", "OffsetY", 142,
                new ConfigDescription("Distância da barra de corrida até a borda inferior da tela.",
                    new AcceptableValueRange<int>(0, 900)));
            _scale = config.Bind("Sprint", "Scale", 1f,
                new ConfigDescription("Tamanho da barra de corrida (1 = padrão).",
                    new AcceptableValueRange<float>(0.5f, 2f)));
        }

        public string Id => "hud.sprint";
        public string NameToken => "$genesisui_module_sprint";
        public IReadOnlyList<string> Regions => NoVanillaRegions;
        public float RefreshRate => 30f;

        public void Build(ModuleContext context)
        {
            var theme = context.Theme;
            _group = Ui.Place(Ui.Child(context.Root, "Sprint"), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(Width, Height));
            _opacity = _group.gameObject.AddComponent<CanvasGroup>();
            _opacity.alpha = 0f;
            _opacity.interactable = false;
            _opacity.blocksRaycasts = false;

            var drawn = theme.Size("sprint_frame");
            var track = drawn.x > 0f && theme.Sprite("sprint_frame_opening") != null
                ? SheetFrame(theme, drawn)
                : GeneratedFrame(theme);
            var fillSprite = theme.Sprite("bar_fill");
            var stamina = ThemeRuntime.ToUnity(theme.Tokens.BarStamina);
            _trail = Ui.Image(Ui.Fill(Ui.Child(track, "Trail")), fillSprite,
                new Color(stamina.r, stamina.g, stamina.b, theme.Tokens.BarTrailAlpha * 0.35f)); // a faint trail (R-048)
            _trail.type = Image.Type.Filled;
            _trail.fillMethod = Image.FillMethod.Horizontal;
            _trail.fillAmount = 0f;
            _fill = Ui.Image(Ui.Fill(Ui.Child(track, "Fill")), fillSprite, stamina);
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillAmount = 0f;

            // Diego's stamina liquid drifting along the channel, cut at the level (D-027).
            _liquid = null;
            var liquid = theme.Texture("liquid_stamina_h");
            if (liquid != null)
            {
                _fill.enabled = false;
                _liquidMask = Ui.Child(track, "Liquid");
                _liquidMask.anchorMin = Vector2.zero;
                _liquidMask.anchorMax = new Vector2(0f, 1f);
                _liquidMask.offsetMin = _liquidMask.offsetMax = Vector2.zero;
                _liquidMask.gameObject.AddComponent<RectMask2D>();
                var area = track.rect.size;
                var flow = Ui.Place(Ui.Child(_liquidMask, "Flow"), new Vector2(0f, 0.5f), Vector2.zero, area);
                flow.pivot = new Vector2(0f, 0.5f);
                _liquid = new LiquidLayer(flow, liquid, area, theme.Size("liquid_stamina_h"), 1.2f, Color.white,
                    new Vector2(0.02f, 0f), new Vector2(0f, 0.35f), mirror: false);
            }

            // The burn: a short hot edge where stamina is being spent, like the vertical bars'.
            var burnTex = theme.Texture("bar_burn");
            if (burnTex != null)
            {
                _burnRt = Ui.Child(track, "Burn");
                _burnRt.anchorMin = new Vector2(0f, 0f);
                _burnRt.anchorMax = new Vector2(0f, 1f);
                _burnRt.offsetMin = _burnRt.offsetMax = Vector2.zero;
                _burn = _burnRt.gameObject.AddComponent<RawImage>();
                _burn.texture = burnTex;
                _burn.color = new Color(1f, 0.93f, 0.62f, 0.9f);
                _burn.raycastTarget = false;
                _burn.uvRect = new Rect(0f, 0f, 1f, 1f);
            }

            _bar = new BarAnimator();
            _linger = 0f;
            _appliedY = int.MinValue;
            _appliedScale = float.NaN;
            _group.gameObject.SetActive(false);
        }

        public void Refresh(float deltaSeconds)
        {
            if (_group == null) return;
            int y = _offsetY.Value;
            if (y != _appliedY)
            {
                _group.anchoredPosition = new Vector2(0f, y);
                _appliedY = y;
            }
            float scale = _scale.Value;
            if (scale != _appliedScale)
            {
                _group.localScale = Vector3.one * scale;
                _appliedScale = scale;
            }

            var player = Player.m_localPlayer;
            float stamina = player != null ? player.GetStamina() : 0f;
            float max = player != null ? player.GetMaxStamina() : 0f;
            // Anything that spends stamina shows the bar; it goes only once stamina is full again.
            bool spent = player != null && max > 0f && stamina < max - 0.5f;
            if (spent) _linger = FullHoldSeconds + FadeSeconds;
            else _linger = Mathf.Max(0f, _linger - deltaSeconds);
            bool visible = player != null && _linger > 0f;
            if (_group.gameObject.activeSelf != visible) _group.gameObject.SetActive(visible);
            if (!visible) return;

            float alpha = Mathf.Clamp01(_linger / FadeSeconds);
            if (!Mathf.Approximately(_opacity.alpha, alpha)) _opacity.alpha = alpha;
            _bar.Update(stamina, max, deltaSeconds);
            if (!Mathf.Approximately(_fill.fillAmount, _bar.Fast)) _fill.fillAmount = _bar.Fast;
            if (_liquid != null)
            {
                if (!Mathf.Approximately(_liquidMask.anchorMax.x, _bar.Fast)) _liquidMask.anchorMax = new Vector2(_bar.Fast, 1f);
                _liquid.Scroll(deltaSeconds);
            }

            if (!Mathf.Approximately(_trail.fillAmount, _bar.Slow)) _trail.fillAmount = _bar.Slow;
            if (_burn != null)
            {
                // Only a short edge above the level, never a long band (as the vertical bars, R-040).
                float to = Mathf.Min(_bar.Slow, _bar.Fast + 0.035f);
                bool burning = to > _bar.Fast + 0.002f;
                if (_burn.enabled != burning) _burn.enabled = burning;
                if (burning)
                {
                    _burnRt.anchorMin = new Vector2(_bar.Fast, 0f);
                    _burnRt.anchorMax = new Vector2(to, 1f);
                    var uv = _burn.uvRect;
                    uv.x = Mathf.Repeat(uv.x + deltaSeconds * 0.35f, 1f);
                    _burn.uvRect = uv;
                }
            }
        }

        /// <summary>
        /// Diego's readout (D-027, R-042): knot ends and one channel for the liquid, drawn at its own
        /// proportions, as wide as the generated one; returns the channel.
        /// </summary>
        private RectTransform SheetFrame(ThemeRuntime theme, Vector2 drawn)
        {
            float k = 1f; // Diego's bar at its drawn size, 360 wide (R-048: the readout was too small)
            _group.sizeDelta = drawn * k;
            var frame = Ui.Fill(Ui.Child(_group, "Frame"));
            Frame.Background(frame, theme, "sprint_frame", "Sprint");
            var opening = Ui.Fill(Ui.Child(frame, "Opening"));
            Ui.Image(opening, theme.Sprite("sprint_frame_opening"), Color.white);
            opening.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            Ui.Image(Ui.Fill(Ui.Child(frame, "Art")), theme.Sprite("sprint_frame"), Color.white);

            var c = theme.Content("sprint_frame", new Vector4(40f, 10f, 40f, 33f)) * k;
            return Ui.Fill(Ui.Child(opening, "Track"), c.x, c.y, c.z, c.w);
        }

        /// <summary>The generated frame (no sheet art): 9-sliced, shrunk with the readout.</summary>
        private RectTransform GeneratedFrame(ThemeRuntime theme)
        {
            // The frame is drawn 36 high; this readout is smaller, so the whole frame (its
            // 9-slice ends and its content area) shrinks with it instead of being squashed.
            float k = Height / 36f;
            var frame = Ui.Fill(Ui.Child(_group, "Frame"));
            var frameImage = Ui.Image(frame, theme.Sprite("sprint_frame"), Color.white);
            frameImage.pixelsPerUnitMultiplier = 1f / k;
            var c = theme.Content("sprint_frame", new Vector4(24f, 12f, 24f, 12f)) * k;
            return Ui.Fill(Ui.Child(frame, "Track"), c.x, c.y, c.z, c.w);
        }

        public void Teardown()
        {
            if (_group != null) Object.Destroy(_group.gameObject);
            _group = null;
            _opacity = null;
            _trail = _fill = null;
            _liquid = null;
            _liquidMask = null;
            _burn = null;
            _burnRt = null;
            _bar = null;
        }
    }
}
