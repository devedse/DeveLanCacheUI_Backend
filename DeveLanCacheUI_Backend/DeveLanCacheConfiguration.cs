namespace DeveLanCacheUI_Backend
{
    public class DeveLanCacheConfiguration
    {
        public required string DeveLanCacheUIDataDirectory { get; set; }
        public required string LanCacheLogsDirectory { get; set; }
        public required bool UseDirectSteamIntegrationForDepots { get; set; }
    }
}
