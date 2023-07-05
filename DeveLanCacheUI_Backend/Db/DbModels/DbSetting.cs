namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbSetting
    {
        [Key]
        public required string Key { get; set; }
        public string? Value { get; set; }

        public const string SettingKey_DepotVersion = nameof(SettingKey_DepotVersion);
    }
}
