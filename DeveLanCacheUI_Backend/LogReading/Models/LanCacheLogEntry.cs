namespace DeveLanCacheUI_Backend.LogReading.Models
{
    public class LanCacheLogEntry
    {
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
    }
}
