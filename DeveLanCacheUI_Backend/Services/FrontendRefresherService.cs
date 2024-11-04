namespace DeveLanCacheUI_Backend.Services
{
    public class FrontendRefresherService : BackgroundService
    {
        private readonly ILogger<FrontendRefresherService> _logger;
        private readonly IHubContext<LanCacheHub> _lanCacheHubContext;

        private static long _frontendRefreshRequired;

        public FrontendRefresherService(ILogger<FrontendRefresherService> logger, IHubContext<LanCacheHub> lanCacheHubContext)
        {
            _logger = logger;
            _lanCacheHubContext = lanCacheHubContext;
        }

        public static void RequireFrontendRefresh()
        {
            Interlocked.Increment(ref _frontendRefreshRequired);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            long lastFrontendRefresh = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                var curNumber = Interlocked.Read(ref _frontendRefreshRequired);

                if (curNumber != lastFrontendRefresh)
                {
                    await _lanCacheHubContext.Clients.All.SendAsync("UpdateDownloadEvents");
                    lastFrontendRefresh = curNumber;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
