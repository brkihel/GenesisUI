using UnityEngine;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// A HUD piece that moves and fades to where its module wants it, smoothly (D-037): modules set a
    /// target position and opacity at their own refresh rate; this eases there every frame (unscaled,
    /// critically damped feel, about a quarter of a second). The first target is taken at once, so a
    /// piece never slides in from the corner when the HUD is built. No work once settled.
    /// </summary>
    internal sealed class Glide : MonoBehaviour
    {
        private const float Rate = 9f;

        private RectTransform _rt;
        private CanvasGroup _group;
        private Vector2 _target;
        private float _alpha = 1f, _targetAlpha = 1f;
        private bool _placed;

        public static Glide On(RectTransform rt)
        {
            var g = rt.gameObject.GetComponent<Glide>();
            if (g != null) return g;
            g = rt.gameObject.AddComponent<Glide>();
            g._rt = rt;
            g._group = rt.gameObject.GetComponent<CanvasGroup>();
            if (g._group == null) g._group = rt.gameObject.AddComponent<CanvasGroup>();
            g._group.blocksRaycasts = false;
            g._group.interactable = false;
            return g;
        }

        /// <summary>Where the piece goes (anchored position) and how visible it ends (0..1).</summary>
        public void To(Vector2 position, float alpha = 1f)
        {
            _target = position;
            _targetAlpha = alpha;
            if (_placed) return;
            _placed = true;
            _rt.anchoredPosition = position;
            _alpha = alpha;
            _group.alpha = alpha;
        }

        public bool Shown => _alpha > 0.01f || _targetAlpha > 0f;

        private void Update()
        {
            if (!_placed) return;
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * Rate);
            var p = _rt.anchoredPosition;
            if ((p - _target).sqrMagnitude > 0.0025f) _rt.anchoredPosition = Vector2.Lerp(p, _target, k);
            else if (p != _target) _rt.anchoredPosition = _target;
            if (!Mathf.Approximately(_alpha, _targetAlpha))
            {
                _alpha = Mathf.Abs(_alpha - _targetAlpha) < 0.01f ? _targetAlpha : Mathf.Lerp(_alpha, _targetAlpha, k);
                _group.alpha = _alpha;
            }
        }
    }

    /// <summary>
    /// Where the vital bars end, in the HUD root's units (bottom-left origin), published by the vitals
    /// module for the pieces laid out beside them (food and potion columns, quick-use and action slots).
    /// Right moves smoothly when the eitr bar appears or leaves. Valid only while the vitals show.
    /// </summary>
    internal static class HudAnchor
    {
        internal static bool Valid;
        internal static float Right, Bottom, Top, Scale = 1f;

        /// <summary>Right edge of the effect columns (or of the bars when there are none): where the slots begin.</summary>
        internal static float EffectsRight;
        internal static bool EffectsValid;

        internal static float SlotsLeft => EffectsValid ? EffectsRight : Valid ? Right : 172f;
        internal static float SlotsBottom => Valid ? Bottom : 90f;
    }
}
