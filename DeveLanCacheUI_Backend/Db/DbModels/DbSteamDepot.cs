using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbSteamDepot
    {
        [Key]
        public int Id { get; set; }

        public int? SteamAppId { get; set; }
        public DbSteamApp? SteamApp { get; set; }

        [JsonIgnore]
        public virtual ICollection<DbSteamAppDownloadEvent> DownloadEvents { get; set; }
    }
}
