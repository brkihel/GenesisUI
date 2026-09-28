using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Modules.Minimap
{
    /// <summary>
    /// A round RawImage using the source map material directly. A UI Mask would ask Unity
    /// for a stencil material copy, leaving Valheim's later texture and fog updates behind.
    /// </summary>
    internal sealed class CircularMapImage : RawImage
    {
        private const int Segments = 96;

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var rect = GetPixelAdjustedRect();
            var uv = uvRect;
            float cx = rect.center.x;
            float cy = rect.center.y;
            float rx = rect.width * 0.5f;
            float ry = rect.height * 0.5f;
            var tint = (Color32)color;
            vertices.AddVert(new Vector3(cx, cy), tint, uv.center);

            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * (Mathf.PI * 2f / Segments);
                float x = Mathf.Cos(angle);
                float y = Mathf.Sin(angle);
                vertices.AddVert(new Vector3(cx + x * rx, cy + y * ry), tint,
                    new Vector2(uv.center.x + x * uv.width * 0.5f, uv.center.y + y * uv.height * 0.5f));
                if (i > 0) vertices.AddTriangle(0, i + 1, i);
            }
        }
    }
}
