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

        public string? DownloadIdentifier { get; private set; }

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
                    DownloadIdentifier = splittedUrl[2];
                }
                else
                {
                    throw new InvalidOperationException($"Could not parse SteamDepotId from {Request}");
                }
            }
            else if (CacheIdentifier == "blizzard")
            {
                var urlPart = Request.Split(' ')[1];
                var splittedUrl = urlPart.Split('/');
                if (splittedUrl.Length >= 3 && splittedUrl[2].StartsWith("bnt"))
                {
                    splittedUrl[2] = "bnt";
                }
                DownloadIdentifier = string.Join("/", splittedUrl.Skip(1).Take(2));
            }
            else if (CacheIdentifier == "epicgames")
            {
                var urlPart = Request.Split(' ')[1];
                var splittedUrl = urlPart.Split('/');
                if (splittedUrl[1] == "Builds")
                {
                    DownloadIdentifier = splittedUrl[2];
                }
                else
                {
                    throw new InvalidOperationException($"Could not parse epicgamesProjectID from {Request}");
                }
            }
            else if (CacheIdentifier == "riot")
            {
                DownloadIdentifier = "unknown";
            }
            else if (CacheIdentifier == "xboxlive")
            {
                var urlPart = Request.Split(' ')[1];
                var lastPart = urlPart.Split('/').Last();
                //BehaviourInteractive.DeadbyDaylightWindows_7.0.200.0_x64__b1gz2xhdanwfm.msixvc
                var lastPartSplitted = lastPart.Split('_');
                DownloadIdentifier = lastPartSplitted.First();
            }
            else if (CacheIdentifier == "wsus")
            {
                DownloadIdentifier = "unknown";
            }
            else
            {
                DownloadIdentifier = "UnknownDownloadIdentifier";
            }
        }

    }
}
