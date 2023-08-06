namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbSteamAppInfo
    {
        [Key]
        public uint AppId { get; set; }

        public string Name { get; set; }

        public List<DbSteamDepot> Depots { get; set; }
    }
}
