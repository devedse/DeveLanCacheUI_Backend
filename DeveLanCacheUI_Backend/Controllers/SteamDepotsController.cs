using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SteamDepotsController : ControllerBase
    {
        private readonly DeveLanCacheUIDbContext _dbContext;
        private readonly ILogger<SteamDepotsController> _logger;

        public SteamDepotsController(DeveLanCacheUIDbContext dbContext, ILogger<SteamDepotsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet(Name = "GetUnknownDepotIds")]
        public async Task<IEnumerable<int>> Get()
        {
            return await _dbContext.SteamDepots.Where(t => t.SteamApp == null).Select(t => t.Id).ToListAsync();
        }
    }
}