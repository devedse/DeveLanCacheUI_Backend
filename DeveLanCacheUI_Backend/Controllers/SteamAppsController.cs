using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SteamAppsController : ControllerBase
    {
        private readonly DeveLanCacheUIDbContext _dbContext;
        private readonly ILogger<SteamAppsController> _logger;

        public SteamAppsController(DeveLanCacheUIDbContext dbContext, ILogger<SteamAppsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IEnumerable<DbSteamAppDownloadEvent>> Get()
        {
            return await _dbContext.SteamAppDownloadEvents.OrderByDescending(t => t.LastUpdatedAt).ToListAsync();
        }
    }
}