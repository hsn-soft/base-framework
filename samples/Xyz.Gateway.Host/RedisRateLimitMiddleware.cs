using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Xyz.Gateway.Host;

public sealed class RateLimitOptions
{
    public int RequestsPerMinute { get; set; } = 120;
}

/// <summary>
/// Fixed-window rate limit shared by every gateway instance: one redis counter per caller and minute.
/// The caller is the token subject, or the client address when there is no token. If redis is down the request passes.
/// </summary>
public sealed class RedisRateLimitMiddleware(RequestDelegate next, IConnectionMultiplexer redis, IOptions<RateLimitOptions> options, ILogger<RedisRateLimitMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        int limit = options.Value.RequestsPerMinute;
        string caller = context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        long window = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;

        long count;
        try
        {
            var db = redis.GetDatabase();
            string key = $"xyz:ratelimit:{caller}:{window}";
            count = await db.StringIncrementAsync(key);
            if (count == 1)
            {
                await db.KeyExpireAsync(key, TimeSpan.FromMinutes(2));
            }
        }
        catch (RedisException exception)
        {
            logger.LogWarning("Rate limit skipped, redis is not available: {Reason}", exception.Message);
            await next(context);
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit - count).ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (count > limit)
        {
            int retryAfter = 60 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 60);
            context.Response.Headers.RetryAfter = retryAfter.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { title = "Too many requests", status = 429, retryAfterSeconds = retryAfter });
            return;
        }

        await next(context);
    }
}
