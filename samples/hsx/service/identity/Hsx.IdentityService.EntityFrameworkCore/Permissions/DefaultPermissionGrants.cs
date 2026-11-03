using Hsx.Shared.Helper;

namespace Hsx.IdentityService.EntityFrameworkCore.Permissions;

/// <summary>
/// What the two roles of the sample are granted when they are made (<c>SampleIdentitySeeder</c>) or when a permission is first added (<c>PermissionDatabase.EnsurePermissionsAsync</c>): a starting point, not a rule. After it every grant is changed through its endpoints
/// (the administrator of a tenant changes the grants of the roles of its own tenant). The <c>SystemAdmin</c> of the system tenant does all of it; the <c>Admin</c> of a reseller or of an account manages everything of its tenant, and sees the tree of tenants without
/// changing it (only the system tenant makes, changes, moves and deletes a tenant). A role that is not named here is granted nothing.
/// </summary>
public static class DefaultPermissionGrants
{
    private static readonly HashSet<string> s_systemOnly =
    [
        HsxEndpointPermissions.Identity.Tenants.Create,
        HsxEndpointPermissions.Identity.Tenants.Update,
        HsxEndpointPermissions.Identity.Tenants.Delete,
        HsxEndpointPermissions.Identity.Tenants.Move,
    ];

    /// <summary>The codes a role of this name is granted.</summary>
    public static IReadOnlyList<string> For(string roleName)
        => roleName switch
        {
            HsxRoles.SystemAdmin => PermissionDatabase.CatalogCodes,
            HsxRoles.Admin => [.. PermissionDatabase.CatalogCodes.Where(code => !s_systemOnly.Contains(code))],
            _ => [],
        };
}
