using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using HsnSoft.Base;
using HsnSoft.Base.Security.Claims;
using JetBrains.Annotations;

namespace System.Security.Principal;

public static class BaseClaimsIdentityExtensions
{
    public static Guid? FindUserId([NotNull] this ClaimsPrincipal principal)
    {
        Check.NotNull(principal, nameof(principal));

        var userIdOrNull = principal.Claims?.FirstOrDefault(c => c.Type == BaseClaimTypes.UserId);
        if (userIdOrNull == null || userIdOrNull.Value.IsNullOrWhiteSpace())
        {
            return null;
        }

        if (Guid.TryParse(userIdOrNull.Value, out var guid))
        {
            return guid;
        }

        return null;
    }

    public static Guid? FindTenantId([NotNull] this ClaimsPrincipal principal)
    {
        Check.NotNull(principal, nameof(principal));

        var tenantIdOrNull = principal.Claims?.FirstOrDefault(c => c.Type == BaseClaimTypes.TenantId);
        if (tenantIdOrNull == null || tenantIdOrNull.Value.IsNullOrWhiteSpace())
        {
            return null;
        }

        if (Guid.TryParse(tenantIdOrNull.Value, out var guid))
        {
            return guid;
        }

        return null;
    }

    public static List<Guid> FindAllowedTenantIds([NotNull] this ClaimsPrincipal principal)
    {
        Check.NotNull(principal, nameof(principal));

        var idList = principal?.Claims?.Where(c => c.Type == BaseClaimTypes.AllowedTenantId).ToList();
        if (idList is not { Count: > 0 })
        {
            return [];
        }

        List<Guid> results = [];
        var checkedList = idList.Where(x => !string.IsNullOrEmpty(x.Value)).ToList();
        foreach (var checkedId in checkedList)
        {
            if (Guid.TryParse(checkedId.Value, out var guid))
            {
                results.Add(guid);
            }
        }

        return results;
    }

    public static string FindClientId([NotNull] this ClaimsPrincipal principal)
    {
        Check.NotNull(principal, nameof(principal));

        var clientIdOrNull = principal.Claims?.FirstOrDefault(c => c.Type == BaseClaimTypes.ClientId);
        if (clientIdOrNull == null || clientIdOrNull.Value.IsNullOrWhiteSpace())
        {
            return null;
        }

        return clientIdOrNull.Value;
    }
}