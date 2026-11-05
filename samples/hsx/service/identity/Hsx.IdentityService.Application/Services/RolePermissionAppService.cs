using System.Globalization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleDomain.Exceptions;
using Hsx.IdentityService.Domain.AppRoleDomain.Repositories;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Exceptions;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;

namespace Hsx.IdentityService.Application.Services;

/// <summary>
/// The grants of a role: what it is granted is read, and set as a whole. A set replaces the permissions and the constraints of the role together in one unit of work, so the role is left with all of what was meant or with what it had. The caller cannot give a role
/// more than it has itself (a permission it was not granted, a constraint with a higher value than its own), unless it is of the system tenant: an administrator of a reseller would otherwise grant itself the whole system. The store the services read is written after
/// the unit has ended.
/// </summary>
public sealed class RolePermissionAppService(
    IAppRoleRepository roles,
    IPermissionGrantRepository grants,
    IAppRolePermissionRepository rolePermissions,
    IAppRolePermissionConstraintRepository roleConstraints,
    PermissionCacheSynchronizer synchronizer,
    ICurrentUser currentUser,
    ICurrentTenant currentTenant,
    IUnitOfWork unitOfWork) : IRolePermissionAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">The role is not one the caller may see.</exception>
    public async Task<RolePermissionsDto> GetAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        _ = await FindRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
        return await ToDtoAsync(roleId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gives the role exactly these grants (a code given twice is one grant, the last value wins).</summary>
    /// <exception cref="HsnSoft.Base.NotFoundException">The role is not one the caller may see, or a code is not an active permission of the catalogue.</exception>
    /// <exception cref="PermissionValueRequiredException">A constraint has no value.</exception>
    /// <exception cref="PermissionValueNotAllowedException">A permission that limits no value has one.</exception>
    /// <exception cref="AppRolePermissionNotGrantableException">The caller does not have what it grants.</exception>
    public async Task<RolePermissionsDto> SetAsync(Guid roleId, SetRolePermissionsDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // a role that is not the caller's tenant's is not found (the tenant filter), so a grant can only be set where the caller may
        AppRole role = await FindRoleAsync(roleId, cancellationToken).ConfigureAwait(false);
        List<(Permission Permission, string? Value)> asked = await ResolveAsync(input.Grants, cancellationToken).ConfigureAwait(false);
        await EnsureCallerHasAsync(asked, cancellationToken).ConfigureAwait(false);

        await unitOfWork.ExecuteAsync(async token =>
        {
            // what is made first can stop the change before anything is taken away: a value that is wrong throws here
            List<AppRolePermission> plain = [.. asked.Where(grant => grant.Permission.Type != PermissionType.Constraint).Select(grant => AppRolePermission.Grant(role, grant.Permission))];
            List<AppRolePermissionConstraint> limits = [.. asked.Where(grant => grant.Permission.Type == PermissionType.Constraint).Select(grant => AppRolePermissionConstraint.Grant(role, grant.Permission, grant.Value))];

            await rolePermissions.DeleteWhereAsync(grant => grant.RoleId == role.Id, token).ConfigureAwait(false);
            await roleConstraints.DeleteWhereAsync(grant => grant.RoleId == role.Id, token).ConfigureAwait(false);
            await rolePermissions.AddManyAsync(plain, token).ConfigureAwait(false);
            await roleConstraints.AddManyAsync(limits, token).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);

        await synchronizer.SyncRoleAsync(role, cancellationToken).ConfigureAwait(false);
        return await ToDtoAsync(role.Id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<RolePermissionsDto> ToDtoAsync(Guid roleId, CancellationToken cancellationToken)
        => new() { RoleId = roleId, Grants = GrantMapping.ToDtos(await grants.GetRoleGrantsAsync(roleId, cancellationToken).ConfigureAwait(false)) };

    private async Task<AppRole> FindRoleAsync(Guid roleId, CancellationToken cancellationToken)
        => await roles.FindAsync(roleId, cancellationToken).ConfigureAwait(false) ?? throw AppRoleNotFound.For(roleId);

    // a code that is not an active permission of the catalogue stops the change before anything is written; a constraint has to have its value and any other permission must have none
    private async Task<List<(Permission Permission, string? Value)>> ResolveAsync(IReadOnlyList<GrantInputDto> input, CancellationToken cancellationToken)
    {
        Dictionary<string, string?> asked = [];
        foreach (GrantInputDto grant in input)
        {
            asked[grant.Code!.Trim().ToLowerInvariant()] = grant.Value;
        }

        IReadOnlyList<Permission> found = await grants.FindActiveByCodesAsync(asked.Keys, cancellationToken).ConfigureAwait(false);
        Dictionary<string, Permission> byCode = found.ToDictionary(permission => permission.Code);
        return [.. asked.Select(item =>
        {
            Permission permission = byCode.GetValueOrDefault(item.Key) ?? throw PermissionNotFound.For(item.Key);
            return (permission, permission.CheckValue(item.Value));
        })];
    }

    private async Task EnsureCallerHasAsync(List<(Permission Permission, string? Value)> asked, CancellationToken cancellationToken)
    {
        if (currentTenant.IsSystemTenant)
        {
            return;
        }

        IReadOnlyList<GrantedPermission> own = currentUser.Id is { } userId ? await grants.GetEffectiveGrantsAsync(userId, cancellationToken).ConfigureAwait(false) : [];
        Dictionary<string, GrantedPermission> ownByCode = own.ToDictionary(grant => grant.Code);
        foreach ((Permission permission, string? value) in asked)
        {
            if (!ownByCode.TryGetValue(permission.Code, out GrantedPermission? has) || !Covers(has.Value, value))
            {
                throw new AppRolePermissionNotGrantableException(permission.Code);
            }
        }
    }

    // a number is covered by an equal or higher one of the caller; any other value has to be the same
    private static bool Covers(string? own, string? asked)
        => asked is null
            || (own is not null && (decimal.TryParse(own, CultureInfo.InvariantCulture, out decimal ownNumber) && decimal.TryParse(asked, CultureInfo.InvariantCulture, out decimal askedNumber)
                ? askedNumber <= ownNumber
                : string.Equals(own, asked, StringComparison.Ordinal)));
}
