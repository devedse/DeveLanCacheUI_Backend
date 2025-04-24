using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeveLanCacheUI_Backend;
using DeveLanCacheUI_Backend.Controllers.Models;

namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class DownloadStatsController : ControllerBase
    {
        private readonly DeveLanCacheUIDbContext _dbContext;
        private readonly ILogger<DownloadStatsController> _logger;
        private readonly DeveLanCacheConfiguration _config;

        public DownloadStatsController(
            DeveLanCacheUIDbContext dbContext,
            ILogger<DownloadStatsController> logger,
            DeveLanCacheConfiguration config)
        {
            _dbContext = dbContext;
            _logger = logger;
            _config = config;
        }

        [HttpGet]
        public async Task<DownloadStats> GetTotalDownloadStats()
        {
            // Sum stats excluding the excluded client IPs
            var query = _dbContext.DownloadEvents
                .AsQueryable()
                .Where(de => !_config.ExcludedClientIps.Contains(de.ClientIp));

            var totalStats = await query
                .GroupBy(de => 1)
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
            // Exclude specified client IPs
            var query = _dbContext.DownloadEvents
                .AsQueryable()
                .Where(de => !_config.ExcludedClientIps.Contains(de.ClientIp));

            var stats = await query
                .GroupBy(de => de.ClientIp)
                .Select(g => new DownloadStats
                {
                    Identifier = g.Key,
                    TotalCacheHitBytes = g.Sum(de => de.CacheHitBytes),
                    TotalCacheMissBytes = g.Sum(de => de.CacheMissBytes)
                })
                .ToListAsync();

            return stats;
        }

        [HttpGet]
        public async Task<IEnumerable<DownloadStats>> GetDownloadStatsPerService()
        {
            // Service stats unaffected by client IP exclusion
            var stats = await _dbContext.DownloadEvents
                .GroupBy(de => de.CacheIdentifier)
                .Select(g => new DownloadStats
                {
                    Identifier = g.Key,
                    TotalCacheHitBytes = g.Sum(de => de.CacheHitBytes),
                    TotalCacheMissBytes = g.Sum(de => de.CacheMissBytes)
                })
                .ToListAsync();

            return stats;
        }
    }
}
