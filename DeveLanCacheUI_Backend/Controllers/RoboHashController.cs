using DeveHashImageGenerator.RoboHash;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using System.Collections.Concurrent;

namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RoboHashController : ControllerBase
    {
        private readonly ILogger<RoboHashController> _logger;
        private static ConcurrentDictionary<string, byte[]> _imagesCache = new ConcurrentDictionary<string, byte[]>();

        public RoboHashController(ILogger<RoboHashController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{text}")]
        public IActionResult GetRoboHash(string text, string? roboSet = null, string? color = null, string? format = "png", string? bgSet = null, int sizeX = 300, int sizeY = 300)
        {
            byte[] data;

            var key = $"{text}_{roboSet}_{color}_{format}_{bgSet}_{sizeX}_{sizeY}";
            if (_imagesCache.TryGetValue(key, out data))
            {
                return new FileContentResult(data, GetMimeType(format));
            }
            else
            {
                try
                {
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

                    data = outputStream.ToArray();

                    _imagesCache.TryAdd(key, data); // Adding to cache

                    return new FileContentResult(data, GetMimeType(format));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occurred while generating RoboHash image");
                    return BadRequest("Failed to generate image");
                }
            }
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