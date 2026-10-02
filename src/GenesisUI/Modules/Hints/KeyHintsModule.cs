using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Hints
{
    /// <summary>
    /// Vanilla's key hints (attack, block, build, fishing...) in GenesisUI's look and at half size
    /// (Diego, after R-058: they spanned the whole screen at 1920x1080 in vanilla's style). Vanilla
    /// keeps deciding which hints show; this module only restyles them reversibly — our fonts, our
    /// thin key caps — and scales the whole block down anchored at its bottom-right corner, together
    /// with the lift above the hotbar ([Hotbar] KeyHintsLift) that the hotbar module applied before.
    /// </summary>
    [GameContract("assembly_valheim", "KeyHints", "get_instance")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.VanillaSkin), typeof(GenesisUI.Host.ModuleContext), typeof(GenesisUI.Foundation.VanillaNudge), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Widgets.Frame), typeof(GenesisUI.Foundation.GenesisLog))]
    internal sealed class KeyHintsModule : IUiModule
    {
        private const string Owner = "module:hud.keyhints";
        private static readonly string[] NoRegions = new string[0];

        private readonly ConfigFile _config;
        private readonly ConfigEntry<float> _scale;
        private ConfigEntry<int> _lift;
        private readonly VanillaSkin _skin = new VanillaSkin(Owner);
        private ThemeRuntime _theme;
        private RectTransform _target;
        private int _styledCount = -1;
        private float _appliedScale = -1f;
        private int _appliedLift = int.MinValue;
        private float _rescanIn;

        /// <summary>While on, this module owns the key hints' position (the hotbar module leaves it).</summary>
        internal static bool Active { get; private set; }

        public KeyHintsModule(ConfigFile config)
        {
            _config = config;
            _scale = config.Bind("KeyHints", "Scale", 0.55f, new ConfigDescription(
                "Tamanho das dicas de atalho (Atacar, Bloquear...). 0,55 ocupa menos da metade da tela em 1920x1080.",
                new AcceptableValueRange<float>(0.3f, 1f)));
        }

        public string Id => "hud.keyhints";
        public string NameToken => "$genesisui_module_keyhints";
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 4f;

        public void Build(ModuleContext context)
        {
            _theme = context.Theme;
            _config.TryGetEntry("Hotbar", "KeyHintsLift", out _lift);
            _target = null;
            _styledCount = -1;
            _appliedScale = -1f;
            _appliedLift = int.MinValue;
            Active = true;
        }

        public void Refresh(float deltaSeconds)
        {
            var hints = global::KeyHints.instance;
            var rt = hints != null ? hints.transform as RectTransform : null;
            if (rt == null) return;
            if (rt != _target)
            {
                _skin.Restore();
                VanillaNudge.RestoreAll(Owner);
                _target = rt;
                _styledCount = -1;
                _appliedScale = -1f;
            }
            _rescanIn -= deltaSeconds;
            if (_styledCount < 0 || _rescanIn <= 0f)
            {
                _rescanIn = 2f;
                Restyle(rt);
            }
            Place(rt);
        }

        public void Teardown()
        {
            Active = false;
            VanillaNudge.RestoreAll(Owner);
            _skin.Restore();
            _target = null;
        }

        /// <summary>Scale about the bottom-right corner: the nudge carries the lift plus the corner's shift.</summary>
        private void Place(RectTransform rt)
        {
            float s = _scale.Value;
            int lift = _lift != null ? _lift.Value : 0;
            if (Mathf.Approximately(s, _appliedScale) && lift == _appliedLift) return;
            _appliedScale = s;
            _appliedLift = lift;
            _skin.Rect(rt);
            rt.localScale = new Vector3(s, s, 1f);
            var size = rt.rect.size;
            var corner = new Vector2((1f - rt.pivot.x) * size.x, -rt.pivot.y * size.y);
            var shift = corner * (1f - s);
            VanillaNudge.RestoreAll(Owner);
            VanillaNudge.Apply(Owner, "hud.keyHints", rt, shift + new Vector2(0f, lift));
        }

        /// <summary>Our fonts on every hint text; our thin key cap behind every key label.</summary>
        private void Restyle(RectTransform rt)
        {
            var texts = rt.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length == _styledCount) return;
            _styledCount = texts.Length;
            var t = _theme.Tokens;
            var body = _theme.Font(FontRole.Body);
            var label = _theme.Font(FontRole.Label);
            var cap = _theme.Sprite("keycap_wide");
            foreach (var text in texts)
            {
                if (text == null) continue;
                // A key label sits on a sliced background image (its key cap); everything else is a caption.
                var parentImage = text.transform.parent != null ? text.transform.parent.GetComponent<Image>() : null;
                bool key = parentImage != null && parentImage.type == Image.Type.Sliced;
                _skin.Text(text);
                var font = key ? label : body;
                if (font != null && text.font != font)
                {
                    text.font = font;
                    var outlined = _theme.OutlinedMaterial(key ? FontRole.Label : FontRole.Body);
                    if (outlined != null) text.fontSharedMaterial = outlined;
                }
                text.color = ThemeRuntime.ToUnity(key ? t.AccentGoldBright : t.TextTitle);
                if (key && cap != null)
                {
                    _skin.Image(parentImage);
                    parentImage.sprite = cap;
                    parentImage.type = Image.Type.Sliced;
                    parentImage.color = Color.white;
                    parentImage.pixelsPerUnitMultiplier = Frame.CanvasScale(parentImage.transform) * _theme.Size("keycap_wide").y / Mathf.Max(8f, parentImage.rectTransform.rect.height);
                }
            }
            GenesisLog.Info(Owner, "key hints restyled: " + texts.Length + " text(s)");
        }
    }
}
