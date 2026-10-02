using GenesisUI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// A light that plays once and goes dark: the panels' reveal when a window opens (GenesisUI/Reveal,
    /// light running along the frame's lines), the shine over a recipe that has just become
    /// craftable (GenesisUI/Shine) and the ring around a new map pin (GenesisUI/Ring). Its graphic is disabled except while playing; unscaled time, so a
    /// paused game still plays it. Null from the factories when the effects are off or the shader is
    /// not loaded: nothing replaces them.
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Foundation.GuardedBehaviour))]
    internal sealed class OneShotLight : GenesisUI.Foundation.GuardedBehaviour
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int RectId = Shader.PropertyToID("_Rect");
        private static readonly int SizeId = Shader.PropertyToID("_Size");
        private static readonly Vector3[] Corners = new Vector3[4];

        private Graphic _graphic;
        private Material _material;
        private RectTransform _rt;
        private float _duration, _delay, _time = -1f;
        private bool _onEnable, _canvasRect;
        private Vector2 _size;

        /// <summary>While playing, the light stays centred on this (a map pin that moves with the map).</summary>
        public Transform Follow;

        /// <summary>
        /// Light along <paramref name="frame"/>'s lines, played each time the panel shows (its window
        /// opens). Panels further right start a little later, so the reveal reads left to right.
        /// </summary>
        public static OneShotLight Reveal(RectTransform panel, Image frame, ThemeRuntime theme)
        {
            if (frame == null || frame.sprite == null) return null;
            var material = theme.NewLightMaterial("GenesisUI/Reveal");
            if (material == null) return null;
            var rt = Ui.Fill(Ui.Child(panel, "Reveal"));
            rt.SetSiblingIndex(frame.transform.GetSiblingIndex() + 1); // right over the frame, under the content
            // Not Ui.Image: that one would hand the frame's sprite to the metal shader.
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = frame.sprite;
            image.type = frame.type;
            image.pixelsPerUnitMultiplier = frame.pixelsPerUnitMultiplier;
            image.raycastTarget = false;
            image.material = material;
            var light = Make(rt, image, material, 0.75f);
            light._onEnable = true;
            light._canvasRect = true;
            return light;
        }

        /// <summary>A band crossing <paramref name="piece"/> once, behind its text; <see cref="Play"/> starts it.</summary>
        public static OneShotLight Shine(RectTransform piece, int siblingIndex, ThemeRuntime theme, float inset = 3f)
        {
            var material = theme.NewLightMaterial("GenesisUI/Shine");
            if (material == null) return null;
            var rt = Ui.Fill(Ui.Child(piece, "Shine"), inset, inset, inset, inset);
            rt.SetSiblingIndex(siblingIndex);
            var image = rt.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.material = material;
            return Make(rt, image, material, 0.9f);
        }

        /// <summary>A ring spreading from a point; <see cref="Play"/> starts it, <see cref="Follow"/> places it.</summary>
        public static OneShotLight Ring(RectTransform parent, ThemeRuntime theme, float size)
        {
            var material = theme.NewLightMaterial("GenesisUI/Ring");
            if (material == null) return null;
            var rt = Ui.Child(parent, "Ring");
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            var image = rt.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.material = material;
            return Make(rt, image, material, 1.1f);
        }

        private static OneShotLight Make(RectTransform rt, Graphic graphic, Material material, float duration)
        {
            graphic.enabled = false;
            var light = rt.gameObject.AddComponent<OneShotLight>();
            light._graphic = graphic;
            light._material = material;
            light._rt = rt;
            light._duration = duration;
            return light;
        }

        public void Play(float delay = 0f)
        {
            _delay = delay;
            _time = 0f;
        }

        protected override void OnOwnerEnabled()
        {
            if (!_onEnable) return;
            // Left to right across the screen: up to a tenth of a second apart; after the window's own fade starts.
            float delay = 0.06f;
            var canvas = _graphic.canvas;
            if (canvas != null)
            {
                var root = (RectTransform)canvas.rootCanvas.transform;
                _rt.GetWorldCorners(Corners);
                float x = root.InverseTransformPoint((Corners[0] + Corners[2]) * 0.5f).x;
                float w = Mathf.Max(1f, root.rect.width);
                delay += 0.1f * Mathf.Clamp01(x / w + 0.5f);
            }
            Play(delay);
        }

        protected override void OnOwnerDisabled()
        {
            _time = -1f;
            if (_graphic != null) _graphic.enabled = false;
        }

        protected override void OnOwnerDestroyed()
        {
            if (_material != null) Destroy(_material);
        }

        protected override void OnOwnerUpdate()
        {
            if (_time < 0f) return;
            _time += Time.unscaledDeltaTime;
            float p = (_time - _delay) / _duration;
            if (p >= 1f)
            {
                _time = -1f;
                _graphic.enabled = false;
                return;
            }
            bool on = p >= 0f;
            if (_graphic.enabled != on) _graphic.enabled = on;
            if (!on) return;
            if (Follow != null) _rt.position = Follow.position;
            if (_canvasRect) SetCanvasRect();
            else
            {
                var size = _rt.rect.size;
                if (size != _size) { _size = size; _material.SetVector(SizeId, new Vector4(size.x, size.y, 0f, 0f)); }
            }
            // Ease out: quick start, settling at the end.
            float eased = 1f - (1f - p) * (1f - p) * (1f - p);
            _material.SetFloat(ProgressId, eased);
        }

        /// <summary>The piece in its canvas's space, where UI vertices reach the shader.</summary>
        private void SetCanvasRect()
        {
            var canvas = _graphic.canvas;
            if (canvas == null) return;
            var space = canvas.transform;
            _rt.GetWorldCorners(Corners);
            Vector2 min = space.InverseTransformPoint(Corners[0]);
            Vector2 max = space.InverseTransformPoint(Corners[2]);
            _material.SetVector(RectId, new Vector4(min.x, min.y, max.x, max.y));
        }
    }
}
