using System;

namespace GenesisUI.Layout
{
    /// <summary>Uniform scaling with a final viewport bound, including extended inventory boards.</summary>
    public static class InterfaceScale
    {
        public static float Multiplier(float value, float maximum = 2f) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 1f : Math.Max(0.5f, Math.Min(maximum, value));

        public static float Window(float screenWidth, float screenHeight, float boardWidth, float boardHeight,
            float widthFraction, float heightFraction, float general, float windows)
        {
            if (!(screenWidth > 0f) || !(screenHeight > 0f) || !(boardWidth > 0f) || !(boardHeight > 0f)) return 1f;
            widthFraction = Fraction(widthFraction, .45f);
            heightFraction = Fraction(heightFraction, .5f);
            float fit = Math.Min(screenWidth * widthFraction / boardWidth, screenHeight * heightFraction / boardHeight);
            // Leave room for the board's subtle parallax when a multiplier reaches the screen limit.
            float limit = .98f * Math.Min(screenWidth / boardWidth, screenHeight / boardHeight);
            return Math.Min(fit * Multiplier(general, 1.5f) * Multiplier(windows), limit);
        }

        private static float Fraction(float value, float minimum) =>
            float.IsNaN(value) || float.IsInfinity(value) ? .75f : Math.Max(minimum, Math.Min(1f, value));
    }
}
