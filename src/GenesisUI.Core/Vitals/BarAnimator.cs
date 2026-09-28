using System;

namespace GenesisUI.Vitals
{
    /// <summary>
    /// Everything a vital bar shows, computed from game values only.
    /// Fast fraction follows the value at once. The slow "trail" shows what was just
    /// lost: it holds for a moment, then drains toward the value, as the vanilla bar
    /// does. Gains move both at once, so healing never shows a trail.
    /// </summary>
    public sealed class BarAnimator
    {
        public const float TrailHoldSeconds = 0.45f;
        public const float TrailDrainPerSecond = 0.6f;

        private float _hold;

        public float Fast { get; private set; }
        public float Slow { get; private set; }

        /// <summary>The number shown on the bar: rounded up, like vanilla (1.2 health shows 2, never 1).</summary>
        public int Display { get; private set; }

        /// <summary>True when Display changed on the last Update; the view only rewrites text then.</summary>
        public bool DisplayChanged { get; private set; }

        public bool HasCapacity { get; private set; }

        public void Update(float current, float max, float deltaSeconds)
        {
            if (float.IsNaN(current) || float.IsInfinity(current)) current = 0f;
            if (float.IsNaN(max) || float.IsInfinity(max)) max = 0f;
            if (deltaSeconds < 0f || float.IsNaN(deltaSeconds)) deltaSeconds = 0f;

            HasCapacity = max > 0.001f;
            float target = HasCapacity ? Clamp01(current / max) : 0f;

            if (target >= Slow)
            {
                Slow = target;
                _hold = 0f;
            }
            else if (target < Fast)
            {
                _hold = TrailHoldSeconds; // new loss: restart the hold
            }

            Fast = target;

            if (Slow > Fast)
            {
                if (_hold > 0f) _hold -= deltaSeconds;
                else Slow = Math.Max(Fast, Slow - TrailDrainPerSecond * deltaSeconds);
            }

            int display = current <= 0f ? 0 : (int)Math.Ceiling(current - 1e-4f);
            DisplayChanged = display != Display;
            Display = display;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
