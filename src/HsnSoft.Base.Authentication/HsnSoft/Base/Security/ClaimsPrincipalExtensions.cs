using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace HsnSoft.Base.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The user id (<see cref="BaseClaimTypes.UserId"/>), or null for an anonymous caller.</summary>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindFirst(BaseClaimTypes.UserId)?.Value;
    }

    /// <summary>The user id; a caller without one is a mistake of the endpoint (it should require authentication).</summary>
    /// <exception cref="BusinessRuleException">The principal has no user id.</exception>
    public static string GetRequiredUserId(this ClaimsPrincipal principal)
        => principal.GetUserId() ?? throw new BusinessRuleException("The token has no subject.");

    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindAll(BaseClaimTypes.Role).Select(claim => claim.Value).ToList();
    }
}
