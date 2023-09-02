namespace DeveLanCacheUI_Backend
{
    public class DeveLanCacheConfiguration
    {
        public required string DeveLanCacheUIDataDirectory { get; set; }
        public required string LanCacheLogsDirectory { get; set; }
        public required bool Feature_DirectSteamIntegration { get; set; }
        public required bool Feature_SkipLinesBasedOnBytesRead { get; set; }
    }
}
