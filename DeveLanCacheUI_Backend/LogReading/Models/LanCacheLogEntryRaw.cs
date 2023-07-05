namespace DeveLanCacheUI_Backend.LogReading.Models
{
    public class LanCacheLogEntryRaw
    {
        //0
        public string CacheIdentifier { get; set; }
        //1
        public string RemoteAddress { get; set; }
        //2 random slash
        //3
        public string ForwardedFor { get; set; }
        //4 random dash
        //5
        public string RemoteUser { get; set; }
        //6
        public string TimeLocal { get; set; }
        //7
        public string Request { get; set; }
        //8
        public string Status { get; set; }
        //9
        public string BodyBytesSent { get; set; }
        //10
        public string Referer { get; set; }
        //11
        public string UserAgent { get; set; }
        //12
        public string UpstreamCacheStatus { get; set; }
        //13
        public string Host { get; set; }
        //14
        public string HttpRange { get; set; }
    }

}
