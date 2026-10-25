using System.Security.Claims;
using HsnSoft.Base.EventBus;
using Microsoft.AspNetCore.Http;

namespace Abc.Shared.Hosting.Microservices;

/// <summary>
/// Carries who is asking from the request into the events it causes (<c>UserId</c>, <c>UserRoles</c>, <c>ClientChannel</c>, <c>ClientVersion</c>), so the consume logs of the whole chain say who started it.
/// This is the product's knowledge, so it lives here: the event bus only carries the headers.
/// </summary>
public sealed class AbcEventHeaderProvider(IHttpContextAccessor accessor) : IEventHeaderProvider
{
    public string? GetCorrelationId() => accessor.HttpContext?.Request.Headers["X-Correlation-Id"].ToString() is { Length: > 0 } id ? id : null;

    public void AddHeaders(IDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        HttpContext? context = accessor.HttpContext;
        if (context is null)
        {
            return;
        }

        Add(headers, "UserId", context.User.FindFirstValue("sub"));
        Add(headers, "UserRoles", string.Join(',', context.User.Claims.Where(claim => claim.Type is "role" or ClaimTypes.Role).Select(claim => claim.Value).Distinct()));
        Add(headers, "ClientChannel", context.Request.Headers["X-Channel"].ToString());
        Add(headers, "ClientVersion", context.Request.Headers["X-Client-Version"].ToString());
    }

    private static void Add(IDictionary<string, string> headers, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            headers[name] = value;
        }
    }
}
