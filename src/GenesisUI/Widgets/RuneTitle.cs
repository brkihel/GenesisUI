using GenesisUI.Theme;
using TMPro;
using UnityEngine;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// A window title that is written in runes first (D-036): each time its panel shows, the letters
    /// appear as Elder Futhark (Noto Sans Runic, a fallback font) and turn into the Latin letters one
    /// after another, left to right with a little randomness, in under half a second. If anything
    /// else changes the text meanwhile, that text wins and the effect stops.
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Foundation.GuardedBehaviour))]
    internal sealed class RuneTitle : GenesisUI.Foundation.GuardedBehaviour
    {
        private const float Duration = 0.45f;

        private TextMeshProUGUI _label;
        private string _final, _written;
        private char[] _buffer;
        private float _time = -1f, _delay;

        /// <summary>Adds the effect to a title; nothing when the runes cannot be drawn or the effects are off.</summary>
        public static void Attach(TextMeshProUGUI label, ThemeRuntime theme, float delay = 0.05f)
        {
            if (label == null || theme == null || !theme.RunesAvailable || !theme.LightsEnabled) return;
            var title = label.gameObject.AddComponent<RuneTitle>();
            title._label = label;
            title._delay = delay;
        }

        protected override void OnOwnerEnabled()
        {
            if (_label == null) return;
            _final = _label.text;
            if (string.IsNullOrEmpty(_final)) { _time = -1f; return; }
            if (_buffer == null || _buffer.Length != _final.Length) _buffer = new char[_final.Length];
            _time = 0f;
            Write(0f);
        }

        protected override void OnOwnerDisabled()
        {
            if (_time >= 0f && _label != null && _label.text == _written) _label.text = _final;
            _time = -1f;
        }

        protected override void OnOwnerUpdate()
        {
            if (_time < 0f) return;
            if (_label.text != _written) { _time = -1f; return; } // someone else wrote a new title: it wins
            _time += Time.unscaledDeltaTime;
            float p = (_time - _delay) / Duration;
            if (p >= 1f)
            {
                _label.text = _final;
                _time = -1f;
                return;
            }
            Write(Mathf.Max(0f, p));
        }

        private void Write(float p)
        {
            int n = _final.Length;
            for (int i = 0; i < n; i++)
            {
                char c = _final[i];
                // Left to right, each letter a little early or late.
                float at = (i + 0.5f) / n * 0.7f + Jitter(i) * 0.3f;
                _buffer[i] = p < at ? Rune(c) : c;
            }
            _written = new string(_buffer);
            _label.text = _written;
        }

        private static float Jitter(int i) => Mathf.Repeat(Mathf.Sin(i * 12.9898f) * 43758.5453f, 1f);

        /// <summary>The Elder Futhark rune closest to a Latin letter; anything else stays as it is.</summary>
        internal static char Rune(char c) => GenesisUI.Text.RuneText.Rune(c);
    }
}
