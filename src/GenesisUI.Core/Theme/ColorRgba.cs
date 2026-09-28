using System;
using System.Globalization;

namespace GenesisUI.Theme
{
    /// <summary>A colour without Unity: the plugin converts it to UnityEngine.Color at the edge.</summary>
    public readonly struct ColorRgba : IEquatable<ColorRgba>
    {
        public readonly float R, G, B, A;

        public ColorRgba(float r, float g, float b, float a = 1f)
        {
            R = Clamp01(r); G = Clamp01(g); B = Clamp01(b); A = Clamp01(a);
        }

        /// <summary>"#RRGGBB" or "#RRGGBBAA".</summary>
        public static ColorRgba FromHex(string hex)
        {
            if (!TryFromHex(hex, out var c)) throw new FormatException("expected #RRGGBB or #RRGGBBAA, got '" + hex + "'");
            return c;
        }

        public static bool TryFromHex(string hex, out ColorRgba color)
        {
            color = default;
            if (hex == null || hex.Length < 7 || hex[0] != '#' || (hex.Length != 7 && hex.Length != 9)) return false;
            if (!byte.TryParse(hex.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r)) return false;
            if (!byte.TryParse(hex.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g)) return false;
            if (!byte.TryParse(hex.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b)) return false;
            byte a = 255;
            if (hex.Length == 9 && !byte.TryParse(hex.Substring(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a)) return false;
            color = new ColorRgba(r / 255f, g / 255f, b / 255f, a / 255f);
            return true;
        }

        public ColorRgba WithAlpha(float a) => new ColorRgba(R, G, B, a);

        public static ColorRgba Lerp(ColorRgba a, ColorRgba b, float t)
        {
            t = Clamp01(t);
            return new ColorRgba(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t, a.A + (b.A - a.A) * t);
        }

        public bool Equals(ColorRgba o) => R == o.R && G == o.G && B == o.B && A == o.A;
        public override bool Equals(object obj) => obj is ColorRgba o && Equals(o);
        public override int GetHashCode() => (R, G, B, A).GetHashCode();

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
