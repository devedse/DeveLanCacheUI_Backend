using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SteamDownloadEventsController : ControllerBase
    {
        private readonly DeveLanCacheUIDbContext _dbContext;
        private readonly ILogger<SteamDownloadEventsController> _logger;

        public SteamDownloadEventsController(DeveLanCacheUIDbContext dbContext, ILogger<SteamDownloadEventsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<DbSteamAppDownloadEvent>> Get(int skip, int count)
        {
            return await _dbContext.SteamAppDownloadEvents.Include(t => t.SteamDepot).ThenInclude(t => t.SteamApp).OrderByDescending(t => t.LastUpdatedAt).Skip(skip).Take(count).ToListAsync();
        }
    }
}