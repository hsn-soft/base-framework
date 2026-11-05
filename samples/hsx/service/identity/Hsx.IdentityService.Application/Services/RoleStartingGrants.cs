using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.PermissionDomain.Catalog;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;

namespace Hsx.IdentityService.Application.Services;

/// <summary>
/// What a role starts with when it is made: the permissions the catalog says its type is granted (those that carry no value) and its constraints with their starting values (<see cref="IPermissionCatalog"/>). A role made by an administrator and the
/// administrator role of a new reseller start the same way. Run inside the unit of work that makes the role.
/// </summary>
public sealed class RoleStartingGrants(
    IPermissionCatalog catalog,
    IPermissionGrantRepository grants,
    IAppRolePermissionRepository rolePermissions,
    IAppRolePermissionConstraintRepository roleConstraints)
{
    public async Task GrantAsync(AppRole role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        IReadOnlyList<string> plainCodes = catalog.GrantsFor(role.RoleType, role.Name);
        IReadOnlyDictionary<string, string> limits = catalog.ConstraintsFor(role.RoleType, role.Name);
        IReadOnlyList<Permission> found = await grants.FindActiveByCodesAsync([.. plainCodes, .. limits.Keys], cancellationToken).ConfigureAwait(false);
        await rolePermissions.AddManyAsync([.. found.Where(permission => permission.Type != PermissionType.Constraint).Select(permission => AppRolePermission.Grant(role, permission))], cancellationToken).ConfigureAwait(false);
        await roleConstraints.AddManyAsync([.. found.Where(permission => permission.Type == PermissionType.Constraint).Select(permission => AppRolePermissionConstraint.Grant(role, permission, limits[permission.Code]))], cancellationToken).ConfigureAwait(false);
    }
}
