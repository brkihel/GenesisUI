using GenesisUI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// Embers rising from the bottom of an area, drawn by GenesisUI/Embers: the forge behind the
    /// crafting details, a load near the carry limit over the weight bar. Eased in and out; disabled
    /// while dark. Null from <see cref="Create"/> when the effects are off or the shader is not loaded.
    /// </summary>
    internal sealed class EmberField : MonoBehaviour
    {
        private static readonly int SizeId = Shader.PropertyToID("_Size");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int FillId = Shader.PropertyToID("_Fill");

        private RawImage _image;
        private Material _material;
        private RectTransform _rt;
        private Vector2 _size;
        private float _current, _shown = -1f, _shownFill = -1f;

        /// <summary>How bright (0 hides it); eased towards at <see cref="Speed"/> per second.</summary>
        public float Intensity;
        public float Speed = 1.5f;
        /// <summary>The share of the width the embers rise from, left to right.</summary>
        public float Fill = 1f;

        /// <summary>Fills <paramref name="area"/> (placed by the caller); the embers rise from its bottom.</summary>
        public static EmberField Create(RectTransform area, ThemeRuntime theme)
        {
            var material = theme.NewLightMaterial("GenesisUI/Embers");
            if (material == null) return null;
            var rt = Ui.Fill(Ui.Child(area, "Embers"));
            var image = rt.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.material = material;
            image.enabled = false;
            var field = rt.gameObject.AddComponent<EmberField>();
            field._image = image;
            field._material = material;
            field._rt = rt;
            return field;
        }

        public void SetFloat(string name, float value) => _material.SetFloat(name, value);

        public void SetColors(Color hot, Color cool)
        {
            _material.SetColor("_Hot", hot);
            _material.SetColor("_Cool", cool);
        }

        private void OnDisable()
        {
            _current = 0f;
            if (_image != null) _image.enabled = false;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        private void Update()
        {
            if (Intensity == 0f && _current == 0f) return;
            _current = Mathf.MoveTowards(_current, Intensity, Time.unscaledDeltaTime * Speed);
            bool on = _current > 0.001f;
            if (_image.enabled != on) _image.enabled = on;
            if (!on) return;
            var size = _rt.rect.size;
            if (size != _size) { _size = size; _material.SetVector(SizeId, new Vector4(size.x, size.y, 0f, 0f)); }
            if (!Mathf.Approximately(_current, _shown)) { _shown = _current; _material.SetFloat(IntensityId, _current); }
            if (!Mathf.Approximately(Fill, _shownFill)) { _shownFill = Fill; _material.SetFloat(FillId, Fill); }
        }
    }
}
