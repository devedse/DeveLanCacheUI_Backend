namespace DeveLanCacheUI_Backend.Services.OriginalDepotEnricher
{
    public class GithubLatestApiPoco
    {
        public required string url { get; set; }
        public required string assets_url { get; set; }
        public required string upload_url { get; set; }
        public required string html_url { get; set; }
        public required int id { get; set; }
        public required Author author { get; set; }
        public required string node_id { get; set; }
        public required string tag_name { get; set; }
        public required string target_commitish { get; set; }
        public required string name { get; set; }
        public required bool draft { get; set; }
        public required bool prerelease { get; set; }
        public required DateTime created_at { get; set; }
        public required DateTime published_at { get; set; }
        public required Asset[] assets { get; set; }
        public required string tarball_url { get; set; }
        public required string zipball_url { get; set; }
        public required string body { get; set; }
    }

    public class Author
    {
        public required string login { get; set; }
        public required int id { get; set; }
        public required string node_id { get; set; }
        public required string avatar_url { get; set; }
        public required string gravatar_id { get; set; }
        public required string url { get; set; }
        public required string html_url { get; set; }
        public required string followers_url { get; set; }
        public required string following_url { get; set; }
        public required string gists_url { get; set; }
        public required string starred_url { get; set; }
        public required string subscriptions_url { get; set; }
        public required string organizations_url { get; set; }
        public required string repos_url { get; set; }
        public required string events_url { get; set; }
        public required string received_events_url { get; set; }
        public required string type { get; set; }
        public required bool site_admin { get; set; }
    }

    public class Asset
    {
        public required string url { get; set; }
        public required int id { get; set; }
        public required string node_id { get; set; }
        public required string name { get; set; }
        public required string label { get; set; }
        public required Uploader uploader { get; set; }
        public required string content_type { get; set; }
        public required string state { get; set; }
        public required int size { get; set; }
        public required int download_count { get; set; }
        public required DateTime created_at { get; set; }
        public required DateTime updated_at { get; set; }
        public required string browser_download_url { get; set; }
    }

    public class Uploader
    {
        public required string login { get; set; }
        public required int id { get; set; }
        public required string node_id { get; set; }
        public required string avatar_url { get; set; }
        public required string gravatar_id { get; set; }
        public required string url { get; set; }
        public required string html_url { get; set; }
        public required string followers_url { get; set; }
        public required string following_url { get; set; }
        public required string gists_url { get; set; }
        public required string starred_url { get; set; }
        public required string subscriptions_url { get; set; }
        public required string organizations_url { get; set; }
        public required string repos_url { get; set; }
        public required string events_url { get; set; }
        public required string received_events_url { get; set; }
        public required string type { get; set; }
        public required bool site_admin { get; set; }
    }
}
