using System;
using System.Diagnostics;
using HsnSoft.Base.Security.Claims;
using JetBrains.Annotations;

namespace HsnSoft.Base.Users;

public static class CurrentUserExtensions
{
    [CanBeNull]
    public static string FindClaimValue(this ICurrentUser currentUser, string claimType)
    {
        return currentUser.FindClaim(claimType)?.Value;
    }

    public static T FindClaimValue<T>(this ICurrentUser currentUser, string claimType)
        where T : struct
    {
        var value = currentUser.FindClaimValue(claimType);
        if (value == null)
        {
            return default;
        }

        return value.To<T>();
    }

    public static Guid GetId(this ICurrentUser currentUser)
    {
        Debug.Assert(currentUser.Id != null, "currentUser.Id != null");

        return currentUser.Id.Value;
    }

    public static Guid? FindImpersonatorTenantId([NotNull] this ICurrentUser currentUser)
    {
        var impersonatorTenantId = currentUser.FindClaimValue(BaseClaimTypes.ImpersonatorTenantId);
        if (impersonatorTenantId.IsNullOrWhiteSpace())
        {
            return null;
        }
        if (Guid.TryParse(impersonatorTenantId, out var guid))
        {
            return guid;
        }

        return null;
    }

    public static Guid? FindImpersonatorUserId([NotNull] this ICurrentUser currentUser)
    {
        var impersonatorUserId = currentUser.FindClaimValue(BaseClaimTypes.ImpersonatorUserId);
        if (impersonatorUserId.IsNullOrWhiteSpace())
        {
            return null;
        }
        if (Guid.TryParse(impersonatorUserId, out var guid))
        {
            return guid;
        }

        return null;
    }

    public static string FindImpersonatorTenantName([NotNull] this ICurrentUser currentUser)
    {
        return currentUser.FindClaimValue(BaseClaimTypes.ImpersonatorTenantName);
    }

    public static string FindImpersonatorUserName([NotNull] this ICurrentUser currentUser)
    {
        return currentUser.FindClaimValue(BaseClaimTypes.ImpersonatorUserName);
    }
}
