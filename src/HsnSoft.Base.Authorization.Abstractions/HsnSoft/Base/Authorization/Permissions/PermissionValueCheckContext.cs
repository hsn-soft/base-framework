using System.Security.Claims;
using JetBrains.Annotations;

namespace HsnSoft.Base.Authorization.Permissions;

public class PermissionValueCheckContext
{
    public PermissionValueCheckContext(
        [NotNull] string permissionName,
        [CanBeNull] ClaimsPrincipal principal)
    {
        Check.NotNull(permissionName, nameof(permissionName));

        Permission = new PermissionDefinition(permissionName);
        Principal = principal;
    }

    [NotNull]
    public PermissionDefinition Permission { get; }

    [CanBeNull]
    public ClaimsPrincipal Principal { get; }
}