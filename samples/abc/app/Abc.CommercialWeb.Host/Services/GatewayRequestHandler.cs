using System.Net.Http.Headers;

namespace Abc.CommercialWeb.Host.Services;

/// <summary>What every call to the gateway carries: the correlation id of the page request being served (so the whole chain, the page, the gateway and the services behind it, is one in the logs) and the bearer token of the signed-in user.</summary>
public sealed class GatewayRequestHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpContext? context = accessor.HttpContext;
        if (context is not null && context.Request.Headers.TryGetValue("X-Correlation-Id", out Microsoft.Extensions.Primitives.StringValues correlationId))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", (string?)correlationId);
        }

        // the token of the signed-in user, kept in his cookie
        string? token = context?.User.FindFirst("access_token")?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
