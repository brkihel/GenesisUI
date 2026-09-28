using GenesisUI.HudModel;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Notice
{
    /// <summary>One top-left notice card: the card frame, an optional icon and the text, sized to it.</summary>
    internal sealed class NoticeCard
    {
        private const float IconSize = 30f;

        public NoticeStack.Notice Notice { get; private set; }
        public bool Used;

        private readonly RectTransform _rt;
        private readonly CanvasGroup _opacity;
        private readonly Image _icon;
        private readonly TextMeshProUGUI _text;
        private readonly Vector4 _content;
        private readonly float _height;
        private int _shownVersion = -1;
        private float _y;

        public NoticeCard(RectTransform parent, int index, ThemeRuntime theme, float height)
        {
            var t = theme.Tokens;
            _height = height;
            _rt = Ui.Place(Ui.Child(parent, "Notice" + index), new Vector2(0f, 1f), Vector2.zero, new Vector2(240f, height));
            _rt.pivot = new Vector2(0f, 1f);
            _opacity = _rt.gameObject.AddComponent<CanvasGroup>();
            _opacity.blocksRaycasts = false;
            _opacity.interactable = false;
            var sprite = theme.Sprite("card");
            Ui.Image(Ui.Fill(Ui.Child(_rt, "Card")), sprite, sprite != null ? Color.white : ThemeRuntime.ToUnity(t.PanelBackground));
            var c = theme.Content("card", new Vector4(12f, 12f, 12f, 12f));
            _content = new Vector4(Mathf.Min(c.x, 10f), 0f, Mathf.Min(c.z, 10f), 0f);

            var iconRt = Ui.Place(Ui.Child(_rt, "Icon"), new Vector2(0f, 0.5f), new Vector2(_content.x + 6f, 0f), new Vector2(IconSize, IconSize));
            iconRt.pivot = new Vector2(0f, 0.5f);
            _icon = Ui.Image(iconRt, null, Color.white);
            _icon.preserveAspect = true;

            _text = Ui.Text(_rt, "Text", theme, FontRole.BodyStrong, 18f, ThemeRuntime.ToUnity(t.TextTitle), TextAlignmentOptions.Left, outlined: true);
            _rt.gameObject.SetActive(false);
        }

        public void Bind(NoticeStack.Notice notice, float startY)
        {
            Notice = notice;
            _shownVersion = -1;
            _y = startY;
            _opacity.alpha = 0f;
            _rt.gameObject.SetActive(true);
        }

        public void Unbind()
        {
            Notice = null;
            if (_rt.gameObject.activeSelf) _rt.gameObject.SetActive(false);
        }

        public void Show(float targetY, float glide)
        {
            var n = Notice;
            if (n.Version != _shownVersion) Layout(n);
            _y += (targetY - _y) * glide;
            _rt.anchoredPosition = new Vector2(0f, _y);
            if (!Mathf.Approximately(_opacity.alpha, n.Alpha)) _opacity.alpha = n.Alpha;
        }

        private void Layout(NoticeStack.Notice n)
        {
            _shownVersion = n.Version;
            var icon = n.Icon as Sprite;
            bool hasIcon = icon != null;
            _icon.enabled = hasIcon;
            if (hasIcon) _icon.sprite = icon;
            _text.text = n.Text;
            float left = _content.x + 8f + (hasIcon ? IconSize + 8f : 0f);
            float width = Mathf.Min(520f, _text.GetPreferredValues(n.Text, 480f, 0f).x) + left + _content.z + 12f;
            _rt.sizeDelta = new Vector2(width, _height);
            var rt = _text.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(left, 0f);
            rt.offsetMax = new Vector2(-(_content.z + 8f), 0f);
        }
    }
}
