using System;

namespace GenesisUI.Geometry
{
    /// <summary>Fits a fixed upright box, rather than widening the frame for a changing equipment mesh.</summary>
    public static class PreviewFraming
    {
        public static float Distance(float width, float height, float depth, float pitch, float verticalFov, float aspect, float margin)
        {
            if (!(aspect > 0f) || !(verticalFov > 0f && verticalFov < 180f) || !(margin > 0f))
                throw new ArgumentOutOfRangeException(nameof(aspect));
            double radians = Math.PI / 180.0;
            double s = Math.Abs(Math.Sin(pitch * radians)), c = Math.Abs(Math.Cos(pitch * radians));
            double halfY = (height * c + depth * s) * 0.5 * margin;
            double halfZ = (depth * c + height * s) * 0.5 * margin;
            double tan = Math.Tan(verticalFov * 0.5 * radians);
            return (float)(Math.Max(halfY / tan, width * 0.5 * margin / (tan * aspect)) + halfZ);
        }
    }
}
