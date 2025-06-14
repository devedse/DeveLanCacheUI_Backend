using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DeveLanCacheUI_Backend.Db
{
    public class DeveLanCacheUIDbContext : DbContext
    {
        public DbSet<DbSteamDepot> SteamDepots => Set<DbSteamDepot>();
        public DbSet<DbDownloadEvent> DownloadEvents => Set<DbDownloadEvent>();
        public DbSet<DbSetting> Settings => Set<DbSetting>();
        public DbSet<DbSteamManifest> SteamManifests => Set<DbSteamManifest>();

        public DeveLanCacheUIDbContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DbDownloadEvent>()
                        .HasIndex(t => t.ClientIp);

            modelBuilder.Entity<DbDownloadEvent>()
                        .HasIndex(t => t.CacheIdentifier);

            modelBuilder.Entity<DbSteamDepot>()
                        .HasKey(pc => new { pc.SteamDepotId, pc.SteamAppId });
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            ArgumentNullException.ThrowIfNull(configurationBuilder);

            configurationBuilder.Properties<DateTime>().HaveConversion<DateTimeAsUtcValueConverter>();
            configurationBuilder.Properties<DateTime?>().HaveConversion<NullableDateTimeAsUtcValueConverter>();
        }
    }

    public class NullableDateTimeAsUtcValueConverter() : ValueConverter<DateTime?, DateTime?>(
    v => !v.HasValue ? v : ToUtc(v.Value), v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
        private static DateTime? ToUtc(DateTime v) => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime();
    }

    public class DateTimeAsUtcValueConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

}
