using DeveLanCacheUI_Backend.Db.DbModels;
using DeveLanCacheUI_Backend.Steam;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Db
{
    public class DeveLanCacheUIDbContext : DbContext
    {
        public DbSet<DbSteamApp> SteamApps => Set<DbSteamApp>();
        public DbSet<DbSteamDepot> SteamDepots => Set<DbSteamDepot>();
        public DbSet<DbSteamAppDownloadEvent> SteamAppDownloadEvents => Set<DbSteamAppDownloadEvent>();

        public DeveLanCacheUIDbContext(DbContextOptions options) : base(options)
        {

        }

        public async Task SeedDataAsync(int appId, params int[] depotIds)
        {
            var existingApp = await SteamApps.FirstOrDefaultAsync(a => a.Id == appId);

            if (existingApp == null)
            {
                var newApp = new DbSteamApp
                {
                    Id = appId,
                    AppName = SteamApi.SteamApiData?.applist?.apps?.FirstOrDefault(t => t.appid == appId)?.name ?? "NameNotFound",
                };

                Console.WriteLine($"Seeding {newApp.AppName} ({appId}), depots: {string.Join(", ", depotIds)})");

                await SteamApps.AddAsync(newApp);
                await SaveChangesAsync();
            }

            foreach (var depotId in depotIds)
            {
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
            modelBuilder.Entity<DbSteamApp>()
                .Property(t => t.Id)
                .ValueGeneratedNever();

            base.OnModelCreating(modelBuilder);
        }
    }
}
