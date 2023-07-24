using DeveLanCacheUI_Backend.Controllers.Models;
using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.SteamProto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class SteamManifestController : ControllerBase
    {
        private readonly DeveLanCacheUIDbContext _dbContext;
        private readonly SteamManifestService _steamManifestService;
        private readonly ILogger<SteamManifestController> _logger;

        public SteamManifestController(DeveLanCacheUIDbContext dbContext, SteamManifestService steamManifestService, ILogger<SteamManifestController> logger)
        {
            _dbContext = dbContext;
            _steamManifestService = steamManifestService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<SteamManifest>> GetSteamManifests(int skip = 0, int take = 10)
        {
            var steamManifests = await _dbContext.SteamManifests
                .OrderByDescending(t => t.CreationTime)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            var mapped = steamManifests
                .Select(t =>
                {
                    var originalProtoBytes = _steamManifestService.GetBytesForUniqueManifestIdentifier(t.UniqueManifestIdentifier);
                    var jsonifiedProtoBytes = originalProtoBytes != null ? SteamManifestService.ManifestBytesToJsonValue(originalProtoBytes) : null;

                    var retval = new SteamManifest()
                    {
                        CreationTime = t.CreationTime,
                        DepotId = t.DepotId,
                        UniqueManifestIdentifier = t.UniqueManifestIdentifier,
                        ManifestBytesSize = t.ManifestBytesSize,
                        ProtobufDataAsJson = jsonifiedProtoBytes,
                        TotalCompressedSize = t.TotalCompressedSize,
                        TotalUncompressedSize = t.TotalUncompressedSize,
                    };
                    return retval;
                }).ToList();
            return mapped;
        }

        [HttpGet]
        public async Task<IEnumerable<SteamManifest>> GetSteamManifestsForDepot(int depotId, int skip = 0, int take = 10)
        {
            var steamManifests = await _dbContext.SteamManifests
                .Where(t => t.DepotId == depotId)
                .OrderByDescending(t => t.CreationTime)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            var mapped = steamManifests
                .Select(t =>
                {
                    var originalProtoBytes = _steamManifestService.GetBytesForUniqueManifestIdentifier(t.UniqueManifestIdentifier);
                    var jsonifiedProtoBytes = originalProtoBytes != null ? SteamManifestService.ManifestBytesToJsonValue(originalProtoBytes) : null;

                    var retval = new SteamManifest()
                    {
                        CreationTime = t.CreationTime,
                        DepotId = t.DepotId,
                        UniqueManifestIdentifier = t.UniqueManifestIdentifier,
                        ManifestBytesSize = t.ManifestBytesSize,
                        ProtobufDataAsJson = jsonifiedProtoBytes,
                        TotalCompressedSize = t.TotalCompressedSize,
                        TotalUncompressedSize = t.TotalUncompressedSize,
                    };
                    return retval;
                }).ToList();
            return mapped;
        }
    }
}