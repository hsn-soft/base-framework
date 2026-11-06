using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HsnSoft.Base.RateLimiting.StackExchangeRedis;

/// <summary>
/// <see cref="IRateLimiter"/> on Redis, for every pod of a service: the count of a key under a name is one Redis counter whose life is the window (the first request makes it and gives it its time to live, by the clock of Redis, so the pods need no same time). A request is
/// one script, so two pods that count at the same moment cannot both see the last permit. If Redis cannot be reached the call throws; it does not let the request through unlimited (the host decides, <c>RateLimiting:FailOpen</c> of the AspNetCore package).
/// </summary>
public sealed class RedisRateLimiter(IConnectionMultiplexer redis, IOptions<RateLimitOptions> options) : IRateLimiter
{
    // KEYS[1] the counter; ARGV[1] the window in milliseconds. Returns the count and the milliseconds the counter has left.
    private const string CountScript = """
        local count = redis.call('INCR', KEYS[1])
        if count == 1 then
            redis.call('PEXPIRE', KEYS[1], tonumber(ARGV[1]))
        end
        local left = redis.call('PTTL', KEYS[1])
        if left < 0 then
            redis.call('PEXPIRE', KEYS[1], tonumber(ARGV[1]))
            left = tonumber(ARGV[1])
        end
        return { count, left }
        """;

    private readonly RateLimitOptions _options = options.Value;

    public async ValueTask<RateLimitResult> TryAcquireAsync(string name, string key, int permits, TimeSpan window, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThan(permits, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(window, TimeSpan.Zero);
        cancellationToken.ThrowIfCancellationRequested();

        RedisResult[] counted = (RedisResult[])(await redis.GetDatabase().ScriptEvaluateAsync(CountScript, [$"{_options.KeyPrefix}:{name}:{key}"], [(long)window.TotalMilliseconds]).ConfigureAwait(false))!;
        long count = (long)counted[0];
        long leftMilliseconds = (long)counted[1];
        return new RateLimitResult(count <= permits, (int)Math.Max(0, permits - count), TimeSpan.FromMilliseconds(leftMilliseconds));
    }
}
