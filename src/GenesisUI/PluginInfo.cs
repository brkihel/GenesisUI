namespace GenesisUI
{
    /// <summary>The single place for name, GUID and version (docs/RELEASE.md).</summary>
    internal static class PluginInfo
    {
        public const string Guid = "Genesis.GenesisUI";
        public const string Name = "GenesisUI";

        /// <summary>MAJOR.MINOR.PATCH; BepInEx parses it as System.Version.</summary>
        public const string Version = "1.0.1";

        /// <summary>Bumped for every Preview package handed out for testing.</summary>
        public const int PreviewNumber = 1; // next Preview of the next version
    }
}
