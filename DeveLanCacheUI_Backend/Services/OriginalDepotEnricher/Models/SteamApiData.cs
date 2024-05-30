namespace DeveLanCacheUI_Backend.Steam
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
        public uint appid { get; set; }
        public string name { get; set; }
    }
}
