using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace GenesisUI.Contract.Tests
{
    internal static class TestPaths
    {
        public static string RepoRoot
        {
            get
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "GenesisUI.sln"))) dir = dir.Parent;
                return dir?.FullName;
            }
        }

        public static string RefDir => RepoRoot == null ? null : Path.Combine(RepoRoot, "ref");

        public static string Configuration =>
            typeof(TestPaths).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "Configuration")?.Value ?? "Debug";

        public static string PluginDll =>
            RepoRoot == null ? null : Path.Combine(RepoRoot, "src", "GenesisUI", "bin", Configuration, "net48", "GenesisUI.dll");

        public static void SkipUnlessRefs()
        {
            Skip.If(RefDir == null || !File.Exists(Path.Combine(RefDir, "assembly_valheim.dll")),
                "ref/ is empty: run tools/fill-ref.sh (see docs/TESTING.md).");
        }

        public static void SkipUnlessPlugin()
        {
            Skip.If(PluginDll == null || !File.Exists(PluginDll),
                "GenesisUI.dll not built for " + Configuration + " at " + PluginDll);
        }
    }
}
