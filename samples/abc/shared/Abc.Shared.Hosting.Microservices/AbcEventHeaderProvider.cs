using HsnSoft.Base.EventBus;
using Microsoft.AspNetCore.Http;

namespace Abc.Shared.Hosting.Microservices;

/// <summary>
/// Carries what the client says about itself from the request into the events it causes (<c>ClientChannel</c>, <c>ClientVersion</c>) and the correlation id of the request. Who is asking (the user, the roles, the tenant) is not here:
/// the event bus takes it from the current user and puts it in the envelope itself. This is the product's knowledge, so it lives here: the event bus only carries the headers.
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
