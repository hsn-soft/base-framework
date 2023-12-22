using System;
using System.Linq;
using System.Security.Claims;
using HsnSoft.Base.AspNetCore.Tracing;
using HsnSoft.Base.EventBus.Abstractions;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.AspNetCore.Security.Claims;

public class HttpContextTraceAccessor : IEventBusTraceAccesor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTraceAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCorrelationId()
    {
        return _httpContextAccessor.HttpContext?.GetCorrelationId() ?? Guid.NewGuid().ToString("N");
    }

    public string GetUserId()
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        Check.NotNull(principal, nameof(principal));

        var userIdOrNull = principal.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdOrNull == null || userIdOrNull.Value.IsNullOrWhiteSpace())
        {
            return null;
        }

        return userIdOrNull.Value;
    }

    public string[] GetRoles()
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        Check.NotNull(principal, nameof(principal));

        var roles = principal?.Claims.Where(c => c.Type == ClaimTypes.Role).ToArray() ?? Array.Empty<Claim>();

        return roles.Select(c => c.Value).Distinct().ToArray();
    }
}