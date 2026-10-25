using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.Security;

/// <summary>The current user of the request being served: what the token of its <c>HttpContext</c> says.</summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? Id => ReadGuid(BaseClaimTypes.Id);

    public string? UserName => Principal?.GetUserId();

    public Guid? TenantId => ReadGuid(BaseClaimTypes.TenantId);

    public bool IsSystemTenant => string.Equals(Principal?.FindFirst(BaseClaimTypes.IsSystemTenant)?.Value, "true", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<Guid> AllowedTenantIds
        => [.. (Principal?.FindAll(BaseClaimTypes.AllowedTenantId) ?? []).Select(claim => Guid.TryParse(claim.Value, out Guid id) ? id : (Guid?)null).OfType<Guid>()];

    private Guid? ReadGuid(string claimType) => Guid.TryParse(Principal?.FindFirst(claimType)?.Value, out Guid id) ? id : null;
}
