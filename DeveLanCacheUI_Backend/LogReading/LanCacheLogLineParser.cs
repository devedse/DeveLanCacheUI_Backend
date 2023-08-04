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

        public static LanCacheLogEntryRaw LogLineToLanCacheLogEntryRaw(string logLine)
        {
            var splittedLogEntry = SuperSplit(logLine);

            return new LanCacheLogEntryRaw
            {
                CacheIdentifier = splittedLogEntry[0],
                RemoteAddress = splittedLogEntry[1],
                ForwardedFor = splittedLogEntry[3],
                RemoteUser = splittedLogEntry[5],
                TimeLocal = splittedLogEntry[6],
                Request = splittedLogEntry[7],
                Status = splittedLogEntry[8],
                BodyBytesSent = splittedLogEntry[9],
                Referer = splittedLogEntry[10],
                UserAgent = splittedLogEntry[11],
                UpstreamCacheStatus = splittedLogEntry[12],
                Host = splittedLogEntry[13],
                HttpRange = splittedLogEntry[14],

                OriginalLogLine = logLine
            };
        }

        public static LanCacheLogEntryRaw ParseLogEntry(string logLine)
        {
            try
            {
                var entry = LogLineToLanCacheLogEntryRaw(logLine);
                entry.CalculateFields();
                return entry;
            }
            catch (Exception ex)
            {
                return new LanCacheLogEntryRaw()
                {
                    OriginalLogLine = logLine,
                    CacheIdentifier = "Unknown",
                    ParseException = ex.ToString()
                };
            }
        }
    }
}
