using System;
using GenesisUI.Theme;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// A clipped area whose content scrolls with the mouse wheel, with the thin gold scrollbar every
    /// window uses. Content is laid out by the caller in design units from its top (y downwards);
    /// the caller tells the content height.
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Widgets.WindowCanvas), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.ColorExtensions), typeof(GenesisUI.Foundation.GuardedBehaviour))]
    internal sealed class ScrollArea
    {
        public readonly RectTransform Viewport;
        public readonly RectTransform Content;
        private readonly RectTransform _track, _thumb;
        private readonly float _step;
        private float _offset, _height;

        public ScrollArea(RectTransform parent, string name, float x, float y, float width, float height, ThemeTokens t, float step = 60f)
        {
            _step = step;
            Viewport = WindowCanvas.At(parent, name, x, y, width - 10f, height);
            Viewport.gameObject.AddComponent<RectMask2D>();
            Ui.Image(Viewport, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            Viewport.gameObject.AddComponent<Wheel>().Init(Scroll);
            Content = WindowCanvas.At(Viewport, "Content", 0f, 0f, width - 10f, height);
            _track = WindowCanvas.At(parent, name + " Scroll", x + width - 4f, y, 3f, height);
            Ui.Image(_track, null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.25f));
            _thumb = WindowCanvas.At(_track, "Thumb", -1f, 0f, 5f, 40f);
            Ui.Image(_thumb, null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.85f));
            _track.gameObject.SetActive(false);
        }

        public float ContentHeight
        {
            get => _height;
            set
            {
                _height = value;
                Content.sizeDelta = new Vector2(Content.sizeDelta.x, Mathf.Max(value, Viewport.sizeDelta.y));
                Apply(_offset);
            }
        }

        public void ToTop() => Apply(0f);

        public void Scroll(float wheel) => Apply(_offset - Mathf.Sign(wheel) * (wheel == 0f ? 0f : _step));

        private void Apply(float offset)
        {
            float view = Viewport.sizeDelta.y;
            float max = Mathf.Max(0f, _height - view);
            _offset = Mathf.Clamp(offset, 0f, max);
            Content.anchoredPosition = new Vector2(0f, _offset);
            bool bar = max > 0.5f;
            if (_track.gameObject.activeSelf != bar) _track.gameObject.SetActive(bar);
            if (!bar) return;
            float thumb = Mathf.Max(24f, view * view / _height);
            _thumb.sizeDelta = new Vector2(_thumb.sizeDelta.x, thumb);
            _thumb.anchoredPosition = new Vector2(-1f, -(view - thumb) * _offset / max);
        }

        /// <summary>Mouse wheel anywhere over the viewport (events bubble up from its children).</summary>
        internal sealed class Wheel : GenesisUI.Foundation.GuardedBehaviour, IScrollHandler
        {
            private Action<float> _onScroll;
            internal Wheel Init(Action<float> onScroll) { _onScroll = onScroll; return this; }
            public void OnScroll(PointerEventData e) { if (_onScroll != null) GenesisUI.Foundation.Guard.Run(CallbackOwner, _onScroll, e.scrollDelta.y); }
        }
    }
}
