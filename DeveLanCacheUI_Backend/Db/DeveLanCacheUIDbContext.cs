using DeveLanCacheUI_Backend.Db.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DeveLanCacheUI_Backend.Db
{
    public class DeveLanCacheUIDbContext : DbContext
    {
        public DbSet<DbSteamApp> SteamApps => Set<DbSteamApp>();
        public DbSet<DbSteamAppDownloadEvent> SteamAppDownloadEvents => Set<DbSteamAppDownloadEvent>();

        public DeveLanCacheUIDbContext(DbContextOptions options) : base (options)
        {

        }
    }
}
