
namespace Abc.PublicWeb.Host.Services;

/// <summary>What every call to the gateway carries: the correlation id of the page request being served (so the whole chain, the page, the gateway and the services behind it, is one in the logs).</summary>
public sealed class GatewayRequestHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpContext? context = accessor.HttpContext;
        if (context is not null && context.Request.Headers.TryGetValue("X-Correlation-Id", out Microsoft.Extensions.Primitives.StringValues correlationId))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", (string?)correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
