using System.Security.Claims;
using JetBrains.Annotations;

namespace HsnSoft.Base.Authorization.Permissions.ValueProviders;

public class PermissionValueCheckContext
{
    [NotNull] public string Permission { get; }

    [CanBeNull] public ClaimsPrincipal Principal { get; }

    public PermissionValueCheckContext([NotNull] string permissionName, [CanBeNull] ClaimsPrincipal principal)
    {
        Check.NotNull(permissionName, nameof(permissionName));

        Permission =  permissionName ;
        Principal = principal;
    }
}