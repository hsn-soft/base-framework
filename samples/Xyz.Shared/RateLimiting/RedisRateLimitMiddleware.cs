using System.Globalization;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Xyz.Shared.RateLimiting;

/// <summary>
/// Fixed-window rate limit shared by every instance of a service: one Redis counter per caller and minute. The caller is the
/// token subject, or the client address when there is no token. If Redis is down the request passes (fail open).
/// This is Xyz's own policy, so it lives in the product's shared library, not in the framework.
/// </summary>
public sealed class RedisRateLimitMiddleware(
    RequestDelegate next,
    IConnectionMultiplexer redis,
    IOptions<RateLimitOptions> rateLimit,
    IOptions<RedisOptions> redisOptions,
    ILogger<RedisRateLimitMiddleware> logger)
{
    private static readonly TimeSpan CounterLifetime = TimeSpan.FromMinutes(2);

    public async Task InvokeAsync(HttpContext context)
    {
        int limit = rateLimit.Value.RequestsPerMinute;
        string caller = context.User.GetUserId() ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        long count;
        try
        {
            var database = redis.GetDatabase();
            string key = $"{redisOptions.Value.InstanceName}ratelimit:{caller}:{now / 60}";
            count = await database.StringIncrementAsync(key);
            if (count == 1)
            {
                await database.KeyExpireAsync(key, CounterLifetime);
            }
        }
        catch (RedisException exception)
        {
            logger.LogWarning("Rate limit skipped, Redis is not available: {Reason}", exception.Message);
            await next(context);
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit - count).ToString(CultureInfo.InvariantCulture);

        if (count > limit)
        {
            int retryAfter = 60 - (int)(now % 60);
            context.Response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { title = "Too many requests", status = 429, retryAfterSeconds = retryAfter });
            return;
        }

        await next(context);
    }
}
