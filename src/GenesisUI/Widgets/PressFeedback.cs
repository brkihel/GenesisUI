using GenesisUI.Theme;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// Hover and press feedback for GenesisUI's buttons (Diego, R-060: they had none). A soft gold light
    /// rises over the piece while the pointer is on it and brightens while it is pressed, and the piece
    /// sinks a little under the press. A disabled button gives none. Eased with unscaled time; no work
    /// once settled.
    /// </summary>
    internal sealed class PressFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float HoverLight = 0.10f, PressLight = 0.2f, PressScale = 0.965f, Speed = 14f;

        private static Sprite _soft;

        private Selectable _button;
        private Image _light;
        private RectTransform _rt;
        private Vector3 _scale = Vector3.one;
        private float _glow, _sink;
        private bool _over, _down;

        /// <summary>Adds the light (inset into the frame) and the feedback to a button's piece.</summary>
        public static void Attach(RectTransform rt, Selectable button, ThemeRuntime theme, float inset = 3f)
        {
            var light = Ui.Fill(Ui.Child(rt, "Light"), inset, inset, inset, inset);
            var img = Ui.Image(light, Soft(), ThemeRuntime.ToUnity(theme.Tokens.AccentGoldBright));
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            img.canvasRenderer.SetAlpha(0f);
            var fb = rt.gameObject.AddComponent<PressFeedback>();
            fb._button = button;
            fb._light = img;
            fb._rt = rt;
        }

        public void OnPointerEnter(PointerEventData e) => _over = true;
        public void OnPointerExit(PointerEventData e) { _over = false; _down = false; }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left) _down = true;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left) _down = false;
        }

        private void OnDisable()
        {
            _over = _down = false;
            _glow = _sink = 0f;
            if (_light != null) _light.canvasRenderer.SetAlpha(0f);
            if (_rt != null) _rt.localScale = _scale;
        }

        private void Update()
        {
            bool live = _button == null || _button.interactable;
            float glow = !live ? 0f : _down ? PressLight : _over ? HoverLight : 0f;
            float sink = live && _down && _over ? 1f : 0f;
            if (glow == _glow && sink == _sink) return;
            float step = Time.unscaledDeltaTime * Speed;
            if (_sink == 0f && sink > 0f) _scale = _rt.localScale; // the piece's own scale, kept
            _glow = Mathf.MoveTowards(_glow, glow, step * PressLight);
            _sink = Mathf.MoveTowards(_sink, sink, step);
            _light.canvasRenderer.SetAlpha(_glow);
            _rt.localScale = _scale * Mathf.Lerp(1f, PressScale, _sink);
        }

        /// <summary>A white rounded square fading to nothing at its edges (sliced, so the fade stays thin).</summary>
        private static Sprite Soft()
        {
            if (_soft != null) return _soft;
            const int size = 48, edge = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "GenesisUI.SoftLight",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, edge - Mathf.Min(x + 0.5f, size - x - 0.5f));
                    float dy = Mathf.Max(0f, edge - Mathf.Min(y + 0.5f, size - y - 0.5f));
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / edge);
                    float a = 1f - Mathf.SmoothStep(0f, 1f, d);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            _soft = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(edge, edge, edge, edge));
            _soft.name = "GenesisUI.SoftLight";
            return _soft;
        }
    }
}
