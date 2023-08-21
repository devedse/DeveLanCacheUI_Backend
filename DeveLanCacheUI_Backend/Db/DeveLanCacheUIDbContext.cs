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

            modelBuilder.Entity<DbSteamDepot>()
                        .HasKey(pc => new { pc.SteamDepotId, pc.SteamAppId });
        }
    }
}
