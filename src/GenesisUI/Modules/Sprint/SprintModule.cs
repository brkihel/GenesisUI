using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation;
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
    [GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Player")]
    [GameContract("assembly_valheim", "Player", "GetStamina", Parameters = new string[0])]
    [GameContract("assembly_valheim", "Player", "GetMaxStamina", Parameters = new string[0])]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Character", "GetMaxStamina", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Host.ModuleContext), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Widgets.LiquidLayer), typeof(GenesisUI.Widgets.BurnLight), typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.Vitals.BarAnimator), typeof(GenesisUI.Widgets.Frame))]
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
        private BurnLight _burnLight;
        private RawImage _burn;
        private RectTransform _glowRt;
        private Image _glow;
        private readonly RectTransform[] _sparkRt = new RectTransform[5];
        private readonly Image[] _spark = new Image[5];
        private Material _additive;
        private float _burnClock;
        private LiquidLayer _liquid;
        private BarAnimator _bar;
        private float _linger;
        private float _displayed;
        private float _burnTime;
        private bool _initialized;
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

            // The burn as light (D-033), outside the frame's mask so the halo leaks past it. Without the
            // shader: the earlier leading line and sparks.
            _burnLight = BurnLight.Create(theme, _group, track, vertical: false);
            var burnTex = _burnLight == null ? theme.Texture("bar_burn_h") : null;
            if (burnTex != null)
            {
                _burnRt = Ui.Child(track, "Burn");
                _burnRt.anchorMin = new Vector2(0f, 0f);
                _burnRt.anchorMax = new Vector2(0f, 1f);
                _burnRt.offsetMin = _burnRt.offsetMax = Vector2.zero;
                _burn = _burnRt.gameObject.AddComponent<RawImage>();
                _burn.texture = burnTex;
                _burn.color = Color.white;
                _burn.raycastTarget = false;
                _burn.uvRect = new Rect(0f, 0f, 1f, 1f);

                // The core always uses UI's ordinary shader (reliable through the frame mask).
                // If the game's additive particle shader is available, the halo and pooled
                // embers add light to the liquid. Alpha-blended copies remain the fallback.
                var shader = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Particles/Additive");
                if (shader != null && shader.isSupported)
                {
                    _additive = new Material(shader) { name = "GenesisUI stamina burn glow", hideFlags = HideFlags.DontSave };
                    GenesisLog.Info("Module:hud.sprint", "additive burn glow shader available");
                }
                else GenesisLog.Warn("Module:hud.sprint", "additive shader unavailable; using alpha glow");

                var ember = theme.Sprite("ember");
                _glowRt = Ui.Place(Ui.Child(track, "BurnGlow"), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(26f, 20f));
                _glowRt.pivot = new Vector2(0.5f, 0.5f);
                _glow = Ui.Image(_glowRt, ember, new Color(1f, 0.49f, 0.09f, 0f));
                if (_additive != null) _glow.material = _additive;
                for (int i = 0; i < _spark.Length; i++)
                {
                    _sparkRt[i] = Ui.Place(Ui.Child(track, "Spark" + i), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3f, 3f));
                    _sparkRt[i].pivot = new Vector2(0.5f, 0.5f);
                    _spark[i] = Ui.Image(_sparkRt[i], ember, Color.clear);
                    if (_additive != null) _spark[i].material = _additive;
                }
            }

            _bar = new BarAnimator();
            _linger = 0f;
            _initialized = false;
            _burnTime = 0f;
            _burnClock = 0f;
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
            if (!visible)
            {
                _initialized = false;
                return;
            }

            float alpha = Mathf.Clamp01(_linger / FadeSeconds);
            if (!Mathf.Approximately(_opacity.alpha, alpha)) _opacity.alpha = alpha;
            _bar.Update(stamina, max, deltaSeconds);
            float target = _bar.Fast;
            if (!_initialized)
            {
                _displayed = target;
                _initialized = true;
            }
            else
            {
                if (target < _displayed - 0.004f) _burnTime = 0.25f;
                float speed = target < _displayed ? 14f : 8f;
                _displayed = Mathf.Lerp(_displayed, target, 1f - Mathf.Exp(-speed * Mathf.Max(0f, deltaSeconds)));
                if (Mathf.Abs(_displayed - target) < 0.001f) _displayed = target;
            }
            _burnTime = Mathf.Max(0f, _burnTime - deltaSeconds);
            if (!Mathf.Approximately(_fill.fillAmount, _displayed)) _fill.fillAmount = _displayed;
            if (_liquid != null)
            {
                if (!Mathf.Approximately(_liquidMask.anchorMax.x, _displayed)) _liquidMask.anchorMax = new Vector2(_displayed, 1f);
                _liquid.Scroll(deltaSeconds);
            }

            float trail = Mathf.Min(_bar.Slow, _displayed + 0.04f);
            if (!Mathf.Approximately(_trail.fillAmount, trail)) _trail.fillAmount = trail;
            if (_burnLight != null)
                _burnLight.Set(_displayed, Mathf.Max(_displayed, _bar.Slow), Mathf.Clamp01(_burnTime / 0.25f));
            if (_burn != null)
            {
                float to = Mathf.Min(1f, _displayed + 0.04f);
                bool burning = _burnTime > 0f && to > _displayed + 0.002f;
                if (_burn.enabled != burning) _burn.enabled = burning;
                if (_glow.enabled != burning) _glow.enabled = burning;
                if (burning)
                {
                    _burnClock += deltaSeconds;
                    _burnRt.anchorMin = new Vector2(_displayed, 0f);
                    _burnRt.anchorMax = new Vector2(to, 1f);
                    float heat = Mathf.Clamp01(_burnTime / 0.25f);
                    float pulse = 0.8f + 0.2f * Mathf.Sin(_burnClock * 38f);
                    _burn.color = new Color(1f, 0.79f, 0.37f, heat * pulse);
                    var uv = _burn.uvRect;
                    uv.y = Mathf.Repeat(uv.y + deltaSeconds * 0.7f, 1f);
                    _burn.uvRect = uv;

                    _glowRt.anchorMin = _glowRt.anchorMax = new Vector2(_displayed, 0.5f);
                    _glow.color = new Color(1f, 0.42f, 0.06f, heat * (0.52f + 0.2f * pulse));
                    for (int i = 0; i < _spark.Length; i++)
                    {
                        float phase = Mathf.Repeat(_burnClock * (2.9f + i * 0.13f) + i * 0.21f, 1f);
                        float size = 1.8f + (1f - phase) * (i % 2 == 0 ? 2.6f : 1.4f);
                        _sparkRt[i].anchorMin = _sparkRt[i].anchorMax = new Vector2(_displayed, 0.5f);
                        _sparkRt[i].anchoredPosition = new Vector2(2f + phase * 19f,
                            Mathf.Sin(i * 2.4f + phase * 5f) * (2f + phase * 5f));
                        _sparkRt[i].sizeDelta = new Vector2(size, size);
                        _spark[i].color = new Color(1f, 0.68f + (1f - phase) * 0.3f, 0.18f,
                            heat * (1f - phase) * (i % 2 == 0 ? 0.95f : 0.7f));
                    }
                }
                for (int i = 0; i < _spark.Length; i++)
                    if (_spark[i] != null && _spark[i].enabled != burning) _spark[i].enabled = burning;
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
            if (_additive != null) Object.Destroy(_additive);
            if (_burnLight != null) _burnLight.Destroy();
            _burnLight = null;
            _group = null;
            _opacity = null;
            _trail = _fill = null;
            _liquid = null;
            _liquidMask = null;
            _burn = null;
            _burnRt = null;
            _glowRt = null;
            _glow = null;
            _additive = null;
            for (int i = 0; i < _spark.Length; i++) { _spark[i] = null; _sparkRt[i] = null; }
            _bar = null;
        }
    }
}
