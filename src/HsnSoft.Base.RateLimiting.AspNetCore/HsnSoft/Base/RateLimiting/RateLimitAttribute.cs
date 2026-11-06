using System;
using System.Threading.Tasks;
using HsnSoft.Base.Communication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HsnSoft.Base.RateLimiting;

/// <summary>
/// At most <see cref="Permits"/> requests of one caller to this endpoint in <see cref="PerSeconds"/> seconds, in all the pods together (the <see cref="IRateLimiter"/> the service registered). A request over the limit is answered at once with 429 and <c>Retry-After</c>
/// (a <see cref="BaseResponse"/> with the error code <c>rate_limit_exceeded</c>). The name is a limit of its own: two endpoints with the same name share the count of a caller. <c>RateLimiting:Limits:{name}</c> in the settings changes the permits and the window without a deployment.
/// </summary>
/// <param name="name">What is limited (<c>sign-in</c>).</param>
/// <param name="permits">How many requests a caller may make in the window when the settings say nothing.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RateLimitAttribute(string name, int permits) : Attribute, IAsyncResourceFilter
{
    /// <summary>What is limited.</summary>
    public string Name { get; } = name;

    /// <summary>How many requests a caller may make in the window when the settings say nothing.</summary>
    public int Permits { get; } = permits;

    /// <summary>The window in seconds when the settings say nothing. Default: 60.</summary>
    public int PerSeconds { get; init; } = 60;

    /// <summary>Who is counted. Default: the address.</summary>
    public RateLimitBy By { get; init; } = RateLimitBy.Address;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        BaseResponse? refused = await RateLimitGate.CheckAsync(context.HttpContext, Name, Permits, PerSeconds, By).ConfigureAwait(false);
        if (refused is not null)
        {
            context.Result = new ObjectResult(refused) { StatusCode = StatusCodes.Status429TooManyRequests };
            return;
        }

        await next().ConfigureAwait(false);
    }
}

public static class RateLimitEndpointExtensions
{
    /// <summary>The same limit for a minimal api endpoint: see <see cref="RateLimitAttribute"/>.</summary>
    public static RouteHandlerBuilder RequireRateLimit(this RouteHandlerBuilder builder, string name, int permits, int perSeconds = 60, RateLimitBy by = RateLimitBy.Address)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return builder.AddEndpointFilter(async (invocation, next) =>
        {
            BaseResponse? refused = await RateLimitGate.CheckAsync(invocation.HttpContext, name, permits, perSeconds, by).ConfigureAwait(false);
            return refused is not null ? Results.Json(refused, statusCode: StatusCodes.Status429TooManyRequests) : await next(invocation).ConfigureAwait(false);
        });
    }
}
