namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class DownloadStatsController : ControllerBase
    {
        private readonly DeveLanCacheConfiguration _config;
        private readonly DeveLanCacheUIDbContext _dbContext;
        private readonly ILogger<DownloadStatsController> _logger;

        public DownloadStatsController(
            DeveLanCacheUIDbContext dbContext,
            DeveLanCacheConfiguration config,
            ILogger<DownloadStatsController> logger)
        {
            _dbContext = dbContext;
            _config = config;
            _logger = logger;
        }

        [HttpGet]
        public async Task<DownloadStats> GetTotalDownloadStats()
        {
            var excludedIps = _config.ExcludedClientIpsArray ?? Array.Empty<string>();

            var totalStats = await _dbContext.DownloadEvents
                .Where(de => !excludedIps.Contains(de.ClientIp))
                .GroupBy(_ => 1)
                .Select(g => new DownloadStats
                {
                    Identifier = "Total",
                    TotalCacheHitBytes = g.Sum(de => de.CacheHitBytes),
                    TotalCacheMissBytes = g.Sum(de => de.CacheMissBytes)
                })
                .FirstOrDefaultAsync();

            return totalStats ?? new DownloadStats { Identifier = "Total" };
        }

        [HttpGet]
        public async Task<IEnumerable<DownloadStats>> GetDownloadStatsPerClient()
        {
            var excludedIps = _config.ExcludedClientIpsArray ?? Array.Empty<string>();

            var statsQuery = await _dbContext.DownloadEvents
                .Where(de => !excludedIps.Contains(de.ClientIp))
                .GroupBy(de => de.ClientIp)
                .Select(g => new DownloadStats
                {
                    Identifier = g.Key,
                    TotalCacheHitBytes = g.Sum(de => de.CacheHitBytes),
                    TotalCacheMissBytes = g.Sum(de => de.CacheMissBytes)
                })
                .ToListAsync();

            return statsQuery;
        }

        [HttpGet]
        public async Task<IEnumerable<DownloadStats>> GetDownloadStatsPerService()
        {
            var excludedIps = _config.ExcludedClientIpsArray ?? Array.Empty<string>();

            var statsQuery = await _dbContext.DownloadEvents
                .Where(de => !excludedIps.Contains(de.ClientIp))
                .GroupBy(de => de.CacheIdentifier)
                .Select(g => new DownloadStats
                {
                    Identifier = g.Key,
                    TotalCacheHitBytes = g.Sum(de => de.CacheHitBytes),
                    TotalCacheMissBytes = g.Sum(de => de.CacheMissBytes)
                })
                .ToListAsync();

            return statsQuery;
        }
    }
}
