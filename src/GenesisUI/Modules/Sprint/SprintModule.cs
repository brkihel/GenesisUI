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
    /// <summary>A temporary stamina readout above the hotbar while the player runs.</summary>
    [GameContract("assembly_valheim", "Player", "m_localPlayer")]
    [GameContract("assembly_valheim", "Character", "IsRunning", Parameters = new string[0])]
    [GameContract("assembly_valheim", "Player", "GetStamina", Parameters = new string[0])]
    [GameContract("assembly_valheim", "Player", "GetMaxStamina", Parameters = new string[0])]
    internal sealed class SprintModule : IUiModule
    {
        private const float Width = 460f;
        private const float LingerSeconds = 1.5f;
        private static readonly string[] NoVanillaRegions = new string[0];

        private readonly ConfigEntry<int> _offsetY;
        private readonly ConfigEntry<float> _scale;
        private RectTransform _group;
        private CanvasGroup _opacity;
        private Image _trail;
        private Image _fill;
        private BarAnimator _bar;
        private float _linger;
        private int _appliedY = int.MinValue;
        private float _appliedScale = float.NaN;

        public SprintModule(ConfigFile config)
        {
            _offsetY = config.Bind("Sprint", "OffsetY", 120,
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
                Vector2.zero, new Vector2(Width, 58f));
            _opacity = _group.gameObject.AddComponent<CanvasGroup>();
            _opacity.alpha = 0f;
            _opacity.interactable = false;
            _opacity.blocksRaycasts = false;

            var frame = Ui.Place(Ui.Child(_group, "Frame"), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(Width, 36f));
            Ui.Image(frame, theme.Sprite("sprint_frame"), Color.white);

            var track = Ui.Place(Ui.Child(frame, "Track"), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(Width - 49f, 10f));
            var fillSprite = theme.Sprite("bar_fill");
            var stamina = ThemeRuntime.ToUnity(theme.Tokens.BarStamina);
            _trail = Ui.Image(Ui.Fill(Ui.Child(track, "Trail")), fillSprite,
                new Color(stamina.r, stamina.g, stamina.b, theme.Tokens.BarTrailAlpha));
            _trail.type = Image.Type.Filled;
            _trail.fillMethod = Image.FillMethod.Horizontal;
            _trail.fillAmount = 0f;
            _fill = Ui.Image(Ui.Fill(Ui.Child(track, "Fill")), fillSprite, stamina);
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillAmount = 0f;

            var label = Ui.Text(_group, "Label", theme, FontRole.Label, 14f,
                ThemeRuntime.ToUnity(theme.Tokens.AccentGoldBright), TextAlignmentOptions.Center, outlined: true);
            Ui.Place((RectTransform)label.transform, new Vector2(0.5f, 0f),
                new Vector2(0f, 35f), new Vector2(150f, 21f));
            label.text = Localization.instance != null
                ? Localization.instance.Localize("$genesisui_sprint_label") : "STAMINA";

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
            bool running = player != null && player.IsRunning();
            if (running) _linger = LingerSeconds;
            else _linger = Mathf.Max(0f, _linger - deltaSeconds);
            bool visible = player != null && _linger > 0f;
            if (_group.gameObject.activeSelf != visible) _group.gameObject.SetActive(visible);
            if (!visible) return;

            float alpha = Mathf.Clamp01(_linger / 0.45f);
            if (!Mathf.Approximately(_opacity.alpha, alpha)) _opacity.alpha = alpha;
            _bar.Update(player.GetStamina(), player.GetMaxStamina(), deltaSeconds);
            if (!Mathf.Approximately(_fill.fillAmount, _bar.Fast)) _fill.fillAmount = _bar.Fast;
            if (!Mathf.Approximately(_trail.fillAmount, _bar.Slow)) _trail.fillAmount = _bar.Slow;
        }

        public void Teardown()
        {
            if (_group != null) Object.Destroy(_group.gameObject);
            _group = null;
            _opacity = null;
            _trail = _fill = null;
            _bar = null;
        }
    }
}
