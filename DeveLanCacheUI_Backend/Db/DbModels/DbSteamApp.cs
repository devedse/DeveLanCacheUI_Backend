using System.ComponentModel.DataAnnotations;

namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbSteamApp
    {
        [Key]
        public int Id { get; set; }
        public int AppName { get; set; }
    }
}
