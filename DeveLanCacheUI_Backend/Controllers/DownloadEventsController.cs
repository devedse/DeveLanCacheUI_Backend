namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class DownloadEventsController : ControllerBase
    {
        private readonly DeveLanCacheUIDbContext _dbContext;
        private readonly ISteamAppObtainerService _steamAppObtainerService;
        private readonly DeveLanCacheConfiguration _config;
        private readonly ILogger<DownloadEventsController> _logger;

        public DownloadEventsController(
            DeveLanCacheUIDbContext dbContext,
            ISteamAppObtainerService steamAppObtainerService,
            DeveLanCacheConfiguration config,
            ILogger<DownloadEventsController> logger)
        {
            _dbContext = dbContext;
            _steamAppObtainerService = steamAppObtainerService;
            _config = config;
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
            var excludedIps = _config.ExcludedClientIpsArray ?? Array.Empty<string>();

            IQueryable<DbDownloadEvent> tmpResult = _dbContext.DownloadEvents;

            tmpResult = tmpResult.Where(t => !excludedIps.Contains(t.ClientIp));

            if (filter != null)
            {
                tmpResult = tmpResult.Where(t => t.CacheIdentifier == filter);
            }
            tmpResult = tmpResult.Where(t => t.CacheHitBytes != 0 || t.CacheMissBytes != 0);

            var pagedSubQuery = tmpResult.OrderByDescending(t => t.LastUpdatedAt).Skip(skip).Take(count);

            var query = from downloadEvent in pagedSubQuery
                        join steamDepot in _dbContext.SteamDepots on downloadEvent.DownloadIdentifier equals steamDepot.SteamDepotId into steamDepotJoin
                        from steamDepot in steamDepotJoin.DefaultIfEmpty()
                        join epicManifest in _dbContext.EpicManifests on downloadEvent.DownloadIdentifierString equals epicManifest.DownloadIdentifier into epicManifestJoin
                        from epicManifest in epicManifestJoin.DefaultIfEmpty()
                        let steamManifest = (from sm in _dbContext.SteamManifests
                                             where sm.DepotId == downloadEvent.DownloadIdentifier
                                             orderby sm.CreationTime descending
                                             select sm).FirstOrDefault()
                        orderby downloadEvent.LastUpdatedAt descending
                        select new
                        {
                            downloadEvent,
                            steamDepot,
                            epicManifest,
                            steamManifest
                        };

            //var queryString = query.ToQueryString();

            var result = await query.ToListAsync();

            var groupedResult = result.GroupBy(t => t.downloadEvent);

            var mappedResult = groupedResult.Select(group =>
            {
                var firstItemInGroup = group
                  .FirstOrDefault(item => item.steamDepot != null)
                  ?? group.FirstOrDefault(item => item.epicManifest != null)
                  ?? group.First();

                var downloadInfo = new DownloadInfo();

                if (group.Key.CacheIdentifier == "steam")
                {
                    downloadInfo.Name = _steamAppObtainerService.GetSteamAppById(firstItemInGroup.steamDepot.SteamAppId)?.name;
                    downloadInfo.ClickUrl = $"https://steamdb.info/depot/{group.Key.DownloadIdentifierString}/";
                    downloadInfo.ImageUrl = $"https://cdn.cloudflare.steamstatic.com/steam/apps/{firstItemInGroup.steamDepot.SteamAppId}/header.jpg";
                    downloadInfo.TotalBytes = firstItemInGroup.steamManifest?.TotalCompressedSize ?? 0 + firstItemInGroup.steamManifest?.ManifestBytesSize ?? 0;
                }
                else if (group.Key.CacheIdentifier == "epicgames")
                {
                    downloadInfo.Name = firstItemInGroup.epicManifest?.Name;
                    downloadInfo.TotalBytes = firstItemInGroup.epicManifest?.TotalCompressedSize ?? 0 + firstItemInGroup.epicManifest?.ManifestBytesSize ?? 0;
                }

                var downloadEvent = new DownloadEvent
                {
                    Id = group.Key.Id,
                    CacheIdentifier = group.Key.CacheIdentifier,
                    DownloadIdentifier = group.Key.DownloadIdentifier,
                    DownloadIdentifierString = group.Key.DownloadIdentifierString,
                    ClientIp = group.Key.ClientIp,
                    CreatedAt = group.Key.CreatedAt,
                    LastUpdatedAt = group.Key.LastUpdatedAt,
                    CacheHitBytes = group.Key.CacheHitBytes,
                    CacheMissBytes = group.Key.CacheMissBytes,
                    //TotalBytes = (firstItemInGroup.steamManifest?.TotalCompressedSize ?? 0) + (firstItemInGroup.steamManifest?.ManifestBytesSize ?? 0),
                    //SteamDepot = group.Key.CacheIdentifier != "steam" || firstItemInGroup.steamDepot == null
                    //    ? null
                    //    : new SteamDepot
                    //    {
                    //        Id = firstItemInGroup.steamDepot.SteamDepotId,
                    //        SteamAppId = firstItemInGroup.steamDepot.SteamAppId
                    //    }
                    DownloadInfo = downloadInfo
                };

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