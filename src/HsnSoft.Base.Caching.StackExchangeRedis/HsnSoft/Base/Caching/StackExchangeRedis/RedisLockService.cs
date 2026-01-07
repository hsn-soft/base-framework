using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace HsnSoft.Base.Caching.StackExchangeRedis;

public sealed class RedisLockService(IDistributedCache cache)
{
    private const string LockKeyPrefix = "lock:";

    public async Task<bool> TryAcquireAsync(string key, TimeSpan ttl)
    {
        string existing = await cache.GetStringAsync($"{LockKeyPrefix}:{key}");
        if (existing is not null) return false;

        await cache.SetStringAsync($"{LockKeyPrefix}:{key}", "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });

        return true;
    }

    public Task ReleaseAsync(string key) => cache.RemoveAsync($"{LockKeyPrefix}:{key}");
}