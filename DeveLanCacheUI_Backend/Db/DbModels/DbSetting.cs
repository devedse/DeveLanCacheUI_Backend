using System.ComponentModel.DataAnnotations;

namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbSetting
    {
        [Key]
        public int Id { get; set; }
        public required string Key { get; set; }
        public string? Value { get; set; }

        public const string SettingKey_DepotVersion = nameof(SettingKey_DepotVersion);
    }
}
