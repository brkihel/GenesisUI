using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Motion;
using GenesisUI.Vitals;
using GenesisUI.Widgets;
using UnityEngine;

namespace GenesisUI.Modules.Vitals
{
    /// <summary>
    /// Health, stamina and eitr as framed vertical bars at the bottom-left (concepts 4-6).
    /// Read-only: it samples the local player and draws; it never changes anything.
    /// </summary>
    [GameContract("assembly_valheim", "Player", "m_localPlayer")]
    [GameContract("assembly_valheim", "Character", "GetHealth")]
    [GameContract("assembly_valheim", "Character", "GetMaxHealth")]
    [GameContract("assembly_valheim", "Player", "GetStamina")]
    [GameContract("assembly_valheim", "Player", "GetMaxStamina")]
    [GameContract("assembly_valheim", "Player", "GetEitr")]
    [GameContract("assembly_valheim", "Player", "GetMaxEitr")]
    internal sealed class VitalsModule : IUiModule
    {
        private const float LowHealthFraction = 0.25f;

        private static readonly string[] OwnedRegions = { "hud.health", "hud.stamina", "hud.eitr", "hud.healthDecor" };

        private readonly ConfigEntry<int> _offsetX;
        private readonly ConfigEntry<int> _offsetY;
        private readonly ConfigEntry<float> _scale;
        private readonly ConfigEntry<bool> _showValues;

        private readonly BarAnimator _health = new BarAnimator();
        private readonly BarAnimator _stamina = new BarAnimator();
        private readonly BarAnimator _eitr = new BarAnimator();

        private RectTransform _group;
        private VitalBarView _healthView;
        private VitalBarView _staminaView;
        private VitalBarView _eitrView;
        private Vector2 _appliedOffset = new Vector2(float.NaN, float.NaN);
        private float _appliedScale = float.NaN;

        public VitalsModule(ConfigFile config)
        {
            _offsetX = config.Bind("Vitals", "OffsetX", 36,
                new ConfigDescription("Distância das barras até a borda esquerda da tela, em pontos de interface.", new AcceptableValueRange<int>(0, 1600)));
            _offsetY = config.Bind("Vitals", "OffsetY", 72,
                new ConfigDescription("Distância das barras até a borda de baixo da tela, em pontos de interface.", new AcceptableValueRange<int>(0, 900)));
            _scale = config.Bind("Vitals", "Scale", 1f,
                new ConfigDescription("Tamanho das barras (1 = padrão).", new AcceptableValueRange<float>(0.5f, 2f)));
            _showValues = config.Bind("Vitals", "ShowValues", true,
                "Mostra o número de cada barra numa plaquinha. Desligado, as barras ficam limpas, só com o líquido.");
        }

        public string Id => "hud.vitals";
        public string NameToken => "$genesisui_module_vitals";
        public IReadOnlyList<string> Regions => OwnedRegions;
        public float RefreshRate => 30f;

        public void Build(ModuleContext context)
        {
            var theme = context.Theme;
            _group = Ui.Place(Ui.Child(context.Root, "Vitals"), Vector2.zero, Vector2.zero, new Vector2(150f, 260f));

            // Health is the widest and tallest bar; stamina and eitr sit to its right.
            // Larger, translucent bubbles: calm for health, livelier for stamina, parallax for eitr.
            _healthView = new VitalBarView(_group, "Health", theme, theme.Tokens.BarHealth, new Vector2(0f, 22f), new Vector2(40f, 226f),
                new BarMotion { Speed = 0.035f, PatternAlpha = 0.07f, Hot = new Color(1f, 0.62f, 0.26f) });
            _staminaView = new VitalBarView(_group, "Stamina", theme, theme.Tokens.BarStamina, new Vector2(46f, 22f), new Vector2(34f, 196f),
                new BarMotion { Speed = 0.05f, PatternAlpha = 0.07f, Hot = new Color(1f, 0.93f, 0.62f) });
            _eitrView = new VitalBarView(_group, "Eitr", theme, theme.Tokens.BarEitr, new Vector2(84f, 22f), new Vector2(34f, 196f),
                new BarMotion { Speed = 0.04f, PatternAlpha = 0.08f, CounterSpeed = 0.025f, Hot = new Color(0.72f, 0.95f, 1f) });

            var medallion = theme.Sprite("medallion");
            if (medallion != null)
            {
                var m = Ui.Place(Ui.Child(_group, "Medallion"), Vector2.zero, new Vector2(-6f, 0f), new Vector2(58f, 58f));
                Ui.Image(m, medallion, Color.white);
            }

            ApplyLayout();
        }

        public void Refresh(float deltaSeconds)
        {
            if (_group == null) return;
            ApplyLayout();

            var player = Player.m_localPlayer;
            bool show = player != null;
            if (_group.gameObject.activeSelf != show) _group.gameObject.SetActive(show);
            if (!show) return;

            _health.Update(player.GetHealth(), player.GetMaxHealth(), deltaSeconds);
            _stamina.Update(player.GetStamina(), player.GetMaxStamina(), deltaSeconds);
            _eitr.Update(player.GetEitr(), player.GetMaxEitr(), deltaSeconds);

            float danger = _health.HasCapacity && _health.Fast > 0f && _health.Fast <= LowHealthFraction
                ? Pulse.Evaluate(Time.unscaledTimeAsDouble)
                : 0f;

            bool values = _showValues.Value;
            _healthView.ShowValue(values);
            _staminaView.ShowValue(values);
            _eitrView.ShowValue(values);
            _healthView.Apply(_health, danger, deltaSeconds);
            _staminaView.Apply(_stamina, 0f, deltaSeconds);
            _eitrView.SetVisible(_eitr.HasCapacity);
            if (_eitr.HasCapacity) _eitrView.Apply(_eitr, 0f, deltaSeconds);
        }

        public void Teardown()
        {
            if (_group != null) Object.Destroy(_group.gameObject);
            _group = null;
            _healthView = _staminaView = _eitrView = null;
            _appliedOffset = new Vector2(float.NaN, float.NaN);
            _appliedScale = float.NaN;
        }

        private void ApplyLayout()
        {
            var offset = new Vector2(_offsetX.Value, _offsetY.Value);
            if (offset != _appliedOffset)
            {
                _group.anchoredPosition = offset;
                _appliedOffset = offset;
            }
            if (_scale.Value != _appliedScale)
            {
                _group.localScale = Vector3.one * _scale.Value;
                _appliedScale = _scale.Value;
            }
        }
    }
}
