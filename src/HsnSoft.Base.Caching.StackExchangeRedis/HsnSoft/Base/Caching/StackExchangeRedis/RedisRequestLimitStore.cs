using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace HsnSoft.Base.Caching.StackExchangeRedis;

public class RedisRequestLimitStore(IConnectionMultiplexer redis) : IRequestLimitStore
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string Prefix = "req_limit:"; // namespace prefix

    public async Task<long> IncrementAsync(string endpointKey, TimeSpan ttl)
    {
        string key = $"{Prefix}{endpointKey}";
        long count = await _db.StringIncrementAsync(key);

        // TTL just set first increment
        if (count == 1)
            await _db.KeyExpireAsync(key, ttl);

        return count;
    }

    public async Task<long> DecrementAsync(string endpointKey)
    {
        string key = $"{Prefix}{endpointKey}";
        long count = await _db.StringDecrementAsync(key);
        if (count < 0)
        {
            await _db.StringSetAsync(key, 0);
            return 0;
        }
        return count;
    }

    public async Task<long> GetCountAsync(string endpointKey)
    {
        string key = $"{Prefix}{endpointKey}";
        var val = await _db.StringGetAsync(key);
        return val.HasValue ? (long)val : 0;
    }

    public async Task SetEndpointLimitAsync(string endpointKey, int limit)
    {
        await _db.StringSetAsync($"{Prefix}config:{endpointKey}", limit);
    }

    public async Task<int?> GetEndpointLimitAsync(string endpointKey)
    {
        var val = await _db.StringGetAsync($"{Prefix}config:{endpointKey}");
        return val.HasValue ? (int)val : null;
    }
}