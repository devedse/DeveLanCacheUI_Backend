namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbSetting
    {
        [Key]
        public required string Key { get; set; }
        public string? Value { get; set; }

        public const string SettingKey_DepotVersion = nameof(SettingKey_DepotVersion);
        public const string SettingKey_SteamChangeNumber = nameof(SettingKey_SteamChangeNumber);
        public const string SettingKey_LastByteRead = nameof(SettingKey_LastByteRead);
    }
}
