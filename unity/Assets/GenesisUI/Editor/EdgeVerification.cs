using System;
using System.IO;
using UnityEngine;

namespace GenesisUI
{
    /// <summary>Exercise the bundled distance field: bright border, clear interior for short buttons too.</summary>
    public static class EdgeVerification
    {
        public static void Verify(AssetBundle bundle)
        {
            Shader shader = null;
            foreach (var candidate in bundle.LoadAllAssets<Shader>())
                if (candidate.name == "GenesisUI/Edge") shader = candidate;
            if (shader == null || !shader.isSupported) throw new Exception("Edge shader missing/unsupported");
            var previous = RenderTexture.active;
            var material = new Material(shader);
            RenderTexture target = null;
            Texture2D pixels = null;
            try
            {
                material.SetColor("_Color", new Color(1f, 0.82f, 0.52f, 1f));
                material.SetFloat("_Intensity", 0.55f);
                material.SetFloat("_Inset", 2f);
                material.SetFloat("_Radius", 4f);
                material.SetFloat("_Halo", 1.5f);
                material.SetFloat("_Line", 0.9f);
                material.SetFloat("_Progress", 1f);
                material.SetFloat("_Pulse", 0f);
                foreach (int height in new[] { 44, 64, 120 })
                {
                    const int width = 360, margin = 10;
                    int quadWidth = width + margin * 2, quadHeight = height + margin * 2;
                    material.SetVector("_Rect", new Vector4(-width / 2f, -height / 2f, width / 2f, height / 2f));
                    material.SetVector("_QuadSize", new Vector4(quadWidth, quadHeight, 0f, 0f));
                    target = new RenderTexture(quadWidth * 2, quadHeight * 2, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                    target.Create();
                    RenderTexture.active = target;
                    GL.Clear(false, true, Color.clear);
                    Graphics.Blit(Texture2D.whiteTexture, target, material);
                    pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, true);
                    pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    pixels.Apply();
                    var center = pixels.GetPixel(target.width / 2, target.height / 2);
                    var border = pixels.GetPixel(target.width / 2, margin * 2 + 4);
                    if (center.maxColorComponent > 0.01f && Mathf.Max(center.r, center.g, center.b) > 0.01f)
                        throw new Exception("Edge lights the button interior at height " + height);
                    if (border.r < 0.15f) throw new Exception("Edge border not lit at height " + height);
                    if (height == 44) File.WriteAllBytes("Bundles/edge-verification.png", pixels.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(pixels); pixels = null;
                    target.Release(); UnityEngine.Object.DestroyImmediate(target); target = null;
                }
                Debug.Log("GenesisUI edge verification passed: 3 shapes, border lit, interior clear (" + SystemInfo.graphicsDeviceType + ")");
                VerifyOrbit(material);
            }
            finally
            {
                RenderTexture.active = previous;
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        private static void VerifyOrbit(Material material)
        {
            material.SetVector("_Rect", new Vector4(-40f, -40f, 40f, 40f));
            material.SetVector("_QuadSize", new Vector4(100f, 100f, 0f, 0f));
            material.SetFloat("_Orbit", 1f);
            var target = new RenderTexture(200, 200, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(200, 200, TextureFormat.RGBA32, false, true);
            try
            {
                target.Create();
                for (int phase = 0; phase < 4; phase++)
                {
                    material.SetFloat("_Phase", phase * 0.25f);
                    RenderTexture.active = target; GL.Clear(false, true, Color.clear);
                    Graphics.Blit(Texture2D.whiteTexture, target, material);
                    pixels.ReadPixels(new Rect(0, 0, 200, 200), 0, 0); pixels.Apply();
                    var points = new[] { new Vector2Int(100, 176), new Vector2Int(176, 100), new Vector2Int(100, 24), new Vector2Int(24, 100) };
                    if (pixels.GetPixel(100, 100).r > 0.01f) throw new Exception("Orbit lights item interior");
                    for (int i = 0; i < points.Length; i++)
                    {
                        float red = pixels.GetPixel(points[i].x, points[i].y).r;
                        if (i == phase ? red < 0.35f : red > 0.12f) throw new Exception("Orbit segment at wrong edge: phase " + phase + ", edge " + i);
                    }
                    if (phase == 1) File.WriteAllBytes("Bundles/orbit-verification.png", pixels.EncodeToPNG());
                }
                Debug.Log("GenesisUI orbit verification passed: 4 phases, moving segment, interior clear");
            }
            finally { target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels); }
        }
    }
}
