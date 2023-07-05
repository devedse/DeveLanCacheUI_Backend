using System.Globalization;

namespace DeveLanCacheUI_Backend.LogReading.Models
{
    public class LanCacheLogEntryRaw
    {
        //0
        public required string CacheIdentifier { get; init; }
        //1
        public string RemoteAddress { get; init; }
        //2 random slash
        //3
        public string ForwardedFor { get; init; }
        //4 random dash
        //5
        public string RemoteUser { get; init; }
        //6
        public string TimeLocal { get; init; }
        //7
        public string Request { get; init; }
        //8
        public string Status { get; init; }
        //9
        public string BodyBytesSent { get; init; }
        //10
        public string Referer { get; init; }
        //11
        public string UserAgent { get; init; }
        //12
        public string UpstreamCacheStatus { get; init; }
        //13
        public string Host { get; init; }
        //14
        public string HttpRange { get; init; }

        public required string OriginalLogLine { get; init; }
        public string ParseException { get; init; }

        public DateTime DateTime { get; private set; }
        public long BodyBytesSentLong { get; private set; }

        //Steam Only
        public int? SteamDepotId { get; private set; }

        public void CalculateFields()
        {
            DateTime = DateTime.ParseExact(TimeLocal, "dd/MMM/yyyy:HH:mm:ss zzz", CultureInfo.InvariantCulture);
            BodyBytesSentLong = long.Parse(BodyBytesSent);

            if (CacheIdentifier == "steam")
            {
                var urlPart = Request.Split(' ')[1];
                var splittedUrl = urlPart.Split('/');
                if (splittedUrl[1] == "depot")
                {
                    SteamDepotId = int.Parse(splittedUrl[2]);
                }
                else
                {
                    throw new InvalidOperationException($"Could not parse SteamDepotId from {Request}");
                }
            }
        }

    }
}
