using System.Net;
using HsnSoft.Base;
using HsnSoft.Base.Caching.StackExchangeRedis;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.Shared.Hosting.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class EventConcurrencyLimitAttribute(string endpointKey, int defaultLimit = 3, int ttlSeconds = 60) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var limitStore = context.HttpContext.RequestServices.GetRequiredService<IRequestLimitStore>();

        int limit = await limitStore.GetEndpointLimitAsync(endpointKey) ?? defaultLimit;
        long activeCount = await limitStore.GetCountAsync(endpointKey);
        if (activeCount >= limit)
        {
            throw new BaseHttpException((int)HttpStatusCode.TooManyRequests, $"Too many active requests. (Active: {activeCount}, Limit: {limit})");
        }

        _ = await limitStore.IncrementAsync(endpointKey, TimeSpan.FromSeconds(ttlSeconds));

        _ = await next(); // continue endpoint process
    }
}