using System;
using System.Linq;
using System.Security.Claims;
using HsnSoft.Base.Tracing;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.AspNetCore.Tracing;

public class HttpContextTraceAccessor : ITraceAccesor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTraceAccessor(IHttpContextAccessor httpContextAccessor) { _httpContextAccessor = httpContextAccessor; }

    public string GetCorrelationId() => _httpContextAccessor.HttpContext?.GetCorrelationId() ?? Guid.CreateVersion7().ToString("N");

    public string GetClientChannel() => _httpContextAccessor.HttpContext?.GetChannel();

    public string GetClientLat() => _httpContextAccessor.HttpContext?.GetClientRequestLat();

    public string GetClientLong() => _httpContextAccessor.HttpContext?.GetClientRequestLong();

    public string GetClientVersion() => _httpContextAccessor.HttpContext?.GetClientVersion();

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

    public string[] GetUserRoles()
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        Check.NotNull(principal, nameof(principal));

        var roles = principal.Claims.Where(c => c.Type == ClaimTypes.Role).ToArray();

        return roles.Select(c => c.Value).Distinct().ToArray();
    }
}