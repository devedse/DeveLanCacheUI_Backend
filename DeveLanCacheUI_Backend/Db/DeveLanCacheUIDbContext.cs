using DeveLanCacheUI_Backend.Db.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Db
{
    public class DeveLanCacheUIDbContext : DbContext
    {
        public DbSet<DbSteamApp> SteamApps => Set<DbSteamApp>();
        public DbSet<DbSteamDepot> SteamDepots => Set<DbSteamDepot>();
        public DbSet<DbSteamAppDownloadEvent> SteamAppDownloadEvents => Set<DbSteamAppDownloadEvent>();

        public DeveLanCacheUIDbContext(DbContextOptions options) : base (options)
        {

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
