using DeveHashImageGenerator.RoboHash;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;

namespace DeveLanCacheUI_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RoboHashController : ControllerBase
    {
        private readonly ILogger<RoboHashController> _logger;

        public RoboHashController(ILogger<RoboHashController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{text}")]
        public IActionResult GetRoboHash(string text, string? roboSet = null, string? color = null, string? format = "png", string? bgSet = null, int sizeX = 300, int sizeY = 300)
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
                        return File(outputStream.ToArray(), "image/jpeg");
                    case "bmp":
                        image.SaveAsBmp(outputStream);
                        return File(outputStream.ToArray(), "image/bmp");
                    case "png":
                    default:
                        image.SaveAsPng(outputStream);
                        return File(outputStream.ToArray(), "image/png");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while generating RoboHash image");
                return BadRequest("Failed to generate image");
            }
        }
    }
}