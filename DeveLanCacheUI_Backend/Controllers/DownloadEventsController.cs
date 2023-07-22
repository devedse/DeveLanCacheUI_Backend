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
                tmpResult = tmpResult.Where(t => t.CacheIdentifier == filter);
            }
            tmpResult = tmpResult.Where(t => t.CacheHitBytes != 0 || t.CacheMissBytes != 0);

            var query = from downloadEvent in tmpResult
                        join steamDepot in _dbContext.SteamDepots on downloadEvent.DownloadIdentifier equals steamDepot.Id into steamDepotJoin
                        from steamDepot in steamDepotJoin.DefaultIfEmpty()
                        join steamManifest in _dbContext.SteamManifests on downloadEvent.DownloadIdentifier equals steamManifest.DepotId into steamManifestJoin
                        from steamManifest in steamManifestJoin.DefaultIfEmpty()
                        orderby downloadEvent.LastUpdatedAt descending
                        select new
                        {
                            downloadEvent,
                            steamDepot,
                            steamManifest
                        };
            query = query.Skip(skip).Take(count);

            //var queryString = query.ToQueryString();

            var result = await query.ToListAsync();

            var mappedResult = result.Select(item =>
            {
                var steamManifest = item.steamManifest != null && item.steamManifest.CreationTime == result.Where(t => t.steamManifest != null).Max(t => t.steamManifest.CreationTime) ? item.steamManifest : null;
                var downloadEvent = new DownloadEvent
                {
                    Id = item.downloadEvent.Id,
                    CacheIdentifier = item.downloadEvent.CacheIdentifier,
                    DownloadIdentifier = item.downloadEvent.DownloadIdentifier,
                    DownloadIdentifierString = item.downloadEvent.DownloadIdentifierString,
                    ClientIp = item.downloadEvent.ClientIp,
                    CreatedAt = item.downloadEvent.CreatedAt,
                    LastUpdatedAt = item.downloadEvent.LastUpdatedAt,
                    CacheHitBytes = item.downloadEvent.CacheHitBytes,
                    CacheMissBytes = item.downloadEvent.CacheMissBytes,
                    TotalBytes = (steamManifest?.TotalCompressedSize ?? 0) + (steamManifest?.ManifestBytesSize ?? 0),
                    SteamDepot = item.steamDepot == null
                        ? null
                        : new SteamDepot
                        {
                            Id = item.steamDepot.Id,
                            SteamAppId = item.steamDepot.SteamAppId
                        }
                };

                if (downloadEvent.CacheIdentifier == "steam" && downloadEvent.SteamDepot != null)
                {
                    downloadEvent.SteamDepot.SteamApp = SteamApi.SteamApiData?.applist?.apps?.FirstOrDefault(t => t?.appid == downloadEvent.SteamDepot.SteamAppId);
                }

                return downloadEvent;
            }).ToList();

            return mappedResult;
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