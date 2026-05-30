using System.Net;
using HsnSoft.Base;
using HsnSoft.Base.Caching.StackExchangeRedis;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.Shared.Hosting.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class EndpointConcurrencyLimitAttribute(string endpointKey, int defaultLimit = 3, int ttlSeconds = 60) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var redis = context.HttpContext.RequestServices.GetRequiredService<IRequestLimitStore>();
        int limit = await redis.GetEndpointLimitAsync(endpointKey) ?? defaultLimit;
        var ttl = TimeSpan.FromSeconds(ttlSeconds);

        long currentCount = await redis.IncrementAsync(endpointKey, ttl);

        if (currentCount > limit)
        {
            // too many concurrent request
            await redis.DecrementAsync(endpointKey);
            throw new BaseHttpException((int)HttpStatusCode.TooManyRequests, $"Too many active requests. (Active: {currentCount}, Limit: {limit})");
            return;
        }

        try
        {
            _ = await next(); // continue endpoint process
        }
        finally
        {
            await redis.DecrementAsync(endpointKey);
        }
    }
}