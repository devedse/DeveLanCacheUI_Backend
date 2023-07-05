namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class DownloadStatsController : ControllerBase
    {
        private readonly DeveLanCacheUIDbContext _dbContext;
        private readonly ILogger<DownloadStatsController> _logger;

        public DownloadStatsController(DeveLanCacheUIDbContext dbContext, ILogger<DownloadStatsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<DownloadStats>> GetDownloadStatsPerClient()
        {
            var statsQuery = await _dbContext.DownloadEvents
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
            var statsQuery = await _dbContext.DownloadEvents
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