using Microsoft.Extensions.Caching.Memory;

namespace DeveLanCacheUI_Backend.DeveHashImageGeneratorStuff
{
    public class RoboHashCache
    {
        public IMemoryCache Cache { get; }

        public RoboHashCache()
        {
            Cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 1024 * 1024 * 100 // 100MB
            });
        }
    }
}
