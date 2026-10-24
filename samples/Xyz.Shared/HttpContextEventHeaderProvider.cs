using System.Security.Claims;
using HsnSoft.Base.EventBus;
using Microsoft.AspNetCore.Http;

namespace Xyz.Shared;

/// <summary>
/// Carries who is asking from the request into the events it causes, under the names the wire has always had (UserId, UserRoles,
/// ClientChannel, ClientVersion, ClientLat, ClientLong), so the consume logs of the whole chain say who started it. This is the product's
/// knowledge, so it lives in Shared: the event bus only carries the headers.
/// </summary>
public sealed class HttpContextEventHeaderProvider(IHttpContextAccessor accessor) : IEventHeaderProvider
{
    public string? GetCorrelationId() => accessor.HttpContext?.Request.Headers["X-Correlation-Id"].ToString() is { Length: > 0 } id ? id : null;

    public void AddHeaders(IDictionary<string, string> headers)
    {
        HttpContext? context = accessor.HttpContext;
        if (context is null)
        {
            return;
        }

        Add(headers, "UserId", context.User.FindFirstValue("sub"));
        Add(headers, "UserRoles", string.Join(',', context.User.Claims.Where(claim => claim.Type is "role" or ClaimTypes.Role).Select(claim => claim.Value).Distinct()));
        Add(headers, "ClientChannel", context.Request.Headers["X-Channel"].ToString());
        Add(headers, "ClientVersion", context.Request.Headers["X-Client-Version"].ToString());
        Add(headers, "ClientLat", context.Request.Headers["X-Client-Request-Lat"].ToString());
        Add(headers, "ClientLong", context.Request.Headers["X-Client-Request-Long"].ToString());
    }

    private static void Add(IDictionary<string, string> headers, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            headers[name] = value;
        }
    }
}
