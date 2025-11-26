namespace DeveLanCacheUI_Backend.Db.DbModels
{
    public class DbAsyncLogEntryProcessingQueueItem
    {
        [Key]
        public int Id { get; set; }

        public LanCacheLogEntryRaw LanCacheLogEntryRaw { get; set; } = null!;
    }
}
