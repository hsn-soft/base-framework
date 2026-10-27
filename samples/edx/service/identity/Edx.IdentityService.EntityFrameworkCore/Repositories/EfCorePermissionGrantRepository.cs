using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Edx.IdentityService.EntityFrameworkCore.Repositories;

/// <summary>The grants in SQL Server. A change replaces the grants of a role or a user in one transaction. What is asked for the cache of the services ignores the tenant filter, and what is asked for one role or user relies on it (a caller reaches only its tenant's).</summary>
public sealed class EfCorePermissionGrantRepository(IdentityDbContext db) : IPermissionGrantRepository
{
    public async Task<IReadOnlyList<Permission>> FindActiveByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(codes);

        return await db.Permissions.AsNoTracking().Where(permission => permission.IsActive && codes.Contains(permission.Code)).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GrantedPermission>> GetRoleGrantsAsync(Guid roleId, CancellationToken cancellationToken = default)
        => Ordered(await db.RolePermissions.AsNoTracking()
            .Where(grant => grant.RoleId == roleId)
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (grant, permission) => new Row(permission.Code, permission.Type, grant.Value))
            .ToListAsync(cancellationToken).ConfigureAwait(false));

    public async Task<IReadOnlyList<GrantedPermission>> GetUserGrantsAsync(Guid userId, CancellationToken cancellationToken = default)
        => Ordered(await db.UserPermissions.AsNoTracking()
            .Where(grant => grant.UserId == userId)
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (grant, permission) => new Row(permission.Code, permission.Type, grant.Value))
            .ToListAsync(cancellationToken).ConfigureAwait(false));

    public async Task<IReadOnlyList<GrantedPermission>> GetEffectiveGrantsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // the user's own grants first, so a value of the user wins over the roles'; the rest in the order of the code
        List<GrantedPermission> own = [.. await GetUserGrantsAsync(userId, cancellationToken).ConfigureAwait(false)];
        List<Row> ofRoles = await db.UserRoles.AsNoTracking()
            .Where(link => link.UserId == userId)
            .Join(db.RolePermissions, link => link.RoleId, grant => grant.RoleId, (_, grant) => grant)
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (grant, permission) => new Row(permission.Code, permission.Type, grant.Value))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ordered(own.Select(granted => new Row(granted.Code, granted.Type, granted.Value)).Concat(ofRoles));
    }

    public async Task ReplaceRoleGrantsAsync(Role role, IReadOnlyList<(Permission Permission, string? Value)> grants, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(grants);

        // the new ones are made first: a value that is wrong stops the change before anything is taken away
        List<RolePermission> replacement = [.. grants.Select(grant => RolePermission.Grant(role, grant.Permission, grant.Value))];

        // the strategy that retries a failed connection has to run the whole transaction as one unit
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await db.RolePermissions.IgnoreQueryFilters().Where(grant => grant.RoleId == role.Id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            db.RolePermissions.AddRange(replacement);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task ReplaceUserGrantsAsync(User user, IReadOnlyList<(Permission Permission, string? Value)> grants, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(grants);

        List<UserPermission> replacement = [.. grants.Select(grant => UserPermission.Grant(user, grant.Permission, grant.Value))];

        // the strategy that retries a failed connection has to run the whole transaction as one unit
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await db.UserPermissions.IgnoreQueryFilters().Where(grant => grant.UserId == user.Id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            db.UserPermissions.AddRange(replacement);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SubjectGrants>> GetAllRoleSubjectsAsync(CancellationToken cancellationToken = default)
    {
        var roles = await db.Roles.IgnoreQueryFilters().AsNoTracking().Select(role => new { role.Id, role.TenantId, role.Name }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var grants = await db.RolePermissions.IgnoreQueryFilters().AsNoTracking()
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (grant, permission) => new { grant.RoleId, permission.Code, permission.Type, grant.Value })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        ILookup<Guid, Row> byRole = grants.ToLookup(grant => grant.RoleId, grant => new Row(grant.Code, grant.Type, grant.Value));
        return [.. roles.Select(role => new SubjectGrants(role.TenantId, role.Name, Ordered(byRole[role.Id])))];
    }

    public async Task<IReadOnlyList<SubjectGrants>> GetAllUserSubjectsAsync(CancellationToken cancellationToken = default)
    {
        var grants = await db.UserPermissions.IgnoreQueryFilters().AsNoTracking()
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (grant, permission) => new { grant.UserId, grant.TenantId, permission.Code, permission.Type, grant.Value })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. grants.GroupBy(grant => (grant.TenantId, grant.UserId)).Select(group => new SubjectGrants(group.Key.TenantId, group.Key.UserId.ToString(), Ordered(group.Select(grant => new Row(grant.Code, grant.Type, grant.Value)))))];
    }

    // what a query reads of a grant; the rows are made into grants here, in memory, in the order of the code (and one row for a code that came twice: the one with a value wins)
    private sealed record Row(string Code, PermissionType Type, string? Value);

    private static List<GrantedPermission> Ordered(IEnumerable<Row> rows)
        => [.. rows.GroupBy(row => row.Code).Select(group => group.FirstOrDefault(row => row.Value is not null) ?? group.First()).OrderBy(row => row.Code, StringComparer.Ordinal).Select(row => new GrantedPermission(row.Code, row.Type, row.Value))];
}
