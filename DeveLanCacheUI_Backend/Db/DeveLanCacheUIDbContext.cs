namespace DeveLanCacheUI_Backend.Db
{
    public class DeveLanCacheUIDbContext : DbContext
    {
        public DbSet<DbSteamDepot> SteamDepots => Set<DbSteamDepot>();
        public DbSet<DbDownloadEvent> DownloadEvents => Set<DbDownloadEvent>();
        public DbSet<DbSetting> Settings => Set<DbSetting>();
        public DbSet<DbSteamManifest> SteamManifests => Set<DbSteamManifest>();
        public DbSet<DbAsyncLogEntryProcessingQueueItem> ManifestAsyncDownloadProcessingQueueItems => Set<DbAsyncLogEntryProcessingQueueItem>();


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

            modelBuilder.Entity<DbAsyncLogEntryProcessingQueueItem>()
                        .HasIndex(pc => pc.LanCacheLogEntryRaw);
        }
    }
}
