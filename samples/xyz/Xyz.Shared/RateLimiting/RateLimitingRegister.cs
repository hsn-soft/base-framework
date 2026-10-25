using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Xyz.Shared.RateLimiting;

public static class RateLimitingRegister
{
    /// <summary>Reads the limit (<c>RateLimit:RequestsPerMinute</c>) the <see cref="RedisRateLimitMiddleware"/> counts against; it needs Redis (<c>AddXyzRedis</c>).</summary>
    public static IServiceCollection AddXyzRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.Configure<RateLimitOptions>(configuration.GetSection("RateLimit"));
    }
}
