using System.Collections.Generic;

namespace DeveLanCacheUI_Backend
{
    /// <summary>
    /// Configuration settings for the DeveLanCacheUI backend.
    /// </summary>
    public class DeveLanCacheConfiguration
    {
        /// <summary>
        /// List of client IP addresses to exclude from statistics.
        /// </summary>
        public List<string> ExcludedClientIps { get; set; } = new List<string>();

        /// <summary>
        /// Base directory for DeveLanCacheUI data (used for SQLite file path etc.).
        /// </summary>
        public required string DeveLanCacheUIDataDirectory { get; set; }

        /// <summary>
        /// Directory where LanCache access logs are stored.
        /// </summary>
        public required string LanCacheLogsDirectory { get; set; }

        /// <summary>
        /// Feature flag for direct Steam integration.
        /// </summary>
        public required bool Feature_DirectSteamIntegration { get; set; }

        /// <summary>
        /// Feature flag to skip log lines based on bytes read.
        /// </summary>
        public required bool Feature_SkipLinesBasedOnBytesRead { get; set; }
    }
}
