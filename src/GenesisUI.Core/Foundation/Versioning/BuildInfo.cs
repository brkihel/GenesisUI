using System;
using System.Text.RegularExpressions;

namespace GenesisUI.Foundation.Versioning
{
    public enum BuildChannel
    {
        Dev,
        Preview,
        Release,
    }

    /// <summary>Builds the version string shown in logs, reports and the watermark (docs/RELEASE.md).</summary>
    public static class BuildInfo
    {
        private static readonly Regex SemVer = new Regex(@"^\d+\.\d+\.\d+$", RegexOptions.CultureInvariant);
        private static readonly Regex Sha = new Regex(@"^[0-9a-f]{7,40}$", RegexOptions.CultureInvariant);

        public static bool IsValidVersion(string version) => version != null && SemVer.IsMatch(version);

        /// <summary>
        /// "0.1.0-dev+abc1234", "0.1.0-preview.3+abc1234" or "0.1.0".
        /// A missing or malformed sha becomes "unknown" rather than failing: the
        /// version string must never be the thing that breaks a build.
        /// </summary>
        public static string Format(string version, BuildChannel channel, int previewNumber, string sha)
        {
            if (!IsValidVersion(version)) throw new ArgumentException("version must be MAJOR.MINOR.PATCH", nameof(version));
            string build = sha != null && Sha.IsMatch(sha) ? sha : "unknown";
            switch (channel)
            {
                case BuildChannel.Dev: return version + "-dev+" + build;
                case BuildChannel.Preview:
                    if (previewNumber < 1) throw new ArgumentOutOfRangeException(nameof(previewNumber));
                    return version + "-preview." + previewNumber + "+" + build;
                default: return version;
            }
        }
    }
}
