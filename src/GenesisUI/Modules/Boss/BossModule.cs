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

namespace GenesisUI.Modules.Boss
{
    /// <summary>
    /// The boss plate at the top centre (concept 6): name, level stars, health bar with the burn
    /// of what was just lost, and "current / max". Vanilla creates its boss HUD only when a boss
    /// shows up, so the module veils each new one as it appears (region hud.boss is dynamic).
    /// Regular creatures get their plates from hud.enemy.
    /// </summary>
    [GameContract("assembly_valheim", "EnemyHud", "instance")]
    [GameContract("assembly_valheim", "EnemyHud", "GetActiveBoss")]
    [GameContract("assembly_valheim", "Character", "GetHoverName")]
    [GameContract("assembly_valheim", "Character", "GetLevel")]
    [GameContract("assembly_valheim", "Character", "GetHealth")]
    [GameContract("assembly_valheim", "Character", "GetMaxHealth")]
    [GameContract("assembly_valheim", "Character", "IsDead")]
    internal sealed class BossModule : IUiModule
    {
        private const float Width = 520f;
        private const float Height = 74f;
        private const int MaxStars = 5;
        private const float VeilScanSeconds = 0.5f;

        private static readonly string[] OwnedRegions = { "hud.boss" };

        private readonly ConfigEntry<int> _offsetY;
        private readonly BarAnimator _bar = new BarAnimator();
        private readonly Image[] _stars = new Image[MaxStars];
        private RectTransform _group;
        private RectTransform _barArea;
        private Image _fill;
        private Image _trail;
        private RectTransform _liquidMask;
        private LiquidLayer _liquid;
        private TextMeshProUGUI _name;
        private TextMeshProUGUI _value;
        private Character _shownBoss;
        private int _shownHealth = int.MinValue;
        private int _shownMax = int.MinValue;
        private float _scan;
        private int _appliedY = int.MinValue;

        public BossModule(ConfigFile config)
        {
            _offsetY = config.Bind("Boss", "OffsetY", 12,
                new ConfigDescription("Distância da placa do chefe até o topo da tela.", new AcceptableValueRange<int>(0, 900)));
        }

        public string Id => "hud.boss";
        public string NameToken => "$genesisui_module_boss";
        public IReadOnlyList<string> Regions => OwnedRegions;
        public float RefreshRate => 30f;

        public void Build(ModuleContext context)
        {
            var theme = context.Theme;
            var t = theme.Tokens;
            _group = Ui.Place(Ui.Child(context.Root, "Boss"), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Width, Height));
            // Diego's plate at its drawn height; only its straight rails stretch to the width (D-027).
            Frame.Dress(Ui.Fill(Ui.Child(_group, "Plate")), theme, "boss_plate", "Boss", Height);
            var c = theme.Content("boss_plate", new Vector4(22f, 12f, 22f, 12f));
            var content = Ui.Fill(Ui.Child(_group, "Content"), c.x, c.y, c.z, c.w);

