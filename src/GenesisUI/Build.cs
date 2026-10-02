using GenesisUI.Foundation;
using GenesisUI.Foundation.Versioning;

namespace GenesisUI
{
    /// <summary>What this build is (docs/ARCHITECTURE.md §9). Chosen by the build configuration, never at runtime.</summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Versioning.BuildInfo))]
    internal static class Build
    {
#if GENESIS_CHANNEL_DEV
        public const BuildChannel Channel = BuildChannel.Dev;
        public const LogSeverity MinimumLogSeverity = LogSeverity.Debug;
#elif GENESIS_CHANNEL_PREVIEW
        public const BuildChannel Channel = BuildChannel.Preview;
        public const LogSeverity MinimumLogSeverity = LogSeverity.Info;
#else
        public const BuildChannel Channel = BuildChannel.Release;
        public const LogSeverity MinimumLogSeverity = LogSeverity.Warning;
#endif

        public static readonly string FullVersion =
            BuildInfo.Format(PluginInfo.Version, Channel, PluginInfo.PreviewNumber, BuildStamp.Sha);

#if GENESIS_DIAGNOSTICS
        public const bool Diagnostics = true;
#else
        public const bool Diagnostics = false;
#endif
    }
}
