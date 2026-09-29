using GenesisUI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// The loss of a resource bar drawn as light by the GenesisUI/Burn shader (D-033): a white-hot core
    /// at the level, a halo that leaks past the frame, a cooling tail towards the delayed value and a
    /// few rising embers. One quad over the bar's channel plus a margin, placed outside the frame's
    /// masks so the halo is not clipped. Null from <see cref="Create"/> when the shader is not loaded:
    /// callers keep their previous effect.
    /// </summary>
    internal sealed class BurnLight
    {
        private const float Pad = 18f;
        private static readonly int EdgeId = Shader.PropertyToID("_Edge");
        private static readonly int TrailId = Shader.PropertyToID("_Trail");
        private static readonly int HeatId = Shader.PropertyToID("_Heat");
        private static readonly int SizeId = Shader.PropertyToID("_Size");
        private static readonly int PadId = Shader.PropertyToID("_Pad");
        private static readonly int VerticalId = Shader.PropertyToID("_Vertical");

        private readonly RawImage _image;
        private readonly Material _material;
        private float _edge = -1f, _trail = -1f, _heat = -1f;

        private BurnLight(RawImage image, Material material)
        {
            _image = image;
            _material = material;
        }

        /// <param name="host">Where the quad lives: an ancestor of <paramref name="channel"/> outside its masks.</param>
        /// <param name="channel">The bar's liquid area; the quad covers it plus a margin.</param>
        public static BurnLight Create(ThemeRuntime theme, RectTransform host, RectTransform channel, bool vertical)
        {
            var material = theme.NewBurnMaterial();
            if (material == null) return null;
            var corners = new Vector3[4];
            channel.GetWorldCorners(corners);
            Vector2 min = host.InverseTransformPoint(corners[0]);
            Vector2 max = host.InverseTransformPoint(corners[2]);
            var size = max - min;
            var rt = Ui.Child(host, "BurnLight");
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            // host's local space: its pivot is the origin.
            rt.anchoredPosition = (min + max) / 2f + host.rect.size * (host.pivot - new Vector2(0.5f, 0.5f));
            rt.sizeDelta = size + new Vector2(2f * Pad, 2f * Pad);
            var image = rt.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.material = material;
            material.SetFloat(PadId, Pad);
            material.SetFloat(VerticalId, vertical ? 1f : 0f);
            material.SetVector(SizeId, vertical ? new Vector4(size.y, size.x, 0f, 0f) : new Vector4(size.x, size.y, 0f, 0f));
            material.SetFloat(HeatId, 0f);
            image.enabled = false;
            return new BurnLight(image, material);
        }

        /// <summary>Level and delayed level (0..1), and how fresh the loss is (0 hides the light).</summary>
        public void Set(float edge, float trail, float heat)
        {
            bool on = heat > 0.001f;
            if (_image.enabled != on) _image.enabled = on;
            if (!on) return;
            if (!Mathf.Approximately(edge, _edge)) { _edge = edge; _material.SetFloat(EdgeId, edge); }
            if (!Mathf.Approximately(trail, _trail)) { _trail = trail; _material.SetFloat(TrailId, trail); }
            if (!Mathf.Approximately(heat, _heat)) { _heat = heat; _material.SetFloat(HeatId, heat); }
        }

        public void Destroy()
        {
            if (_material != null) Object.Destroy(_material);
        }
    }
}
