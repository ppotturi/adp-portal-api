using Microsoft.Extensions.Caching.Memory;

namespace ADP.Portal.Core.Git.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache cache;
        private readonly TimeSpan defaultExpiration;

        public CacheService(IMemoryCache cache)
        {
            this.cache = cache;
            this.defaultExpiration = CalculateExpiration();
        }

        public T? Get<T>(string key)
        {
            return cache.Get<T>(key) ?? default;
        }

        public void Set<T>(string key, T value)
        {
            cache.Set(key, value, new MemoryCacheEntryOptions().SetAbsoluteExpiration(defaultExpiration));
        }

        private static TimeSpan CalculateExpiration()
        {
            DateTime now = DateTime.Now;
            DateTime nextMidnight = now.Date.AddDays(1);
            return nextMidnight - now;
        }
    }
}
