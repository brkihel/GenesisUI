using System;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using UnityEngine;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// GenesisUI's own interface sounds (D-036): synthesized at start-up from a few sine partials and a
    /// little noise (no audio file, nothing from the game or another mod), soft and metallic like the
    /// gold. Played through the game's GUI mixer group, so the game's volume settings apply. Set in the
    /// <c>[Sound]</c> section (the Som category of the settings window): all on/off, the volume, and each
    /// sound on its own; changes apply at once.
    /// </summary>
    [GameContract("assembly_valheim", "AudioMan", "get_instance")]
    [GameContract("assembly_valheim", "AudioMan", "m_guiMixer")]
    internal static class UiSound
    {
        internal enum Cue { Hover, Tab, Open, Craft }

        private const int Rate = 44100;
        private const float HoverGap = 0.07f;

        /// <summary>Set from the config (and kept in step with it).</summary>
        internal static bool Enabled = true;
        internal static float Volume = 0.6f;
        /// <summary>Each sound on its own, by <see cref="Cue"/>.</summary>
        internal static readonly bool[] CueEnabled = { true, true, true, true };

        private static readonly System.Reflection.MethodInfo SetData =
            typeof(AudioClip).GetMethod("SetData", new[] { typeof(float[]), typeof(int) });

        private static AudioSource _source;
        private static AudioClip[] _clips;
        private static float _lastHover = -1f;

        public static void Play(Cue cue)
        {
            if (!Enabled || Volume <= 0f || !CueEnabled[(int)cue]) return;
            Guard.Try("ui sound", () => PlayNow(cue));
        }

        private static void PlayNow(Cue cue)
        {
            if (cue == Cue.Hover)
            {
                if (Time.unscaledTime - _lastHover < HoverGap) return; // sweeping over the tabs: not a rattle
                _lastHover = Time.unscaledTime;
            }
            if (_source == null) Build();
            if (_source.outputAudioMixerGroup == null && AudioMan.instance != null) _source.outputAudioMixerGroup = AudioMan.instance.m_guiMixer;
            _source.PlayOneShot(_clips[(int)cue], Volume);
        }

        private static void Build()
        {
            var go = new GameObject("GenesisUI.Sound");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = true; // the Esc menu pauses the listener; its buttons still answer
            var random = new System.Random(1931);
            _clips = new AudioClip[4];
            // A tiny, high tink: the pointer over a tab.
            _clips[(int)Cue.Hover] = Clip("GenesisUI hover", 0.22f, random, 0.16f, 0.002f, 0f,
                (3100f, 1f, 0.06f), (4630f, 0.5f, 0.04f), (6870f, 0.25f, 0.025f));
            // A small clink: a tab chosen.
            _clips[(int)Cue.Tab] = Clip("GenesisUI tab", 0.4f, random, 0.22f, 0.001f, 0.15f,
                (1650f, 1f, 0.12f), (2477f, 0.6f, 0.09f), (3900f, 0.35f, 0.06f), (5600f, 0.2f, 0.04f));
            // A soft metal bloom with a breath of air: a window opening.
            _clips[(int)Cue.Open] = Open(random);
            // Two light hammer strikes on metal: an item made.
            _clips[(int)Cue.Craft] = Craft(random);
        }

        private static AudioClip Clip(string name, float seconds, System.Random random, float amp, float attack, float noise,
                                      params (float Freq, float Amp, float Decay)[] partials)
        {
            var data = new float[Mathf.CeilToInt(seconds * Rate)];
            Strike(data, 0, random, amp, attack, noise, partials);
            return Make(name, data);
        }

        private static AudioClip Open(System.Random random)
        {
            var data = new float[Mathf.CeilToInt(0.9f * Rate)];
            // Air: low-passed noise, swelling then fading.
            float lp = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.12f) * Mathf.Exp(-Mathf.Max(0f, t - 0.12f) / 0.18f);
                float n = (float)(random.NextDouble() * 2.0 - 1.0);
                lp += (n - lp) * 0.06f;
                data[i] += lp * env * 0.35f;
            }
            Strike(data, 0, random, 0.16f, 0.015f, 0f, (520f, 1f, 0.5f), (1046f, 0.55f, 0.35f), (1580f, 0.3f, 0.25f), (2650f, 0.15f, 0.15f));
            return Make("GenesisUI open", data);
        }

        private static AudioClip Craft(System.Random random)
        {
            var data = new float[Mathf.CeilToInt(0.75f * Rate)];
            var partials = new[] { (880f, 1f, 0.35f), (1410f, 0.6f, 0.25f), (2350f, 0.4f, 0.18f), (3920f, 0.25f, 0.1f), (5100f, 0.15f, 0.06f) };
            Strike(data, 0, random, 0.24f, 0.001f, 0.25f, partials);
            Strike(data, Mathf.RoundToInt(0.13f * Rate), random, 0.13f, 0.001f, 0.2f, partials);
            return Make("GenesisUI craft", data);
        }

        /// <summary>Adds one metallic strike: decaying sine partials (slightly detuned pairs shimmer) and a click of noise.</summary>
        private static void Strike(float[] data, int start, System.Random random, float amp, float attack, float noise,
                                   params (float Freq, float Amp, float Decay)[] partials)
        {
            for (int i = start; i < data.Length; i++)
            {
                float t = (i - start) / (float)Rate;
                float env = attack > 0f ? Mathf.Clamp01(t / attack) : 1f;
                float s = 0f;
                foreach (var p in partials)
                {
                    float decay = Mathf.Exp(-t / p.Decay);
                    if (decay < 0.0005f) continue;
                    s += p.Amp * decay * (Mathf.Sin(2f * Mathf.PI * p.Freq * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * p.Freq * 1.004f * t) * 0.3f);
                }
                if (noise > 0f && t < 0.006f) s += noise * (float)(random.NextDouble() * 2.0 - 1.0) * (1f - t / 0.006f);
                data[i] += s * env * amp;
            }
        }

        private static AudioClip Make(string name, float[] data)
        {
            // A short fade at the end: never a click when the clip stops.
            int fade = Math.Min(data.Length, Rate / 100);
            for (int i = 0; i < fade; i++) data[data.Length - 1 - i] *= i / (float)fade;
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            // SetData(float[], int) by reflection: the compiler cannot see its Span overload's type on net48.
            SetData.Invoke(clip, new object[] { data, 0 });
            return clip;
        }
    }
}
