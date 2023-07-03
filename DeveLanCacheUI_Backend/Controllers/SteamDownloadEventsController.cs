using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using DeveLanCacheUI_Backend.Steam;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
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
        public async Task<IEnumerable<DbSteamAppDownloadEvent>> GetBySkipAndCount(int skip, int count)
        {
            var allDownloadEvents =  await _dbContext.SteamAppDownloadEvents.Include(t => t.SteamDepot).OrderByDescending(t => t.LastUpdatedAt).Skip(skip).Take(count).ToListAsync();
            foreach(var downloadEvent in allDownloadEvents)
            {
                downloadEvent.SteamDepot.SteamApp = SteamApi.SteamApiData?.applist?.apps?.FirstOrDefault(t => t?.appid == downloadEvent.SteamDepot.SteamAppId);
            }
            return allDownloadEvents;
        }

        [HttpGet]
        public async Task<IEnumerable<EventGroup>> GetBySkipAndCount2(int skip, int count)
        {
            //Yeah this is basically a piece of shit method.

            var allDownloadEvents = await _dbContext.SteamAppDownloadEvents
                .Include(t => t.SteamDepot)
                .OrderByDescending(t => t.LastUpdatedAt)
                .ToListAsync();


            var enumeratedEvents = allDownloadEvents.Select((t, i) => new { Event = t, Index = i, UniqueId = t.SteamDepot.SteamAppId ?? -i }).ToList();

            var groupedEvents = enumeratedEvents
                .GroupBy(t => t.UniqueId)
                .SelectMany(appGroup => appGroup
                    .GroupBy(t => new
                    {
                        t.Event.SteamDepot.SteamAppId,
                        DateKey = new DateTime(t.Event.CreatedAt.Year, t.Event.CreatedAt.Month, t.Event.CreatedAt.Day, t.Event.CreatedAt.Hour, t.Event.CreatedAt.Minute / 5 * 5, 0),
                        IntervalGroup = t.Index / 5
                    })
                    .Select(group => new EventGroup
                    {
                        AppId = group.Key.SteamAppId ?? group.First().UniqueId,
                        StartDate = group.Min(t => t.Event.CreatedAt),
                        LastUpdatedAt = group.Max(t => t.Event.LastUpdatedAt),
                        DepotIds = group.Select(t => t.Event.SteamDepot.Id).ToList()
                    }))
                .OrderByDescending(t => t.LastUpdatedAt)
                .Skip(skip)
                .Take(count)
                .ToList();

            return groupedEvents;
        }

        public class EventGroup
        {
            public int AppId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime LastUpdatedAt { get; set; }
            public List<int> DepotIds { get; set; }
        }

        public class EventIntervalGroup
        {
            public int IntervalGroup { get; set; }
            public EventGroup EventGroup { get; set; }
        }
    }
}