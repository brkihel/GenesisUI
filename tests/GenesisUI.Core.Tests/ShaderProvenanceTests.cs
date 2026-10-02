using System;
using System.IO;
using System.Security.Cryptography;
using GenesisUI.Data;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class ShaderProvenanceTests
    {
        private static string Root()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "GenesisUI.sln"))) dir = dir.Parent;
            return dir?.FullName ?? throw new DirectoryNotFoundException("Repository not found");
        }
        [Fact]
        public void Shipped_provenance_matches_bundle_and_rejects_invalid_gpu_evidence()
        {
            string text = File.ReadAllText(Path.Combine(Root(), "art/shaders/provenance.json"));
            var evidence = ShaderProvenance.Parse(text);
            Assert.Equal(evidence.BundleSha256, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(Root(), "art/shaders/genesisui.shaders")))));
            Assert.Throws<FormatException>(() => ShaderProvenance.Parse(text.Replace("Direct3D11", "unknown")));
            Assert.Throws<FormatException>(() => ShaderProvenance.Parse(new string('x', 16385)));
        }
    }
}
