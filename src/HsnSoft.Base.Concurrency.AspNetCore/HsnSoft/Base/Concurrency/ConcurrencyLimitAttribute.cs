using System;
using System.Threading.Tasks;
using HsnSoft.Base.Communication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Concurrency;

/// <summary>
/// At most <see cref="Max"/> executions of this endpoint at the same time, in all the pods together (the <see cref="IConcurrencyLimiter"/> the service registered). A request that finds the places taken is answered at once with 429 and
/// <c>Retry-After</c> (a <see cref="BaseResponse"/>); the place is held until the answer has been written and given back when it ends, whatever way. The name is a limit of its own: two endpoints with the same name share the places.
/// <c>Concurrency:Limits:{name}</c> in the settings changes the maximum and the lease without a deployment.
/// </summary>
/// <param name="name">What is limited (<c>send-invoice</c>).</param>
/// <param name="max">How many may run at once when the settings say nothing.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ConcurrencyLimitAttribute(string name, int max) : Attribute, IAsyncResourceFilter
{
    /// <summary>What is limited.</summary>
    public string Name { get; } = name;

    /// <summary>How many may run at once when the settings say nothing.</summary>
    public int Max { get; } = max;

    /// <summary>The lease in seconds when the settings say nothing; not set: <c>Concurrency:DefaultLeaseSeconds</c>. It is the longest the endpoint is expected to take: a place that is not given back by then is taken from its holder.</summary>
    public int LeaseSeconds { get; init; }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IConcurrencyLease? lease = await ConcurrencyGate.EnterAsync(context.HttpContext, Name, Max, LeaseSeconds > 0 ? TimeSpan.FromSeconds(LeaseSeconds) : null).ConfigureAwait(false);
        if (lease is null)
        {
            context.Result = new ObjectResult(ConcurrencyGate.TooMany(context.HttpContext)) { StatusCode = StatusCodes.Status429TooManyRequests };
            return;
        }

        await using (lease.ConfigureAwait(false))
        {
            await next().ConfigureAwait(false);
        }
    }
}

public static class ConcurrencyLimitEndpointExtensions
{
    /// <summary>The same limit for a minimal api endpoint: see <see cref="ConcurrencyLimitAttribute"/>.</summary>
    public static RouteHandlerBuilder RequireConcurrencyLimit(this RouteHandlerBuilder builder, string name, int max, int leaseSeconds = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return builder.AddEndpointFilter(async (invocation, next) =>
        {
            IConcurrencyLease? lease = await ConcurrencyGate.EnterAsync(invocation.HttpContext, name, max, leaseSeconds > 0 ? TimeSpan.FromSeconds(leaseSeconds) : null).ConfigureAwait(false);
            if (lease is null)
            {
                return Results.Json(ConcurrencyGate.TooMany(invocation.HttpContext), statusCode: StatusCodes.Status429TooManyRequests);
            }

            await using (lease.ConfigureAwait(false))
            {
                return await next(invocation).ConfigureAwait(false);
            }
        });
    }
}

internal static class ConcurrencyGate
{
    internal static async ValueTask<IConcurrencyLease?> EnterAsync(HttpContext http, string name, int codeMax, TimeSpan? codeLease)
    {
        ConcurrencyOptions options = http.RequestServices.GetRequiredService<IOptions<ConcurrencyOptions>>().Value;
        (int max, TimeSpan lease) = options.Resolve(name, codeMax, codeLease);
        return await http.RequestServices.GetRequiredService<IConcurrencyLimiter>().TryAcquireAsync(name, max, lease, http.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>The answer of "too many": the header and the body; the caller gives it the status.</summary>
    internal static BaseResponse TooMany(HttpContext http)
    {
        int retryAfter = http.RequestServices.GetRequiredService<IOptions<ConcurrencyOptions>>().Value.RetryAfterSeconds;
        http.Response.Headers.RetryAfter = retryAfter.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new BaseResponse { StatusCode = StatusCodes.Status429TooManyRequests, StatusMessages = ["Too many requests are being handled at the moment. Try again shortly."], TraceId = http.TraceIdentifier, ErrorCode = "concurrency_limit_reached" };
    }
}
