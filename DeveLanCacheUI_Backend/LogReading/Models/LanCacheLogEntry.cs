namespace DeveLanCacheUI_Backend.LogReading.Models
{
    public class LanCacheLogEntry
    {
        public string OriginalLogLine { get; set; }
        public string? ParseException { get; set; }

        public string Protocol { get; set; }
        public string IpAddress { get; set; }
        public DateTime DateTime { get; set; }
        public string Method { get; set; }
        public string Uri { get; set; }
        public string ProtocolVersion { get; set; }
        public int HttpStatusCode { get; set; }
        public long ContentLength { get; set; }
        public string Referer { get; set; }
        public string UserAgent { get; set; }
        public string CacheHitStatus { get; set; }
        public string ServerName { get; set; }
        public string Unknown { get; set; }

        public int? SteamAppId { get; set; }

        public override string ToString()
        {
            return $"{DateTime} => {Protocol}: {SteamAppId}";
        }
    }
}
