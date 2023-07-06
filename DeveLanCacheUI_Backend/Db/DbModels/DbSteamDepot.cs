using System.ComponentModel.DataAnnotations;

namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbSteamDepot
    {
        [Key]
        public int Id { get; set; }

        public int? SteamAppId { get; set; }
    }
}
