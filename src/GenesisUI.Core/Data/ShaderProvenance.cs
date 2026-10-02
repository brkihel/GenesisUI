using System;
using System.Collections.Generic;

namespace GenesisUI.Data
{
    public sealed class ShaderProvenance
    {
        public string BundleSha256 { get; private set; }
        public string SourceCommit { get; private set; }
        public string UnityVersion { get; private set; }
        public string GpuApi { get; private set; }
        public string VerifiedAtUtc { get; private set; }

        public static ShaderProvenance Parse(string text)
        {
            if (text == null || text.Length > 16384) throw new FormatException("Shader provenance exceeds its size limit");
            var root = StrictJson.Parse(text) as Dictionary<string, object> ?? throw new FormatException("Shader provenance must be an object");
            string Read(Dictionary<string, object> obj, string key) => obj.TryGetValue(key, out var value) && value is string s && s.Length <= 128 ? s : throw new FormatException("Shader provenance missing " + key);
            bool Number(Dictionary<string, object> obj, string key, double expected) => obj.TryGetValue(key, out var n) && n is double d && d == expected;
            if (!Number(root, "schema", 1) || Read(root, "requiredShader") != "GenesisUI/Keyed" || Read(root, "target") != "StandaloneWindows64") throw new FormatException("Shader provenance schema/target mismatch");
            if (!root.TryGetValue("gpu", out var gpuValue) || !(gpuValue is Dictionary<string, object> gpu) || !Number(gpu, "solid", 256) || !Number(gpu, "edge", 512) || !Number(gpu, "clear", 256)) throw new FormatException("Shader GPU check missing");
            var result = new ShaderProvenance { BundleSha256 = Read(root, "bundleSha256"), SourceCommit = Read(root, "sourceCommit"), UnityVersion = Read(root, "unityVersion"), GpuApi = Read(gpu, "api"), VerifiedAtUtc = Read(gpu, "verifiedAtUtc") };
            if (!Hex(result.BundleSha256, 64) || !Hex(result.SourceCommit, 40) || result.UnityVersion != "6000.0.75f1" || result.GpuApi != "Direct3D11" || !DateTimeOffset.TryParse(result.VerifiedAtUtc, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _)) throw new FormatException("Shader provenance values invalid");
            return result;
        }
        private static bool Hex(string value, int length)
        {
            if (value.Length != length) return false;
            foreach (char c in value) if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F')) return false;
            return true;
        }
    }
}
