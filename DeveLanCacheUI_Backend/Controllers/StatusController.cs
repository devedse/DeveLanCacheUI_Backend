namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly ILogger<StatusController> _logger;
        private readonly DeveLanCacheUIDbContext _dbContext;

        public StatusController(ILogger<StatusController> logger, DeveLanCacheUIDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<StatusModel> GetAsync()
        {
            _logger.Log(LogLevel.Information, "### Status Controller Get() called");

            var statusModel = StatusObtainer.GetStatus();

            var depotVersionSetting = await _dbContext.Settings.FirstOrDefaultAsync(t => t.Key == DbSetting.SettingKey_DepotVersion);
            statusModel.DepotVersion = depotVersionSetting?.Value;
            return statusModel;
        }
    }
}
