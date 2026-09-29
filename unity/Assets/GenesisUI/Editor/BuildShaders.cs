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
        public static void Build()
        {
            // Valheim on Windows runs Direct3D 11 by default; Vulkan and Direct3D 12 by launch option.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[]
                { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11, UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                  UnityEngine.Rendering.GraphicsDeviceType.Direct3D12 });
            const string output = "Bundles";
            Directory.CreateDirectory(output);
            var build = new AssetBundleBuild
            {
                assetBundleName = "genesisui.shaders",
                assetNames = new[] { "Assets/GenesisUI/Shaders/Metal.shader", "Assets/GenesisUI/Shaders/Burn.shader" },
            };
            var manifest = BuildPipeline.BuildAssetBundles(output, new[] { build },
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode | BuildAssetBundleOptions.DeterministicAssetBundle,
                BuildTarget.StandaloneWindows64);
            if (manifest == null) throw new System.Exception("GenesisUI shader bundle build failed");
            Debug.Log("GenesisUI shader bundle built: " + Path.GetFullPath(Path.Combine(output, "genesisui.shaders")));
        }
    }
}