            _name = Ui.Fit(Ui.Text(content, "Name", theme, FontRole.Display, 20f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.Center, outlined: true), 13f);
            var nameRt = (RectTransform)_name.transform;
            nameRt.anchorMin = new Vector2(0f, 0.5f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(0f, 0f);
            nameRt.offsetMax = new Vector2(0f, 0f);

            var star = theme.Sprite("star");
            for (int i = 0; i < MaxStars; i++)
            {
                var srt = Ui.Place(Ui.Child(content, "Star" + i), new Vector2(0.5f, 1f), new Vector2((i - (MaxStars - 1) / 2f) * 15f, 9f), new Vector2(13f, 13f));
                srt.pivot = new Vector2(0.5f, 0.5f);
                _stars[i] = Ui.Image(srt, star, star != null ? Color.white : ThemeRuntime.ToUnity(t.AccentGold));
                _stars[i].enabled = false;
            }

            // Health: a horizontal liquid and the burn of what was just lost.
            _barArea = Ui.Child(content, "Bar");
            _barArea.anchorMin = new Vector2(0f, 0f);
            _barArea.anchorMax = new Vector2(1f, 0.45f);
            _barArea.offsetMin = new Vector2(6f, 2f);
            _barArea.offsetMax = new Vector2(-6f, -2f);
            Ui.Image(Ui.Fill(Ui.Child(_barArea, "Back")), null, new Color(0f, 0f, 0f, 0.55f));
            var health = ThemeRuntime.ToUnity(t.BarHealth);
            // What was just lost glows hot before it drains, like the vital bars' burn.
            _trail = Ui.Image(Ui.Fill(Ui.Child(_barArea, "Trail")), theme.Sprite("bar_fill"), new Color(1f, 0.62f, 0.26f, 0.85f));
            _trail.type = Image.Type.Filled;
            _trail.fillMethod = Image.FillMethod.Horizontal;
            _trail.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill = Ui.Image(Ui.Fill(Ui.Child(_barArea, "Fill")), theme.Sprite("bar_fill"), health);
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;

            // Diego's health liquid, drifting sideways, cut at the level by a mask (the plain fill
            // stays as the fallback when the texture is missing).
            _liquid = null;
            var liquid = theme.Texture("liquid_health_h");
            if (liquid != null)
            {
                _fill.enabled = false;
                _liquidMask = Ui.Child(_barArea, "Liquid");
                _liquidMask.anchorMin = Vector2.zero;
                _liquidMask.anchorMax = Vector2.one;
                _liquidMask.pivot = new Vector2(0f, 0.5f);
                _liquidMask.offsetMin = _liquidMask.offsetMax = Vector2.zero;
                _liquidMask.gameObject.AddComponent<RectMask2D>();
                float barWidth = Width - c.x - c.z - 12f, barHeight = (Height - c.y - c.w) * 0.45f - 4f;
                var layer = Ui.Place(Ui.Child(_liquidMask, "Flow"), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(barWidth, barHeight));
                layer.pivot = new Vector2(0f, 0.5f);
                _liquid = new LiquidLayer(layer, liquid, new Vector2(barWidth, barHeight), theme.Size("liquid_health_h"), 1.4f,
                    Color.white, new Vector2(0.01f, 0f), new Vector2(0f, 0.3f), mirror: false);
            }

            _value = Ui.Fit(Ui.Text(_barArea, "Value", theme, FontRole.Label, 12f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.Center, outlined: true), 9f);
            Ui.Fill((RectTransform)_value.transform);

            _shownBoss = null;
            _appliedY = int.MinValue;
            _group.gameObject.SetActive(false);
        }

        public void Refresh(float deltaSeconds)
        {
            if (_group == null) return;
            if (_offsetY.Value != _appliedY) { _group.anchoredPosition = new Vector2(0f, -_offsetY.Value); _appliedY = _offsetY.Value; }

            _scan -= deltaSeconds;
            if (_scan <= 0f)
            {
                _scan = VeilScanSeconds;
                foreach (var hud in RegionRegistry.BossHuds())
                    if (!VanillaVeil.IsVeiled(hud)) VanillaVeil.Apply("module:" + Id, "hud.boss/" + hud.name, hud);
            }

            var boss = EnemyHud.instance != null ? EnemyHud.instance.GetActiveBoss() : null;
            bool show = boss != null && !boss.IsDead();
            if (_group.gameObject.activeSelf != show) _group.gameObject.SetActive(show);
            if (!show) { _shownBoss = null; return; }

            if (boss != _shownBoss)
            {
                _shownBoss = boss;
                _bar.Update(boss.GetHealth(), boss.GetMaxHealth(), 0f);
                string name = boss.GetHoverName();
                _name.text = Localization.instance != null ? Localization.instance.Localize(name) : name;
                int stars = Mathf.Clamp(boss.GetLevel() - 1, 0, MaxStars);
                for (int i = 0; i < MaxStars; i++)
                {
                    _stars[i].enabled = i < stars;
                    _stars[i].rectTransform.anchoredPosition = new Vector2((i - (stars - 1) / 2f) * 15f, 9f);
                }
            }

            float health = boss.GetHealth(), max = boss.GetMaxHealth();
            _bar.Update(health, max, deltaSeconds);
            _fill.fillAmount = _bar.Fast;
            if (_liquid != null)
            {
                if (!Mathf.Approximately(_liquidMask.anchorMax.x, _bar.Fast)) _liquidMask.anchorMax = new Vector2(_bar.Fast, 1f);
                _liquid.Scroll(deltaSeconds);
            }

            _trail.fillAmount = _bar.Slow;

            int h = Mathf.CeilToInt(health), m = Mathf.CeilToInt(max);
            if (h != _shownHealth || m != _shownMax)
            {
                _shownHealth = h;
                _shownMax = m;
                _value.SetText("{0} / {1}", h, m);
            }
        }

        public void Teardown()
        {
            if (_group != null) Object.Destroy(_group.gameObject);
            _group = null;
            _liquid = null;
            _liquidMask = null;
            _shownBoss = null;
        }
    }
}
