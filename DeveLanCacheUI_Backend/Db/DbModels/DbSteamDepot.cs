using System.ComponentModel.DataAnnotations.Schema;

namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbSteamDepot
    {
        public uint Id { get; set; }
        public uint SteamAppId { get; set; }

        [ForeignKey(nameof(SteamAppId))]
        public DbSteamAppInfo OwningApp { get; set; } 
    }
}
