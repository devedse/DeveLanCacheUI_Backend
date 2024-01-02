namespace DeveLanCacheUI_Backend.Controllers.Models
{
    public class SteamDepot
    {
        public uint Id { get; set; }

        public App? SteamApp { get; set; }
        public uint? SteamAppId { get; set; }
    }
}
