using DeveLanCacheUI_Backend.Controllers.Models;
using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using DeveLanCacheUI_Backend.Steam;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class DownloadEventsController : ControllerBase
    {
        private readonly DeveLanCacheUIDbContext _dbContext;
        private readonly ILogger<DownloadEventsController> _logger;

        public DownloadEventsController(DeveLanCacheUIDbContext dbContext, ILogger<DownloadEventsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public Task<IEnumerable<DownloadEvent>> GetBySkipAndCount(int skip, int count)
        {
            return GetFilteredBySkipAndCountInternal(skip, count);
        }

        [HttpGet]
        public Task<IEnumerable<DownloadEvent>> GetFilteredBySkipAndCount(int skip, int count, string filter)
        {
            return GetFilteredBySkipAndCountInternal(skip, count, filter);
        }

        private async Task<IEnumerable<DownloadEvent>> GetFilteredBySkipAndCountInternal(int skip, int count, string? filter = null)
        {
            IQueryable<DbDownloadEvent> tmpResult = _dbContext.DownloadEvents;
            if (filter != null)
            {
                tmpResult = tmpResult.Where(t => t.CacheIdentifier == filter && t.CacheHitBytes != 0 && t.CacheMissBytes != 0);
            }

            var result = await tmpResult
                .GroupJoin(
                    _dbContext.SteamDepots,
                    downloadEvent => downloadEvent.DownloadIdentifier,
                    steamDepot => steamDepot.Id,
                    (downloadEvent, steamDepot) => new { downloadEvent, steamDepot }
                )
                .SelectMany(
                    x => x.steamDepot.DefaultIfEmpty(),
                    (x, y) => new DownloadEvent
                    {
                        Id = x.downloadEvent.Id,
                        CacheIdentifier = x.downloadEvent.CacheIdentifier,
                        DownloadIdentifier = x.downloadEvent.DownloadIdentifier,
                        DownloadIdentifierString = x.downloadEvent.DownloadIdentifierString,
                        ClientIp = x.downloadEvent.ClientIp,
                        CreatedAt = x.downloadEvent.CreatedAt,
                        LastUpdatedAt = x.downloadEvent.LastUpdatedAt,
                        CacheHitBytes = x.downloadEvent.CacheHitBytes,
                        CacheMissBytes = x.downloadEvent.CacheMissBytes,
                        SteamDepot = y == null
                            ? null
                            : new SteamDepot
                            {
                                Id = y.Id,
                                SteamAppId = y.SteamAppId
                            }
                    }
                ).OrderByDescending(t => t.LastUpdatedAt).Skip(skip).Take(count).ToListAsync();

            foreach (var item in result.Where(t => t.CacheIdentifier == "steam" && t.SteamDepot != null))
            {
                item.SteamDepot.SteamApp = SteamApi.SteamApiData?.applist?.apps?.FirstOrDefault(t => t?.appid == item.SteamDepot.SteamAppId);
            }

            return result.ToList();
        }

        //[HttpGet]
        //public async Task<IEnumerable<EventGroup>> GetBySkipAndCount2(int skip, int count)
        //{
        //    //Yeah this is basically a piece of shit method.

        //    var allDownloadEvents = await _dbContext.DownloadEvents
        //        .Include(t => t.SteamDepot)
        //        .OrderByDescending(t => t.LastUpdatedAt)
        //        .ToListAsync();


        //    var enumeratedEvents = allDownloadEvents.Select((t, i) => new { Event = t, Index = i, UniqueId = t.SteamDepot.SteamAppId ?? -i }).ToList();

        //    var groupedEvents = enumeratedEvents
        //        .GroupBy(t => t.UniqueId)
        //        .SelectMany(appGroup => appGroup
        //            .GroupBy(t => new
        //            {
        //                t.Event.SteamDepot.SteamAppId,
        //                DateKey = new DateTime(t.Event.CreatedAt.Year, t.Event.CreatedAt.Month, t.Event.CreatedAt.Day, t.Event.CreatedAt.Hour, t.Event.CreatedAt.Minute / 5 * 5, 0),
        //                IntervalGroup = t.Index / 5
        //            })
        //            .Select(group => new EventGroup
        //            {
        //                AppId = group.Key.SteamAppId ?? group.First().UniqueId,
        //                StartDate = group.Min(t => t.Event.CreatedAt),
        //                LastUpdatedAt = group.Max(t => t.Event.LastUpdatedAt),
        //                DepotIds = group.Select(t => t.Event.SteamDepot.Id).ToList()
        //            }))
        //        .OrderByDescending(t => t.LastUpdatedAt)
        //        .Skip(skip)
        //        .Take(count)
        //        .ToList();

        //    return groupedEvents;
        //}

        //public class EventGroup
        //{
        //    public int AppId { get; set; }
        //    public DateTime StartDate { get; set; }
        //    public DateTime LastUpdatedAt { get; set; }
        //    public List<int> DepotIds { get; set; }
        //}

        //public class EventIntervalGroup
        //{
        //    public int IntervalGroup { get; set; }
        //    public EventGroup EventGroup { get; set; }
        //}
    }
}