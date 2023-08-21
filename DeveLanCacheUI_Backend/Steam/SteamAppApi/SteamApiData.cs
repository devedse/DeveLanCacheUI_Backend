namespace DeveLanCacheUI_Backend.Steam.SteamAppApi
{
    public class SteamApiData
    {
        public Applist applist { get; set; }
    }

    public class Applist
    {
        public App[] apps { get; set; }
    }

    public class App
    {
        public int appid { get; set; }
        public string name { get; set; }
    }
}
