using System;

namespace GenesisUI.Motion
{
    /// <summary>Soft attention pulse (docs/ART-DIRECTION.md §5: at most 1.5 s per cycle).</summary>
    public static class Pulse
    {
        public const float DefaultPeriodSeconds = 1.4f;

        /// <returns>0..1, starting at 0, smooth (cosine), period clamped to [0.2, 1.5] s.</returns>
        public static float Evaluate(double timeSeconds, float periodSeconds = DefaultPeriodSeconds)
        {
            if (periodSeconds < 0.2f) periodSeconds = 0.2f;
            if (periodSeconds > 1.5f) periodSeconds = 1.5f;
            double phase = timeSeconds % periodSeconds / periodSeconds;
            return (float)(0.5 - 0.5 * Math.Cos(phase * 2 * Math.PI));
        }
    }
}
