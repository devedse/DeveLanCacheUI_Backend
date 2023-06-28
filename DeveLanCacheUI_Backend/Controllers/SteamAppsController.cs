using DeveLanCacheUI_Backend.Db;
using Microsoft.AspNetCore.Mvc;

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
            this._dbContext = dbContext;
            _logger = logger;
        }

        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}