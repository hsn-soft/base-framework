using System.Threading.RateLimiting;
using HsnSoft.Base.AspNetCore.Responses;
using Microsoft.AspNetCore.RateLimiting;

namespace Hsx.AuthServer.Host.Security.RateLimiting;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Limits the requests of one address: <see cref="RateLimitOptions.GlobalPerMinute"/> for everything but the health endpoints, and the policies of <see cref="RateLimitPolicies"/> for the endpoints that ask for them. A request over the limit is answered 429 with <c>Retry-After</c> (the answer of every other error of the host).
    /// </summary>
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        RateLimitOptions limits = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();
        if (limits.GlobalPerMinute < 1 || limits.SignInPerMinute < 1 || limits.RefreshPerMinute < 1)
        {
            throw new InvalidOperationException($"{RateLimitOptions.SectionName}: every limit is one request a minute at least.");
        }

        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = RejectAsync;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context
                => context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
                    ? RateLimitPartition.GetNoLimiter("health")
                    : PerMinute("global:" + AddressOf(context), limits.GlobalPerMinute));
            options.AddPolicy(RateLimitPolicies.SignIn, context => PerMinute("sign-in:" + AddressOf(context), limits.SignInPerMinute));
            options.AddPolicy(RateLimitPolicies.Refresh, context => PerMinute("refresh:" + AddressOf(context), limits.RefreshPerMinute));
        });
        return services;
    }

    private static string AddressOf(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static RateLimitPartition<string> PerMinute(string key, int permits)
        => RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true });

    private static async ValueTask RejectAsync(OnRejectedContext rejected, CancellationToken cancellationToken)
    {
        HttpContext context = rejected.HttpContext;
        int seconds = rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter) ? (int)Math.Ceiling(retryAfter.TotalSeconds) : 60;
        // the answer is written from scratch (the response is cleared), so the header is set when it starts
        context.Response.OnStarting(static state =>
        {
            (HttpContext http, int wait) = ((HttpContext, int))state;
            http.Response.Headers.RetryAfter = wait.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Task.CompletedTask;
        }, (context, seconds));
        await context.RequestServices.GetRequiredService<IApiResponseWriter>().WriteErrorAsync(context, StatusCodes.Status429TooManyRequests, ["Too many requests"], "rate_limited").ConfigureAwait(false);
    }
}
