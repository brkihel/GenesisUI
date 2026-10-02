using System;
using System.IO;
using UnityEngine;

namespace GenesisUI
{
    // A GPU regression check for the shipped preview shader. The red silhouette must
    // stay red at fractional coverage; magenta or a lost silhouette fails this check.
    public static class PreviewKeyVerification
    {
        public static void BuildAndVerify()
        {
            BuildShaders.Build();
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new Exception("Preview edge verification requires a graphics device (omit -nographics)");
            var bundle = AssetBundle.LoadFromFile(Path.GetFullPath("Bundles/genesisui.shaders"));
            if (bundle == null) throw new Exception("Cannot load the built shader bundle");
            Material material = null;
            Texture2D source = null, readback = null;
            RenderTexture target = null;
            var previous = RenderTexture.active;
            try
            {
                var shader = bundle.LoadAsset<Shader>("Assets/GenesisUI/Shaders/Keyed.shader");
                if (shader == null || !shader.isSupported) throw new Exception("Built keyed shader is unsupported");
                material = new Material(shader);
                source = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var redWithoutAlpha = new Color(1f, 0f, 0f, 0f);
                source.SetPixels(new[] { redWithoutAlpha, Color.magenta, redWithoutAlpha, Color.magenta });
                source.Apply();
                target = new RenderTexture(32, 32, 0, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
                target.Create();
                RenderTexture.active = target;
                GL.Clear(false, true, Color.clear);
                Graphics.Blit(source, target, material);
                RenderTexture.active = target;
                readback = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                readback.ReadPixels(new Rect(0, 0, 32, 32), 0, 0);
                readback.Apply();
                int solid = 0, edge = 0, clear = 0;
                foreach (var pixel in readback.GetPixels32())
                {
                    if (pixel.g > 3 || pixel.b > 3) throw new Exception("Key colour leaked into the red silhouette");
                    if (pixel.r > 250) solid++;
                    else if (pixel.r > 3) edge++;
                    else
                    {
                        if (pixel.a > 3) throw new Exception("Key background did not become transparent");
                        clear++;
                    }
                }
                if (solid == 0 || edge == 0 || clear == 0)
                    throw new Exception("Preview must contain solid colour, smooth coverage and a transparent background");
                Debug.Log("GenesisUI preview key verification passed: solid " + solid + ", edge " + edge + ", clear " + clear + " (" + SystemInfo.graphicsDeviceType + ")");
                EdgeVerification.Verify(bundle);
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (source != null) UnityEngine.Object.DestroyImmediate(source);
                if (readback != null) UnityEngine.Object.DestroyImmediate(readback);
                if (material != null) UnityEngine.Object.DestroyImmediate(material);
                bundle.Unload(true);
            }
        }
    }
}
