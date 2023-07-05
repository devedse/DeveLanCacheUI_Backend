using DeveLanCacheUI_Backend.LogReading.Models;
using System.Globalization;

namespace DeveLanCacheUI_Backend.LogReading
{
    public static class LanCacheLogLineParser
    {
        private static Dictionary<char, char> SuperChars = new Dictionary<char, char>()
        {
            { '"', '"' },
            { '\'', '\'' },
            { '[', ']' },
            { '(', ')' },
            { '{', '}' }
        };

        public static string[] SuperSplit(string logLine)
        {
            int lastCutter = 0;
            int cur = 0;
            string[] splitted = new string[15];
            bool skipChar = false;

            char? lastOpenCharacter = null;
            char? lastCloseCharacter = null;

            char? curCloseCharWaiter = null;

            for (int i = 0; i < logLine.Length; i++)
            {
                var curChar = logLine[i];
                bool lastCharacter = i == logLine.Length - 1;
                if (lastCharacter)
                {

                }

                if (!skipChar || lastCharacter)
                {
                    if (lastCharacter || (curCloseCharWaiter == null && curChar == ' '))
                    {
                        var linePart = logLine.Substring(lastCutter, i - lastCutter + (lastCharacter ? 1 : 0));
                        if (lastOpenCharacter != null)
                        {
                            linePart = linePart.TrimStart(lastOpenCharacter.Value);
                        }
                        if (lastCloseCharacter != null)
                        {
                            linePart = linePart.TrimEnd(lastCloseCharacter.Value);
                        }
                        lastOpenCharacter = null;
                        lastCloseCharacter = null;
                        splitted[cur++] = linePart;
                        lastCutter = i + 1;
                    }
                    else if (curCloseCharWaiter == null && SuperChars.TryGetValue(curChar, out var waitChar))
                    {
                        curCloseCharWaiter = waitChar;
                        lastOpenCharacter = curChar;
                        lastCloseCharacter = waitChar;
                    }
                    else if (curCloseCharWaiter == curChar)
                    {
                        curCloseCharWaiter = null;
                    }
                    else if (curChar == '\\')
                    {
                        skipChar = true;
                    }
                }
                else
                {
                    skipChar = false;
                }
            }
            return splitted;
        }

        public static LanCacheLogEntry ParseLogEntry(string logLine)
        {
            var entry = new LanCacheLogEntry();
            entry.OriginalLogLine = logLine;
            try
            {
                // Split the log line by whitespace, assuming it's space delimited
                var tokens = logLine.Split(' ');


                // Assuming [steam] 10.88.10.1 / - - - [28/Jun/2023:20:14:49 +0200] "GET /depot/434174/chunk/9437c354e87778aeafe94a65ee042432440d4037 HTTP/1.1" 200 392304 "-" "Valve/Steam HTTP Client 1.0" "HIT" "cache1-ams1.steamcontent.com" "-"
                //          [127.0.0.1] 127.0.0.1 / - - - [01/Jul/2023:03:32:18 +0200] "GET /lancache-heartbeat HTTP/1.1" 204 0 "-" "Wget/1.19.4 (linux-gnu)" "-" "127.0.0.1" "-"
                entry.Protocol = tokens[0].TrimStart('[').TrimEnd(']');

                if (entry.Protocol == "steam")
                {
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


                    var uriParts = entry.Uri.Split('/');
                    if (uriParts.Length > 2 && uriParts[1] == "depot")
                    {
                        entry.SteamDepotId = int.Parse(uriParts[2]);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Could not parse appid from log: {logLine} => {entry.Uri}");
                    }
                }
                else
                {
                    entry.Protocol = $"unparsable({entry.Protocol})";
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
