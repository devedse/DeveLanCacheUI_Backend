using DeveLanCacheUI_Backend.Db.DbModels;
using DeveLanCacheUI_Backend.Steam;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Db
{
    public class DeveLanCacheUIDbContext : DbContext
    {
        public DbSet<DbSteamDepot> SteamDepots => Set<DbSteamDepot>();
        public DbSet<DbDownloadEvent> DownloadEvents => Set<DbDownloadEvent>();

        public DeveLanCacheUIDbContext(DbContextOptions options) : base(options)
        {

        }

        public async Task SeedDataAsync(int appId, params int[] depotIds)
        {
            var appName = SteamApi.SteamApiData?.applist?.apps?.FirstOrDefault(t => t.appid == appId)?.name ?? "NameNotFound";


            foreach (var depotId in depotIds)
            {
                Console.WriteLine($"Seeding {appName} ({appId}), depots: {depotId})");

                var existingDepot = await SteamDepots.FirstOrDefaultAsync(d => d.Id == depotId);
                if (existingDepot != null)
                {
                    if (existingDepot.SteamAppId != appId)
                    {
                        existingDepot.SteamAppId = appId;
                    }
                }
                else
                {
                    var newDepot = new DbSteamDepot
                    {
                        Id = depotId,
                        SteamAppId = appId
                    };

                    await SteamDepots.AddAsync(newDepot);
                }
            }
            await SaveChangesAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
