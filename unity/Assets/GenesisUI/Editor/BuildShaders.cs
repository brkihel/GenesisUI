// Builds GenesisUI's shader bundle (D-033) for Windows, run in batch mode on the build machine:
//   Unity.exe -batchmode -nographics -quit -projectPath <this project> -executeMethod GenesisUI.BuildShaders.Build -logFile build.log
// Output: Bundles/genesisui.shaders (copied into the plugin's art folder by tools/shaders/build.sh).
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GenesisUI
{
    public static class BuildShaders
    {
        private static readonly string[] Shaders =
        {
            "Assets/GenesisUI/Shaders/Metal.shader", "Assets/GenesisUI/Shaders/Burn.shader", "Assets/GenesisUI/Shaders/Blur.shader",
            "Assets/GenesisUI/Shaders/Beam.shader", "Assets/GenesisUI/Shaders/Reveal.shader", "Assets/GenesisUI/Shaders/Shine.shader",
            "Assets/GenesisUI/Shaders/Backdrop.shader", "Assets/GenesisUI/Shaders/Edge.shader", "Assets/GenesisUI/Shaders/Ring.shader",
            "Assets/GenesisUI/Shaders/Embers.shader", "Assets/GenesisUI/Shaders/Heartbeat.shader",
            "Assets/GenesisUI/Shaders/Keyed.shader",
        };

        public static void Build()
        {
            // Valheim on Windows runs Direct3D 11 by default; Vulkan and Direct3D 12 by launch option.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[]
                { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11, UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                  UnityEngine.Rendering.GraphicsDeviceType.Direct3D12 });
            // A shader with a compile error still lands in the bundle (and draws pink in game): refuse it.
            foreach (var path in Shaders)
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null) throw new System.Exception("missing shader " + path);
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                        throw new System.Exception(path + " (" + message.platform + "): " + message.message + " line " + message.line);
            }
            const string output = "Bundles";
            Directory.CreateDirectory(output);
            var build = new AssetBundleBuild
            {
                assetBundleName = "genesisui.shaders",
                assetNames = Shaders,
            };
            var manifest = BuildPipeline.BuildAssetBundles(output, new[] { build },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.DeterministicAssetBundle,
                BuildTarget.StandaloneWindows64);
            if (manifest == null) throw new System.Exception("GenesisUI shader bundle build failed");
            Debug.Log("GenesisUI shader bundle built: " + Path.GetFullPath(Path.Combine(output, "genesisui.shaders")));
        }
    }
}
