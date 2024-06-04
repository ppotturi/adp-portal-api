using Microsoft.Extensions.Caching.Memory;

namespace ADP.Portal.Core.Git.Services;
public class CacheService : ICacheService
{
    private readonly TimeProvider clock;
    private readonly IMemoryCache cache;

    public CacheService(TimeProvider clock, IMemoryCache cache)
    {
        this.clock = clock;
        this.cache = cache;
    }

    public T? Get<T>(string key)
    {
        return cache.Get<T>(key) ?? default;
    }

    public void Set<T>(string key, T value)
    {
        cache.Set(key, value, new MemoryCacheEntryOptions().SetAbsoluteExpiration(CalculateExpiration()));
    }

    private TimeSpan CalculateExpiration()
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var nextMidnight = now.AddDays(1).Date;
        return nextMidnight - now;
    }
}
