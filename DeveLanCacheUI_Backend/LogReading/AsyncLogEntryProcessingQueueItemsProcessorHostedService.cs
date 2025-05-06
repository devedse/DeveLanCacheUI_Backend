using DeveLanCacheUI_Backend.Services;
using ProtoBuf.Meta;

namespace DeveLanCacheUI_Backend.LogReading
{
    public class AsyncLogEntryProcessingQueueItemsProcessorHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<AsyncLogEntryProcessingQueueItemsProcessorHostedService> _logger;

        public AsyncLogEntryProcessingQueueItemsProcessorHostedService(
            IServiceProvider services,
            ILogger<AsyncLogEntryProcessingQueueItemsProcessorHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            await GoRun(stoppingToken);
        }

        private async Task GoRun(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await using (var scope = _services.CreateAsyncScope())
                {
                    using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();

                    var items = await dbContext.AsyncLogEntryProcessingQueueItems.OrderBy(t => t.Id).ToListAsync(stoppingToken);
                    if (items.Count == 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        continue;
                    }

                    var steamManifestService = scope.ServiceProvider.GetRequiredService<SteamManifestService>();
                    var epicManifestService = scope.ServiceProvider.GetRequiredService<EpicManifestService>();

                    foreach (var item in items)
                    {
                        if (item.LanCacheLogEntryRaw.CacheIdentifier == "steam")
                        {
                            await steamManifestService.TryToDownloadManifest(item.LanCacheLogEntryRaw);
                        }
                        if (item.LanCacheLogEntryRaw.CacheIdentifier == "epicgames")
                        {
                            await epicManifestService.TryToDownloadManifest(item.LanCacheLogEntryRaw);
                        }
                    }

                    dbContext.AsyncLogEntryProcessingQueueItems.RemoveRange(items);
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
        }
    }
}
