namespace DeveLanCacheUI_Backend.Db
{
    public class DeveLanCacheUIDbContext : DbContext
    {
        public DbSet<DbSteamDepot> SteamDepots => Set<DbSteamDepot>();
        public DbSet<DbDownloadEvent> DownloadEvents => Set<DbDownloadEvent>();
        public DbSet<DbSetting> Settings => Set<DbSetting>();
        public DbSet<DbSteamManifest> SteamManifests => Set<DbSteamManifest>();
        public DbSet<DbEpicManifest> EpicManifests => Set<DbEpicManifest>();
        public DbSet<DbAsyncLogEntryProcessingQueueItem> AsyncLogEntryProcessingQueueItems => Set<DbAsyncLogEntryProcessingQueueItem>();


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

            // Configure the owned entity to make eligible properties optional
            modelBuilder.Entity<DbAsyncLogEntryProcessingQueueItem>()
                        .OwnsOne(pc => pc.LanCacheLogEntryRaw, ownedBuilder =>
                        {
                            // Define which properties should remain required regardless of type
                            var requiredProperties = new HashSet<string> { "CacheIdentifier", "OriginalLogLine" };

                            // Process each property
                            foreach (var property in typeof(LanCacheLogEntryRaw).GetProperties())
                            {
                                // Skip required properties
                                if (requiredProperties.Contains(property.Name))
                                    continue;

                                var propertyType = property.PropertyType;

                                // Check if the property type can be nullable in the database:
                                // 1. Reference types (string, classes, etc.)
                                // 2. Already nullable value types (int?, DateTime?, etc.)
                                bool canBeNullable = !propertyType.IsValueType ||
                                                    (propertyType.IsGenericType &&
                                                     propertyType.GetGenericTypeDefinition() == typeof(Nullable<>));

                                if (canBeNullable)
                                {
                                    // Only configure properties that can be nullable
                                    ownedBuilder.Property(propertyType, property.Name).IsRequired(false);
                                }
                            }
                        });
        }
    }
}
