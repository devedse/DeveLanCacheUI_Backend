using System;
using System.IO;
using System.Text.Json;

namespace DeveLanCacheUI_Backend.Services.OriginalDepotEnricher
{
    public static class SteamApi
    {
        private static readonly Lazy<SteamApiData> _steamApiData = new Lazy<SteamApiData>(LoadSteamApiData);

        public static SteamApiData SteamApiData => _steamApiData.Value;

        private static SteamApiData LoadSteamApiData()
        {
            string path = Path.Combine("Steam", "SteamData.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<SteamApiData>(json);
            }
            else
            {
                throw new FileNotFoundException($"File not found: {path}");
            }
        }
    }
}
