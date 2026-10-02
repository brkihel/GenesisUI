using GenesisUI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// The window tabs' light, drawn by the GenesisUI/Beam shader behind the tab's icon and label.
    /// Two states, two lights (Diego, 2026-10-02: one beam for both read as a duplicate, and a beam
    /// following the pointer did not look right):
    /// - the selected tab keeps a still shaft of light from the top rail, which the shader sways
    ///   slowly like a hanging lantern ("you are here");
    /// - the hovered tab gets a glint running once along its rail, a faint line of light while the
    ///   pointer stays, and its icon and label brighten and rise a little ("you can go here").
    /// One quad per tab, disabled while dark. Null from <see cref="Attach"/> when the shader is not
    /// loaded: the caller keeps <see cref="PressFeedback"/>.
    /// </summary>
    internal sealed class TabBeam : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float SelectedIntensity = 0.45f, HoverGlint = 0.7f, PressGlint = 1.2f;
        private const float BeamSpeed = 2.5f, HoverSpeed = 6f, GlintSeconds = 0.45f, Lift = 2f;
        private static readonly int SizeId = Shader.PropertyToID("_Size");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int HoverId = Shader.PropertyToID("_Hover");
        private static readonly int GlintId = Shader.PropertyToID("_GlintPos");
        private static readonly int PhaseId = Shader.PropertyToID("_Phase");

        private Selectable _button;
        private RectTransform _cell;
        private RawImage _image;
        private Material _material;
        private Vector2 _size;
        private float _beam, _hover, _glint = 1.2f;
        private float _shownBeam = -1f, _shownHover = -1f, _shownGlint = -1f, _shownLook = -1f;
        private bool _over, _down, _selected, _lookDirty = true;

        private Graphic _icon, _label;
        private RectTransform _iconRt, _labelRt;
        private Vector2 _iconBase, _labelBase;
        private Color _iconOff, _iconOn, _labelOff, _labelOn;

        /// <summary>Adds the light behind <paramref name="cell"/>'s content; null when the shader is unavailable.</summary>
        public static TabBeam Attach(RectTransform cell, Selectable button, ThemeRuntime theme)
        {
            var material = theme.NewLightMaterial("GenesisUI/Beam");
            if (material == null) return null;
            var rt = Ui.Fill(Ui.Child(cell, "Beam"));
            rt.SetAsFirstSibling(); // behind the icon and the label
            var image = rt.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.material = material;
            image.enabled = false;
            var beam = cell.gameObject.AddComponent<TabBeam>();
            beam._button = button;
            beam._cell = cell;
            beam._image = image;
            beam._material = material;
            // Each tab sways on its own: neighbours never move in step.
            material.SetFloat(PhaseId, cell.GetSiblingIndex() * 7.3f);
            return beam;
        }

        /// <summary>
        /// The tab's icon and label with their colours off and on: the light then owns them (selected:
        /// on; hovered: towards on, and lifted by <see cref="Lift"/>).
        /// </summary>
        public void Bind(Graphic icon, Color iconOff, Color iconOn, TextMeshProUGUI label, Color labelOff, Color labelOn)
        {
            _icon = icon;
            _label = label;
            _iconOff = iconOff; _iconOn = iconOn;
            _labelOff = labelOff; _labelOn = labelOn;
            if (icon != null) { _iconRt = icon.rectTransform; _iconBase = _iconRt.anchoredPosition; }
            if (label != null) { _labelRt = label.rectTransform; _labelBase = _labelRt.anchoredPosition; }
            _lookDirty = true;
        }

        public void SetSelected(bool selected)
        {
            if (_selected == selected) return;
            _selected = selected;
            _lookDirty = true;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            _over = true;
            if (_selected) return;
            _glint = 0f; // the glint runs along the rail once per entry
            UiSound.Play(UiSound.Cue.Hover);
        }

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
            _hover = 0f;
            _glint = 1.2f;
            _beam = _selected ? SelectedIntensity : 0f; // reopening: the selected tab is lit at once
            _shownBeam = _shownHover = _shownGlint = -1f;
            _lookDirty = true;
            if (_image != null) _image.enabled = false;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        private void Update()
        {
            bool live = _button == null || _button.interactable;
            float dt = Time.unscaledDeltaTime;
            float beam = live && _selected ? SelectedIntensity : 0f;
            float hover = !live || !_over || _selected ? 0f : _down ? PressGlint : HoverGlint;
            _beam = Mathf.MoveTowards(_beam, beam, dt * BeamSpeed);
            _hover = Mathf.MoveTowards(_hover, hover, dt * HoverSpeed);
            if (_glint < 1.2f) _glint = Mathf.Min(1.2f, _glint + dt * 1.2f / GlintSeconds);

            UpdateLook();
            bool on = _beam > 0.001f || _hover > 0.001f;
            if (_image.enabled != on) _image.enabled = on;
            if (!on) return;
            var size = _cell.rect.size;
            if (size != _size) { _size = size; _material.SetVector(SizeId, new Vector4(size.x, size.y, 0f, 0f)); }
            if (!Mathf.Approximately(_beam, _shownBeam)) { _shownBeam = _beam; _material.SetFloat(IntensityId, _beam); }
            if (!Mathf.Approximately(_hover, _shownHover)) { _shownHover = _hover; _material.SetFloat(HoverId, _hover); }
            if (!Mathf.Approximately(_glint, _shownGlint)) { _shownGlint = _glint; _material.SetFloat(GlintId, _glint); }
        }

        /// <summary>Icon and label: on when selected; towards on and lifted while hovered.</summary>
        private void UpdateLook()
        {
            float look = _selected ? 1f : Mathf.Clamp01(_hover / HoverGlint);
            if (!_lookDirty && Mathf.Approximately(look, _shownLook)) return;
            _lookDirty = false;
            _shownLook = look;
            float lift = _selected ? 0f : look * Lift;
            if (_icon != null)
            {
                _icon.color = Color.Lerp(_iconOff, _iconOn, look);
                _iconRt.anchoredPosition = _iconBase + new Vector2(0f, lift);
            }
            if (_label != null)
            {
                _label.color = Color.Lerp(_labelOff, _labelOn, _selected ? 1f : look * 0.8f);
                _labelRt.anchoredPosition = _labelBase + new Vector2(0f, lift);
            }
        }
    }
}
