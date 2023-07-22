using DeveLanCacheUI_Backend.Controllers.Models;
using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SteamManifestController : ControllerBase
    {
        private readonly DeveLanCacheUIDbContext _dbContext;
        private readonly ILogger<SteamManifestController> _logger;

        public SteamManifestController(DeveLanCacheUIDbContext dbContext, ILogger<SteamManifestController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<DbSteamManifest>> GetSteamManifests()
        {
            var steamManifests = await _dbContext.SteamManifests
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync();

            return steamManifests;
        }
    }
}