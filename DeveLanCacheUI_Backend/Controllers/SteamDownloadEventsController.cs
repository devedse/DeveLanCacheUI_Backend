using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using DeveLanCacheUI_Backend.Steam;
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
            var allDownloadEvents =  await _dbContext.SteamAppDownloadEvents.Include(t => t.SteamDepot).OrderByDescending(t => t.LastUpdatedAt).Skip(skip).Take(count).ToListAsync();
            foreach(var downloadEvent in allDownloadEvents)
            {
                downloadEvent.SteamDepot.SteamApp = SteamApi.SteamApiData?.applist?.apps?.FirstOrDefault(t => t?.appid == downloadEvent.SteamDepot.SteamAppId);
            }
            return allDownloadEvents;
        }
    }
}