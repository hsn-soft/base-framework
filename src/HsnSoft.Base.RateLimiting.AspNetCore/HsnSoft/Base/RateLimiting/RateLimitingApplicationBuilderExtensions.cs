using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using HsnSoft.Base.Communication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.RateLimiting;

public static class RateLimitingApplicationBuilderExtensions
{
    private static readonly JsonSerializerOptions s_json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, Converters = { new JsonStringEnumConverter() } };

    /// <summary>
    /// Limits every request of one address to <c>RateLimiting:Global:Permits</c> in <c>WindowSeconds</c> (the paths of <c>ExcludedPaths</c>, <c>/health</c> by default, are not counted); nothing is done when no permits are set. Put it after <c>UseBaseForwardedHeaders</c> (the
    /// address of the caller is the one the proxy saw) and before the authentication (a token is checked only for a request that is let in). The limits of single endpoints are <see cref="RateLimitAttribute"/>. The limiter has to be registered (<c>AddRedisRateLimiter</c> or
    /// <c>AddInMemoryRateLimiter</c>).
    /// </summary>
    public static IApplicationBuilder UseBaseRateLimiting(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        GlobalRateLimitOptions options = app.ApplicationServices.GetRequiredService<IConfiguration>().GetSection(GlobalRateLimitOptions.SectionName).Get<GlobalRateLimitOptions>() ?? new GlobalRateLimitOptions();
        if (options.Permits <= 0)
        {
            return app;
        }

        if (options.WindowSeconds < 1)
        {
            throw new InvalidOperationException($"{GlobalRateLimitOptions.SectionName}:WindowSeconds has to be one second at least.");
        }

        return app.Use(async (context, next) =>
        {
            if (options.ExcludedPaths.Any(path => context.Request.Path.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase)))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            BaseResponse? refused = await RateLimitGate.CheckAsync(context, "global", options.Permits, options.WindowSeconds, RateLimitBy.Address).ConfigureAwait(false);
            if (refused is null)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(refused, s_json, context.RequestAborted).ConfigureAwait(false);
        });
    }
}
