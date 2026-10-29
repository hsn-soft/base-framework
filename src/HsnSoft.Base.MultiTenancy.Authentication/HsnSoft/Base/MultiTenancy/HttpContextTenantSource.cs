using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.MultiTenancy;

/// <summary>The <see cref="ITenantSource"/> of a request: what the claims of the signed-in user say. Without a signed-in user it does not know a tenant (a message handler has the source of the envelope).</summary>
internal sealed class HttpContextTenantSource(IHttpContextAccessor accessor) : ITenantSource
{
    public TenantContext? Current
    {
        get
        {
            ClaimsPrincipal? principal = accessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            IReadOnlyList<Guid> allowed = [.. principal.FindAll(TenantClaimTypes.AllowedTenantId).Select(claim => Guid.TryParse(claim.Value, out Guid id) ? id : (Guid?)null).OfType<Guid>()];
            Guid? tenant = Guid.TryParse(principal.FindFirst(TenantClaimTypes.TenantId)?.Value, out Guid own) ? own : null;
            bool isSystem = string.Equals(principal.FindFirst(TenantClaimTypes.IsSystemTenant)?.Value, "true", StringComparison.OrdinalIgnoreCase);
            return new TenantContext(tenant, isSystem, allowed);
        }
    }
}
