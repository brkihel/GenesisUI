using GenesisUI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// A soft light around a piece's border, drawn by GenesisUI/Edge: the slot an item is dragged
    /// over, the craft button's progress, a food or effect about to run out. One quad, laid over its
    /// target (plus a margin for the halo) every frame it is lit, so it can follow a target that
    /// moves or changes; disabled while dark. Null from <see cref="Create"/> when the effects are off
    /// or the shader is not loaded.
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Foundation.GuardedBehaviour))]
    [DefaultExecutionOrder(31010)]
    internal sealed class EdgeLight : GenesisUI.Foundation.GuardedBehaviour
    {
        private static readonly int RectId = Shader.PropertyToID("_Rect");
        private static readonly int QuadSizeId = Shader.PropertyToID("_QuadSize");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");
        private static readonly int OrbitId = Shader.PropertyToID("_Orbit");
        private static readonly int PhaseId = Shader.PropertyToID("_Phase");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Vector3[] Corners = new Vector3[4];

        private RawImage _image;
        private Material _material;
        private RectTransform _rt, _parent;
        private float _margin, _shown = -1f, _shownProgress = -1f, _shownPulse = -1f, _current;
        private float _shownPhase = -1f;
        private bool _shownOrbit;

        /// <summary>The piece to light (null: none). Its border is followed every lit frame.</summary>
        public RectTransform Target;
        /// <summary>How bright, 0..~1.5; eased towards at <see cref="Speed"/> per second.</summary>
        public float Intensity;
        public float Speed = 6f;
        /// <summary>The share of the border lit, clockwise from the top centre (1: all of it).</summary>
        public float Progress = 1f;
        /// <summary>Breathing amount (0..1).</summary>
        public float Pulse;
        /// <summary>Use a moving border segment; Phase follows native action progress.</summary>
        public bool Orbit;
        public float Phase;

        /// <param name="parent">Where the quad lives; drawn over its earlier siblings.</param>
        public static EdgeLight Create(RectTransform parent, ThemeRuntime theme, float margin = 12f)
        {
            var material = theme.NewLightMaterial("GenesisUI/Edge");
            if (material == null) return null;
            var rt = Ui.Child(parent, "EdgeLight");
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var image = rt.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.material = material;
            image.enabled = false;
            var light = rt.gameObject.AddComponent<EdgeLight>();
            light._image = image;
            light._material = material;
            light._rt = rt;
            light._parent = parent;
            light._margin = margin;
            return light;
        }

        public void SetColor(Color color) => _material.SetColor(ColorId, color);

        public void SetFloat(string name, float value) => _material.SetFloat(name, value);

        public void HideImmediately()
        {
            Target = null; Intensity = _current = 0f;
            if (_image != null) _image.enabled = false;
        }

        protected override void OnOwnerDisabled()
        {
            _current = 0f;
            if (_image != null) _image.enabled = false;
        }

        protected override void OnOwnerDestroyed()
        {
            if (_material != null) Destroy(_material);
        }

        protected override void OnOwnerLateUpdate()
        {
            float target = Target != null && Target.gameObject.activeInHierarchy ? Intensity : 0f;
            if (target == 0f && _current == 0f) return;
            _current = Mathf.MoveTowards(_current, target, Time.unscaledDeltaTime * Speed);
            bool on = _current > 0.001f && Target != null;
            if (_image.enabled != on) _image.enabled = on;
            if (!on) return;

            // Over the target, plus the halo's margin, in the parent's space.
            Target.GetWorldCorners(Corners);
            Vector2 min = _parent.InverseTransformPoint(Corners[0]);
            Vector2 max = _parent.InverseTransformPoint(Corners[2]);
            _rt.localPosition = (min + max) * 0.5f;
            _rt.sizeDelta = max - min + new Vector2(2f * _margin, 2f * _margin);
            // UV-derived design units are stable under Canvas batching and uniform board scaling.
            var half = (max - min) * 0.5f;
            _material.SetVector(RectId, new Vector4(-half.x, -half.y, half.x, half.y));
            _material.SetVector(QuadSizeId, new Vector4(_rt.sizeDelta.x, _rt.sizeDelta.y, 0f, 0f));
            if (!Mathf.Approximately(_current, _shown)) { _shown = _current; _material.SetFloat(IntensityId, _current); }
            if (!Mathf.Approximately(Progress, _shownProgress)) { _shownProgress = Progress; _material.SetFloat(ProgressId, Progress); }
            if (!Mathf.Approximately(Pulse, _shownPulse)) { _shownPulse = Pulse; _material.SetFloat(PulseId, Pulse); }
            if (Orbit != _shownOrbit) { _shownOrbit = Orbit; _material.SetFloat(OrbitId, Orbit ? 1f : 0f); }
            if (!Mathf.Approximately(Phase, _shownPhase)) { _shownPhase = Phase; _material.SetFloat(PhaseId, Phase); }
        }
    }
}
