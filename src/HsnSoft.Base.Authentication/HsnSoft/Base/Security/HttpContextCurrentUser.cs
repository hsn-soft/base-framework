using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.Security;

/// <summary>
/// The current user of the request being served: what the token of its <c>HttpContext</c> says. Where there is no signed-in user (a message handler, a worker) it is the user of the open <see cref="ActingUser"/> scope, if any.
/// </summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    private bool SignedIn => Principal?.Identity?.IsAuthenticated == true;

    // a signed-in user of the request wins; without one, whoever the code was told to act as
    private UserContext? Acting => SignedIn ? null : ActingUser.Current;

    public bool IsAuthenticated => SignedIn || Acting?.Id is not null;

    public Guid? Id => SignedIn ? ReadGuid(BaseClaimTypes.Id) : Acting?.Id;

    public string? UserName => SignedIn ? Principal?.GetUserId() : Acting?.UserName;

    public Guid? TenantId => SignedIn ? ReadGuid(BaseClaimTypes.TenantId) : Acting?.TenantId;

    public bool IsSystemTenant => SignedIn
        ? string.Equals(Principal?.FindFirst(BaseClaimTypes.IsSystemTenant)?.Value, "true", StringComparison.OrdinalIgnoreCase)
        : Acting?.IsSystemTenant ?? false;

    public IReadOnlyList<Guid> AllowedTenantIds
        => [.. (Principal?.FindAll(BaseClaimTypes.AllowedTenantId) ?? []).Select(claim => Guid.TryParse(claim.Value, out Guid id) ? id : (Guid?)null).OfType<Guid>()];

    public IReadOnlyList<string> Roles => SignedIn ? [.. Principal!.GetRoles()] : Acting?.Roles ?? [];

    private Guid? ReadGuid(string claimType) => Guid.TryParse(Principal?.FindFirst(claimType)?.Value, out Guid id) ? id : null;
}
