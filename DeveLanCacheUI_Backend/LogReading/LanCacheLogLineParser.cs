using DeveLanCacheUI_Backend.LogReading.Models;
using System.Globalization;

namespace DeveLanCacheUI_Backend.LogReading
{
    public static class LanCacheLogLineParser
    {
        public static LanCacheLogEntry ParseLogEntry(string logLine)
        {
            var entry = new LanCacheLogEntry();
            entry.OriginalLogLine = logLine;
            try
            {
                // Split the log line by whitespace, assuming it's space delimited
                var tokens = logLine.Split(' ');


                // Assuming [steam] 10.88.10.1 / - - - [28/Jun/2023:20:14:49 +0200] "GET /depot/434174/chunk/9437c354e87778aeafe94a65ee042432440d4037 HTTP/1.1" 200 392304 "-" "Valve/Steam HTTP Client 1.0" "HIT" "cache1-ams1.steamcontent.com" "-"
                entry.Protocol = tokens[0].TrimStart('[').TrimEnd(']');
                entry.IpAddress = tokens[1];
                // Skip tokens[2] as it's '/' character
                // Skip tokens[3] as it's '-' character
                // Skip tokens[4] as it's '-' character
                // Skip tokens[5] as it's '-' character
                var trimmedDateTime = (tokens[6] + ' ' + tokens[7]).TrimStart('[').TrimEnd(']');
                entry.DateTime = DateTime.ParseExact(trimmedDateTime, "dd/MMM/yyyy:HH:mm:ss zzz", CultureInfo.InvariantCulture);

                //var requestTokens = tokens[7].TrimStart('"').TrimEnd('"').Split(' ');
                entry.Method = tokens[8].Trim('"');
                entry.Uri = tokens[9];
                entry.ProtocolVersion = tokens[10];

                entry.HttpStatusCode = int.Parse(tokens[11]);
                entry.ContentLength = long.Parse(tokens[12]);
                // Skip tokens[10] as it's '-' character

                entry.UserAgent = string.Join(" ", tokens.Skip(14).Take(4)).TrimStart('"').TrimEnd('"');
                entry.CacheHitStatus = tokens[18].TrimStart('"').TrimEnd('"');
                entry.ServerName = tokens[19].TrimStart('"').TrimEnd('"');
                // Skip tokens[14] as it's '-' character

                if (entry.Protocol == "steam")
                {
                    var uriParts = entry.Uri.Split('/');
                    if (uriParts.Length > 2 && uriParts[1] == "depot")
                    {
                        entry.SteamAppId = int.Parse(uriParts[2]);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Could not parse appid from log: {logLine} => {entry.Uri}");
                    }
                }

            }
            catch (Exception ex)
            {
                entry.Protocol = "unknown";
                entry.ParseException = ex.ToString();
            }
            return entry;
        }
    }
}
