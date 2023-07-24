using DeveLanCacheUI_Backend.Db;
using DeveLanCacheUI_Backend.Db.DbModels;
using DeveLanCacheUI_Backend.LogReading.Models;
using Microsoft.EntityFrameworkCore;
using Polly;
using SteamKit2;
using System.Text.Json;

namespace DeveLanCacheUI_Backend.SteamProto
{
    public class SteamManifestService
    {
        private const bool StoreSteamDbProtoManifestBytesInDb = true;

        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _services;
        private readonly IHttpClientFactory _httpClientFactoryForManifestDownloads;
        private readonly ILogger<SteamManifestService> _logger;
        private readonly string _manifestDirectory;

        public SteamManifestService(IConfiguration configuration, IServiceProvider services, IHttpClientFactory httpClientFactory, ILogger<SteamManifestService> logger)
        {
            _configuration = configuration;
            _services = services;
            _httpClientFactoryForManifestDownloads = httpClientFactory;
            _logger = logger;

            var deveLanCacheUIDataDirectory = configuration.GetValue<string>("DeveLanCacheUIDataDirectory") ?? string.Empty;
            _manifestDirectory = Path.Combine(deveLanCacheUIDataDirectory, "manifests");
        }

        public void TryToDownloadManifest(LanCacheLogEntryRaw lanCacheLogEntryRaw)
        {
            if (!lanCacheLogEntryRaw.Request.Contains("/manifest/") || lanCacheLogEntryRaw.DownloadIdentifier == null)
            {
                _logger.LogError($"Code bug: Trying to download manifest that isn't actually a manifest: {lanCacheLogEntryRaw.OriginalLogLine}");
                return;
            }

            _ = Task.Run(async () =>
            {
                var fallbackPolicy = Policy
                    .Handle<Exception>()
                    .FallbackAsync(async (ct) =>
                    {
                        Console.WriteLine($"Manifest saving: All retries failed, skipping...");
                    });

                var retryPolicy = Policy
                   .Handle<Exception>()
                   .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                   (exception, timeSpan, context) =>
                   {
                       Console.WriteLine($"Manifest saving: An error occurred while trying to save changes: {exception.Message}");
                   });

                await fallbackPolicy.WrapAsync(retryPolicy).ExecuteAsync(async () =>
                {
                    await using (var scope = _services.CreateAsyncScope())
                    {
                        var theManifestUrlPart = lanCacheLogEntryRaw.Request.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1];

                        var uniqueManifestIdentifier = theManifestUrlPart.Split("/manifest/", StringSplitOptions.RemoveEmptyEntries).Last();

                        //Replace invalid chars should dissalow reading any file you want :)
                        var uniqueManifestIdentifierFileName = ReplaceInvalidChars(uniqueManifestIdentifier) + ".bin";
                        var depotId = ReplaceInvalidChars(lanCacheLogEntryRaw.DownloadIdentifier!);
                        var depotIdAndManifestIdentifier = Path.Combine(depotId, uniqueManifestIdentifierFileName);


                        using var dbContext = scope.ServiceProvider.GetRequiredService<DeveLanCacheUIDbContext>();
                        var dbManifestFound = await dbContext.SteamManifests.FirstOrDefaultAsync(t => t.UniqueManifestIdentifier == depotIdAndManifestIdentifier);

                        if (dbManifestFound != null)
                        {
                            return;
                        }

                        var fullPath = Path.Combine(_manifestDirectory, depotIdAndManifestIdentifier);
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                        var url = $"http://{lanCacheLogEntryRaw.Host}{theManifestUrlPart}";
                        using var httpClient = _httpClientFactoryForManifestDownloads.CreateClient();
                        var manifestResponse = await httpClient.GetAsync(url);
                        if (!manifestResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Warning: Tried to obtain manifest for: {lanCacheLogEntryRaw.DownloadIdentifier} but status code was: {manifestResponse.StatusCode}");
                        }
                        var manifestBytes = await manifestResponse.Content.ReadAsByteArrayAsync();
                        var dbManifest = ManifestBytesToDbSteamManifest(manifestBytes, depotIdAndManifestIdentifier);

                        if (dbManifest == null)
                        {
                            Console.WriteLine($"Waring: Could not get manifest for depot: {lanCacheLogEntryRaw.DownloadIdentifier}");
                        }

                        var dbValue = dbContext.SteamManifests.FirstOrDefault(t => t.DepotId == dbManifest.DepotId && t.CreationTime == dbManifest.CreationTime);
                        if (dbValue != null)
                        {
                            dbContext.Entry(dbValue).CurrentValues.SetValues(dbManifest);
                            Console.WriteLine($"Info: Updated manifest for {lanCacheLogEntryRaw.DownloadIdentifier}");
                        }
                        else
                        {
                            await dbContext.SteamManifests.AddAsync(dbManifest);
                            Console.WriteLine($"Info: Added manifest for {lanCacheLogEntryRaw.DownloadIdentifier}");
                        }

                        await File.WriteAllBytesAsync(fullPath, manifestBytes);
                        await dbContext.SaveChangesAsync();
                    }
                });
            });
        }

        private string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars())).Replace(".", "");
        }

        private DbSteamManifest? ManifestBytesToDbSteamManifest(byte[] manifestBytes, string uniqueManifestIdentifier)
        {
            var bytesDecompressed = ZipUtil.Decompress(manifestBytes);
            var depotManifest = DepotManifest.Deserialize(bytesDecompressed);

            if (depotManifest == null)
            {
                return null;
            }

            var dbSteamManifest = new DbSteamManifest()
            {
                DepotId = (int)depotManifest.DepotID,
                CreationTime = depotManifest.CreationTime,
                TotalCompressedSize = depotManifest.TotalCompressedSize,
                TotalUncompressedSize = depotManifest.TotalUncompressedSize,
                ManifestBytesSize = (ulong)manifestBytes.LongLength,
                UniqueManifestIdentifier = uniqueManifestIdentifier
            };

            return dbSteamManifest;
        }

        public static string ManifestBytesToJson(byte[] manifestBytes, bool indented)
        {
            var bytesDecompressed = ZipUtil.Decompress(manifestBytes);
            var depotManifest = DepotManifest.Deserialize(bytesDecompressed);
            var json = JsonSerializer.Serialize(depotManifest, new JsonSerializerOptions() { WriteIndented = indented });
            return json;
        }

        public static JsonDocument ManifestBytesToJsonValue(byte[] manifestBytes)
        {
            var json = ManifestBytesToJson(manifestBytes, false);
            var document = JsonDocument.Parse(json);
            return document;
        }

        //public string DecodeBase64(byte[] data)
        //{
        //    string chunkIDHex = BitConverter.ToString(data).Replace("-", "").ToLower();
        //    return chunkIDHex;
        //}

        public byte[]? GetBytesForUniqueManifestIdentifier(string uniqueManifestIdentifier)
        {
            var fullPath = Path.Combine(_manifestDirectory, uniqueManifestIdentifier);
            if (File.Exists(fullPath))
            {
                return File.ReadAllBytes(fullPath);
            }
            else
            {
                return null;
            }
        }
    }
}
