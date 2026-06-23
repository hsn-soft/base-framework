using Hhs.Gateway.Commercial.Helpers;
using Hhs.Gateway.Commercial.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Hhs.Gateway.Commercial.Middlewares;

public sealed class DynamicRequestBodyLimitMiddleware : IMiddleware
{
    private readonly GatewayPolicyOptions _options;

    public DynamicRequestBodyLimitMiddleware(IOptions<GatewayPolicyOptions> options)
    {
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();

        if (feature is { IsReadOnly: false })
        {
            feature.MaxRequestBodySize = GatewayRouteClassifier.GetMaxRequestBodySize(context, _options);
        }

        await next(context);
    }
}