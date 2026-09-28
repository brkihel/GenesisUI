using System.Collections.Generic;
using BepInEx.Configuration;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.HudModel;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;

namespace GenesisUI.Modules.Notice
{
    /// <summary>
    /// Vanilla's messages in GenesisUI's look. The top-left notices (pickups, "sheltered", skill
    /// ups) become a short stack of cards with their icons (NoticeStack: up to three, newest on
    /// top, each fading while dropping down after a few seconds), because vanilla replaces its
    /// single line within a second when several arrive. New messages are detected from vanilla's
    /// own display (its text changes or its fade restarts), so vanilla still decides what is said
    /// and in which order. The centre message mirrors vanilla's text and fade in the display font.
    /// </summary>
    [GameContract("assembly_valheim", "MessageHud", "instance")]
    [GameContract("assembly_valheim", "MessageHud", "m_messageText")]
    [GameContract("assembly_valheim", "MessageHud", "m_messageIcon")]
    [GameContract("assembly_valheim", "MessageHud", "m_messageCenterText")]
    internal sealed class NoticeModule : IUiModule
    {
        private const float CardHeight = 46f;
        private const float Gap = 6f;
        private const float DropDistance = 22f;
        private const float SlideSpeed = 14f;   // how fast cards glide to their new slot (1/s)
        private const int Cards = 6;             // three staying plus the ones still leaving

        private static readonly string[] OwnedRegions = { "hud.messages" };

        private readonly ConfigEntry<int> _offsetX;
        private readonly ConfigEntry<int> _offsetY;
        private readonly NoticeStack _stack = new NoticeStack();
        private readonly NoticeCard[] _cards = new NoticeCard[Cards];
        private readonly Dictionary<NoticeStack.Notice, NoticeCard> _bound = new Dictionary<NoticeStack.Notice, NoticeCard>();
        private RectTransform _column;
        private TextMeshProUGUI _center;
        private string _shownCenter;
        private string _lastVanillaText;
        private float _lastVanillaAlpha;
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
        public float RefreshRate => 0f; // the stack glides every frame

        public void Build(ModuleContext context)
        {
            var theme = context.Theme;
            var t = theme.Tokens;
            _column = Ui.Place(Ui.Child(context.Root, "Notices"), new Vector2(0f, 1f), Vector2.zero, new Vector2(560f, 400f));
            _column.pivot = new Vector2(0f, 1f);
            for (int i = 0; i < Cards; i++) _cards[i] = new NoticeCard(_column, i, theme, CardHeight);

            _center = Ui.Text(context.Root, "CenterMessage", theme, FontRole.Display, 26f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.Center, outlined: true);
            Ui.Place(_center.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(900f, 80f));
            _center.textWrappingMode = TextWrappingModes.Normal;

            _stack.Clear();
            _bound.Clear();
            _shownCenter = null;
            // Whatever vanilla shows at build time is old news: only what changes from now on is new.
            var hud = MessageHud.instance;
            _lastVanillaText = hud != null && hud.m_messageText != null ? hud.m_messageText.text : null;
            _lastVanillaAlpha = hud != null && hud.m_messageText != null ? hud.m_messageText.canvasRenderer.GetAlpha() : 0f;
            _appliedOffset = new Vector2(float.NaN, float.NaN);
            _center.gameObject.SetActive(false);
        }

        public void Refresh(float deltaSeconds)
        {
            if (_column == null) return;
            var offset = new Vector2(_offsetX.Value, -_offsetY.Value);
            if (offset != _appliedOffset) { _column.anchoredPosition = offset; _appliedOffset = offset; }

            var hud = MessageHud.instance;
            if (hud == null)
            {
                _stack.Clear();
                _bound.Clear();
                for (int i = 0; i < Cards; i++) _cards[i].Unbind();
                if (_center.gameObject.activeSelf) _center.gameObject.SetActive(false);
                return;
            }

            // A new top-left message: vanilla changed the text, or restarted its fade for a repeat.
            var src = hud.m_messageText;
            if (src != null)
            {
                string text = src.text;
                float alpha = src.canvasRenderer.GetAlpha();
                bool changed = !string.Equals(text, _lastVanillaText) || alpha > _lastVanillaAlpha + 0.2f;
                if (changed && alpha > 0.5f && !string.IsNullOrEmpty(text))
                {
                    var iconSrc = hud.m_messageIcon;
                    Sprite icon = iconSrc != null && iconSrc.canvasRenderer.GetAlpha() > 0.01f ? iconSrc.sprite : null;
                    _stack.Push(text, icon);
                }
                if (alpha > 0.5f || !string.Equals(text, _lastVanillaText)) _lastVanillaText = text;
                _lastVanillaAlpha = alpha;
            }

            _stack.Tick(deltaSeconds);
            LayoutStack(deltaSeconds);

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
            for (int i = 0; i < Cards; i++) _cards[i] = null;
            _bound.Clear();
            if (_column != null) Object.Destroy(_column.gameObject);
            if (_center != null) Object.Destroy(_center.gameObject);
            _column = null;
            _center = null;
            _stack.Clear();
        }

        /// <summary>
        /// Each notice keeps its card while it lives, so a card glides from its old slot to its new
        /// one when a newer notice pushes it down; leaving ones drop as they fade.
        /// </summary>
        private void LayoutStack(float dt)
        {
            for (int c = 0; c < Cards; c++) _cards[c].Used = false;
            var items = _stack.Items;
            float y = 0f;
            float glide = 1f - Mathf.Exp(-SlideSpeed * Mathf.Min(dt, 0.1f));
            for (int i = 0; i < items.Count; i++)
            {
                var n = items[i];
                float target = -y - n.Drop * DropDistance;
                y += CardHeight + Gap;
                if (!_bound.TryGetValue(n, out var card))
                {
                    card = FreeCard();
                    if (card == null) continue;       // more leaving notices than cards: skip the extra
                    _bound.Add(n, card);
                    card.Bind(n, target + 12f);      // enters from slightly above its slot
                }
                card.Used = true;
                card.Show(target, glide);
            }
            for (int c = 0; c < Cards; c++)
            {
                var card = _cards[c];
                if (card.Used || card.Notice == null) continue;
                _bound.Remove(card.Notice);
                card.Unbind();
            }
        }

        private NoticeCard FreeCard()
        {
            for (int c = 0; c < Cards; c++) if (_cards[c].Notice == null) return _cards[c];
            return null;
        }
    }
}
