using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Notice
{
    /// <summary>
    /// Vanilla's messages in GenesisUI's look: the top-left notice (pickups, "sheltered", skill
    /// ups) as a card with its icon, and the centre message in the display font. It mirrors
    /// MessageHud's text, icon and fade (vanilla fades them through the canvas renderer alpha),
    /// so timing and queueing stay exactly vanilla's.
    /// </summary>
    [GameContract("assembly_valheim", "MessageHud", "instance")]
    [GameContract("assembly_valheim", "MessageHud", "m_messageText")]
    [GameContract("assembly_valheim", "MessageHud", "m_messageIcon")]
    [GameContract("assembly_valheim", "MessageHud", "m_messageCenterText")]
    internal sealed class NoticeModule : IUiModule
    {
        private const float CardHeight = 46f;
        private const float IconSize = 30f;

        private static readonly string[] OwnedRegions = { "hud.messages" };

        private readonly ConfigEntry<int> _offsetX;
        private readonly ConfigEntry<int> _offsetY;
        private RectTransform _card;
        private CanvasGroup _cardOpacity;
        private Image _icon;
        private TextMeshProUGUI _text;
        private TextMeshProUGUI _center;
        private Vector4 _content;
        private string _shownText;
        private string _shownCenter;
        private Vector2 _appliedOffset = new Vector2(float.NaN, float.NaN);

        public NoticeModule(ConfigFile config)
        {
            _offsetX = config.Bind("Notice", "OffsetX", 24,
                new ConfigDescription("Distância das notificações até a borda esquerda da tela.", new AcceptableValueRange<int>(0, 1800)));
            _offsetY = config.Bind("Notice", "OffsetY", 24,
                new ConfigDescription("Distância das notificações até o topo da tela.", new AcceptableValueRange<int>(0, 1000)));
        }

        public string Id => "hud.notice";
        public string NameToken => "$genesisui_module_notice";
        public IReadOnlyList<string> Regions => OwnedRegions;
        public float RefreshRate => 30f;

        public void Build(ModuleContext context)
        {
            var theme = context.Theme;
            var t = theme.Tokens;
            _card = Ui.Place(Ui.Child(context.Root, "Notice"), new Vector2(0f, 1f), Vector2.zero, new Vector2(240f, CardHeight));
            _cardOpacity = _card.gameObject.AddComponent<CanvasGroup>();
            _cardOpacity.blocksRaycasts = false;
            var sprite = theme.Sprite("card");
            Ui.Image(Ui.Fill(Ui.Child(_card, "Card")), sprite, sprite != null ? Color.white : ThemeRuntime.ToUnity(t.PanelBackground));
            _content = theme.Content("card", new Vector4(12f, 12f, 12f, 12f));
            _content = new Vector4(Mathf.Min(_content.x, 10f), 0f, Mathf.Min(_content.z, 10f), 0f);

            var iconRt = Ui.Place(Ui.Child(_card, "Icon"), new Vector2(0f, 0.5f), new Vector2(_content.x + 6f, 0f), new Vector2(IconSize, IconSize));
            iconRt.pivot = new Vector2(0f, 0.5f);
            _icon = Ui.Image(iconRt, null, Color.white);
            _icon.preserveAspect = true;

            _text = Ui.Text(_card, "Text", theme, FontRole.BodyStrong, 18f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.Left, outlined: true);

            _center = Ui.Text(context.Root, "CenterMessage", theme, FontRole.Display, 26f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.Center, outlined: true);
            Ui.Place(_center.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(900f, 80f));
            _center.textWrappingMode = TextWrappingModes.Normal;

            _shownText = _shownCenter = null;
            _appliedOffset = new Vector2(float.NaN, float.NaN);
            _card.gameObject.SetActive(false);
            _center.gameObject.SetActive(false);
        }

        public void Refresh(float deltaSeconds)
        {
            if (_card == null) return;
            var offset = new Vector2(_offsetX.Value, -_offsetY.Value);
            if (offset != _appliedOffset) { _card.anchoredPosition = offset; _appliedOffset = offset; }

            var hud = MessageHud.instance;
            if (hud == null)
            {
                if (_card.gameObject.activeSelf) _card.gameObject.SetActive(false);
                if (_center.gameObject.activeSelf) _center.gameObject.SetActive(false);
                return;
            }

            // Top-left notice.
            var src = hud.m_messageText;
            float alpha = src != null ? src.canvasRenderer.GetAlpha() : 0f;
            string text = src != null ? src.text : null;
            bool show = alpha > 0.01f && !string.IsNullOrEmpty(text);
            if (_card.gameObject.activeSelf != show) _card.gameObject.SetActive(show);
            if (show)
            {
                if (!Mathf.Approximately(_cardOpacity.alpha, alpha)) _cardOpacity.alpha = alpha;
                var iconSrc = hud.m_messageIcon;
                bool hasIcon = iconSrc != null && iconSrc.sprite != null && iconSrc.canvasRenderer.GetAlpha() > 0.01f;
                if (_icon.enabled != hasIcon) _icon.enabled = hasIcon;
                if (hasIcon && _icon.sprite != iconSrc.sprite) _icon.sprite = iconSrc.sprite;
                if (text != _shownText) Layout(text, hasIcon);
            }

            // Centre message.
            var center = hud.m_messageCenterText;
            float centerAlpha = center != null ? center.canvasRenderer.GetAlpha() : 0f;
            string centerText = center != null ? center.text : null;
            bool showCenter = centerAlpha > 0.01f && !string.IsNullOrEmpty(centerText);
            if (_center.gameObject.activeSelf != showCenter) _center.gameObject.SetActive(showCenter);
            if (showCenter)
            {
                if (centerText != _shownCenter) { _shownCenter = centerText; _center.text = centerText; }
                if (!Mathf.Approximately(_center.alpha, centerAlpha)) _center.alpha = centerAlpha;
            }
        }

        public void Teardown()
        {
            if (_card != null) Object.Destroy(_card.gameObject);
            if (_center != null) Object.Destroy(_center.gameObject);
            _card = null;
            _center = null;
        }

        private void Layout(string text, bool hasIcon)
        {
            _shownText = text;
            _text.text = text;
            float left = _content.x + 8f + (hasIcon ? IconSize + 8f : 0f);
            float width = Mathf.Min(520f, _text.GetPreferredValues(text, 480f, 0f).x) + left + _content.z + 12f;
            _card.sizeDelta = new Vector2(width, CardHeight);
            var rt = _text.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(left, 0f);
            rt.offsetMax = new Vector2(-(_content.z + 8f), 0f);
        }
    }
}
