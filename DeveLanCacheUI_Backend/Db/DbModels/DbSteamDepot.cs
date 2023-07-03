using DeveLanCacheUI_Backend.Steam;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbSteamDepot
    {
        [Key]
        public int Id { get; set; }

        [NotMapped]
        //This will be filled by the API (hacky yes, but it works)
        public App? SteamApp { get; set; }
        public int? SteamAppId { get; set; }

        [JsonIgnore]
        public virtual ICollection<DbSteamAppDownloadEvent> DownloadEvents { get; set; }
    }
}
