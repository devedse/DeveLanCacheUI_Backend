namespace DeveLanCacheUI_Backend.Db
{
    public class DeveLanCacheUIDbContext : DbContext
    {
        public DbSet<SteamAppInfo> SteamApps => Set<SteamAppInfo>();
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

            modelBuilder.Entity<SteamAppInfo>()
                        .HasMany(p => p.Depots)        
                        .WithOne(c => c.OwningApp)         
                        .HasForeignKey(c => c.SteamAppId);

            modelBuilder.Entity<DbSteamDepot>()
                        .HasKey(pc => new { pc.Id, pc.SteamAppId });
        }
    }
}
