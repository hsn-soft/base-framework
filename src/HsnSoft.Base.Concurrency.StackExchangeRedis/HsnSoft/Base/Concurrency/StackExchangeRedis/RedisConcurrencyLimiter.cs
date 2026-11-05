using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HsnSoft.Base.Concurrency.StackExchangeRedis;

/// <summary>
/// <see cref="IConcurrencyLimiter"/> on Redis, for every pod of a service: a name is a sorted set whose members are the holders and whose scores are when their leases end, by the clock of Redis (the pods do not need the same time). Taking a place is one
/// script: the ended leases are removed, and a place is added only if fewer than the maximum are left, so two pods that ask at the same moment cannot both take the last one. A pod that dies loses its place when its lease ends.
/// </summary>
public sealed class RedisConcurrencyLimiter(IConnectionMultiplexer redis, IOptions<ConcurrencyOptions> options) : IConcurrencyLimiter
{
    // KEYS[1] the set; ARGV[1] the maximum, ARGV[2] the lease in milliseconds, ARGV[3] the holder
    private const string AcquireScript = """
        local time = redis.call('TIME')
        local now = tonumber(time[1]) * 1000 + math.floor(tonumber(time[2]) / 1000)
        redis.call('ZREMRANGEBYSCORE', KEYS[1], '-inf', now)
        if redis.call('ZCARD', KEYS[1]) >= tonumber(ARGV[1]) then
            return 0
        end
        redis.call('ZADD', KEYS[1], now + tonumber(ARGV[2]), ARGV[3])
        redis.call('PEXPIRE', KEYS[1], tonumber(ARGV[2]))
        return 1
        """;

    // KEYS[1] the set; ARGV[1] the lease in milliseconds, ARGV[2] the holder: the lease is extended only if it has not ended
    private const string RenewScript = """
        local time = redis.call('TIME')
        local now = tonumber(time[1]) * 1000 + math.floor(tonumber(time[2]) / 1000)
        local ends = redis.call('ZSCORE', KEYS[1], ARGV[2])
        if not ends or tonumber(ends) <= now then
            return 0
        end
        redis.call('ZADD', KEYS[1], 'XX', now + tonumber(ARGV[1]), ARGV[2])
        redis.call('PEXPIRE', KEYS[1], math.max(tonumber(ARGV[1]), redis.call('PTTL', KEYS[1])))
        return 1
        """;

    private readonly ConcurrencyOptions _options = options.Value;

    public async ValueTask<IConcurrencyLease?> TryAcquireAsync(string name, int maxConcurrent, TimeSpan leaseTime, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrent, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leaseTime, TimeSpan.Zero);
        cancellationToken.ThrowIfCancellationRequested();

        string key = KeyOf(name);
        string holder = Guid.NewGuid().ToString("N");
        RedisResult taken = await redis.GetDatabase().ScriptEvaluateAsync(AcquireScript, [key], [maxConcurrent, (long)leaseTime.TotalMilliseconds, holder]).ConfigureAwait(false);
        return (long)taken == 1 ? new Lease(redis, key, name, holder) : null;
    }

    private string KeyOf(string name) => $"{_options.KeyPrefix}:{name}";

    private sealed class Lease(IConnectionMultiplexer redis, string key, string name, string holder) : IConcurrencyLease
    {
        public string Name => name;

        public async ValueTask<bool> RenewAsync(TimeSpan leaseTime, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leaseTime, TimeSpan.Zero);
            cancellationToken.ThrowIfCancellationRequested();

            RedisResult renewed = await redis.GetDatabase().ScriptEvaluateAsync(RenewScript, [key], [(long)leaseTime.TotalMilliseconds, holder]).ConfigureAwait(false);
            return (long)renewed == 1;
        }

        public async ValueTask DisposeAsync() => await redis.GetDatabase().SortedSetRemoveAsync(key, holder).ConfigureAwait(false);
    }
}
