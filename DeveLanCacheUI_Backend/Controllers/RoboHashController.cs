namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RoboHashController : ControllerBase
    {
        private readonly ILogger<RoboHashController> _logger;
        private readonly RoboHashCache _roboHashCache;

        public RoboHashController(ILogger<RoboHashController> logger, RoboHashCache roboHashCache)
        {
            _logger = logger;
            _roboHashCache = roboHashCache;
        }

        [HttpGet("{text}")]
        [ResponseCache(CacheProfileName = "ForeverCache")]
        public IActionResult GetRoboHash(string text, string? roboSet = null, string? color = null, string? format = "png", string? bgSet = null, int sizeX = 300, int sizeY = 300)
        {
            byte[] data;

            var key = $"{text}_{roboSet}_{color}_{format}_{bgSet}_{sizeX}_{sizeY}";

            data = _roboHashCache.Cache.GetOrCreate(key, cacheEntry =>
            {
                cacheEntry.SlidingExpiration = TimeSpan.FromDays(1); // Set expiration time as per your requirements

                var roboHashGenerator = new RoboHashGenerator();
                var image = roboHashGenerator.Assemble(text, roboSet, color, format, bgSet, sizeX, sizeY);

                var outputStream = new MemoryStream();
                switch (format?.ToLower())
                {
                    case "jpg":
                        image.SaveAsJpeg(outputStream);
                        break;
                    case "bmp":
                        image.SaveAsBmp(outputStream);
                        break;
                    case "png":
                    default:
                        image.SaveAsPng(outputStream);
                        break;
                }

                var imageData = outputStream.ToArray();

                cacheEntry.SetSize(imageData.Length);

                return imageData;
            })!;

            return new FileContentResult(data, GetMimeType(format));
        }

        private string GetMimeType(string? format)
        {
            return format?.ToLower() switch
            {
                "jpg" => "image/jpeg",
                "bmp" => "image/bmp",
                "png" => "image/png",
                _ => "application/octet-stream",
            };
        }
    }
}
