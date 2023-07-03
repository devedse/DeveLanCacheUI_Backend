using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using DeveLanCacheUI_Backend.LogReading.Models;
using DeveLanCacheUI_Backend.Steam;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.LogReading
{
    public class SteamDepotEnricherHostedService : BackgroundService
    {
        public IServiceProvider Services { get; }

        private readonly IConfiguration _configuration;
        private readonly ILogger<SteamDepotEnricherHostedService> _logger;

        public SteamDepotEnricherHostedService(IServiceProvider services,
            IConfiguration configuration,
            ILogger<SteamDepotEnricherHostedService> logger)
        {
            Services = services;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var logFilePath = _configuration.GetValue<string>("LanCacheLogsDirectory")!;
            if (logFilePath == null)
            {
                throw new NullReferenceException("LogPath == null, please ensure the LogPath ENVIRONMENT_VARIABLE is filled in");
            }
            var accessLogFilePath = Path.Combine(logFilePath, "access.log");


            
        }
    }
}
