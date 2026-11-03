using HsnSoft.Base.EntityFrameworkCore;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.IdentityService.EntityFrameworkCore.Permissions;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

/// <summary>The grants in PostgreSQL: the permissions of a role, read together and replaced together in one transaction. What is asked for the store of the services ignores the tenant filter, and what is asked for one role relies on it (a role of another tenant is not found, so it is not read).</summary>
internal sealed class EfCorePermissionGrantRepository(IdentityDbContext db) : IPermissionGrantRepository
{
    public async Task<IReadOnlyList<Permission>> FindActiveByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(codes);

        return await db.Permissions.AsNoTracking().Where(permission => permission.IsActive && codes.Contains(permission.Code)).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GrantedPermission>> GetRoleGrantsAsync(Guid roleId, CancellationToken cancellationToken = default)
        => Ordered(await db.RolePermissions.AsNoTracking()
            .Where(grant => grant.RoleId == roleId)
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (_, permission) => new GrantedPermission(permission.Code, permission.Type))
            .ToListAsync(cancellationToken).ConfigureAwait(false));

    public async Task<IReadOnlyList<GrantedPermission>> GetEffectiveGrantsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IQueryable<Guid> roleIds = db.AppUserRoles.AsNoTracking().Where(link => link.UserId == userId).Select(link => link.RoleId);
        return Ordered(await db.RolePermissions.AsNoTracking()
            .Where(grant => roleIds.Contains(grant.RoleId))
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (_, permission) => new GrantedPermission(permission.Code, permission.Type))
            .ToListAsync(cancellationToken).ConfigureAwait(false));
    }

    public async Task GrantStartingSetAsync(AppRole role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (role.RoleType is not (AppRoleType.Company or AppRoleType.Customer))
        {
            return;
        }

        IReadOnlyList<string> codes = DefaultPermissionGrants.ForRole(role.RoleType, role.Name);
        List<Permission> permissions = await db.Permissions.Where(permission => permission.IsActive && codes.Contains(permission.Code)).ToListAsync(cancellationToken).ConfigureAwait(false);
        await ReplaceRoleGrantsAsync(role, permissions, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReplaceRoleGrantsAsync(AppRole role, IReadOnlyList<Permission> grants, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(grants);

        List<RolePermission> made = [.. grants.DistinctBy(permission => permission.Id).Select(permission => RolePermission.Grant(role, permission))];

        // the strategy that retries a failed connection runs the whole unit again, so it is one unit
        await db.ExecuteInTransactionAsync(async token =>
        {
            await db.RolePermissions.IgnoreQueryFilters().Where(grant => grant.RoleId == role.Id).ExecuteDeleteAsync(token).ConfigureAwait(false);
            db.RolePermissions.AddRange(made);
            await db.SaveChangesAsync(token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SubjectGrants>> GetAllRoleSubjectsAsync(CancellationToken cancellationToken = default)
    {
        var roles = await db.AppRoles.IgnoreQueryFilters().AsNoTracking().Select(role => new { role.Id, role.TenantId, role.Name }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var grants = await db.RolePermissions.IgnoreQueryFilters().AsNoTracking()
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (grant, permission) => new { grant.RoleId, permission.Code, permission.Type })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        ILookup<Guid, GrantedPermission> byRole = grants.ToLookup(grant => grant.RoleId, grant => new GrantedPermission(grant.Code, grant.Type));
        return [.. roles.Select(role => new SubjectGrants(role.TenantId, role.Name, Ordered(byRole[role.Id])))];
    }

    private static List<GrantedPermission> Ordered(IEnumerable<GrantedPermission> rows) => [.. rows.DistinctBy(row => row.Code).OrderBy(row => row.Code, StringComparer.Ordinal)];
}
