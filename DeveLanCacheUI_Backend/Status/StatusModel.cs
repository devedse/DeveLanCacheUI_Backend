namespace DeveLanCacheUI_Backend.Status
{
    public class StatusModel
    {
        public string ApplicationName { get; set; }
        public string Version { get; set; }
        public string? SteamDepotVersion { get; set; }
        public string? SteamChangeNumber { get; set; }
        public string UpTime { get; set; }
    }
}
