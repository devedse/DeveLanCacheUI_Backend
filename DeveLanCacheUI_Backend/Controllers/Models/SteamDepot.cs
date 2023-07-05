namespace DeveLanCacheUI_Backend.Controllers.Models
{
    public class SteamDepot
    {
        public int Id { get; set; }

        public App? SteamApp { get; set; }
        public int? SteamAppId { get; set; }
    }
}
