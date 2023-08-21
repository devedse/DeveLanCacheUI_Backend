namespace DeveLanCacheUI_Backend.Services
{
    public interface ISteamAppObtainerService
    {
        App? GetSteamAppById(uint? steamAppId);
    }
}
