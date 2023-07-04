using DeveLanCacheUI_Backend.Status;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly ILogger<StatusController> _logger;

        public StatusController(ILogger<StatusController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public Task<StatusModel> GetAsync()
        {
            _logger.Log(LogLevel.Information, "### Status Controller Get() called");

            var statusModel = StatusObtainer.GetStatus();
            return Task.FromResult(statusModel);
        }
    }
}
