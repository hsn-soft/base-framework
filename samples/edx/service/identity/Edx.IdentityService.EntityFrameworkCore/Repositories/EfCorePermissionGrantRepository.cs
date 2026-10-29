using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace Edx.IdentityService.EntityFrameworkCore.Repositories;

/// <summary>The grants in SQL Server: the permissions and the constraints of a role, read together and replaced together in one transaction. What is asked for the cache of the services ignores the tenant filter, and what is asked for one role relies on it (a caller reaches only its tenant's).</summary>
public sealed class EfCorePermissionGrantRepository(IdentityDbContext db) : IPermissionGrantRepository
{
    public async Task<IReadOnlyList<Permission>> FindActiveByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(codes);

        return await db.Permissions.AsNoTracking().Where(permission => permission.IsActive && codes.Contains(permission.Code)).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GrantedPermission>> GetRoleGrantsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        List<Row> plain = await db.RolePermissions.AsNoTracking()
            .Where(grant => grant.RoleId == roleId)
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (_, permission) => new Row(permission.Code, permission.Type, null))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        List<Row> constraints = await db.RolePermissionConstraints.AsNoTracking()
            .Where(grant => grant.RoleId == roleId)
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (grant, permission) => new Row(permission.Code, permission.Type, grant.Value))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ordered([.. plain, .. constraints]);
    }

    public async Task<IReadOnlyList<GrantedPermission>> GetEffectiveGrantsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IQueryable<Guid> roleIds = db.UserRoles.AsNoTracking().Where(link => link.UserId == userId).Select(link => link.RoleId);
        List<Row> plain = await db.RolePermissions.AsNoTracking()
            .Where(grant => roleIds.Contains(grant.RoleId))
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (_, permission) => new Row(permission.Code, permission.Type, null))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        List<Row> constraints = await db.RolePermissionConstraints.AsNoTracking()
            .Where(grant => roleIds.Contains(grant.RoleId))
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (grant, permission) => new Row(permission.Code, permission.Type, grant.Value))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ordered([.. plain, .. constraints]);
    }

    public async Task ReplaceRoleGrantsAsync(Role role, IReadOnlyList<(Permission Permission, string? Value)> grants, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(grants);

        // the new ones are made first: a value that is wrong stops the change before anything is taken away
        List<RolePermission> plain = [.. grants.Where(grant => grant.Permission.Type != PermissionType.Constraint).Select(grant => RolePermission.Grant(role, grant.Permission))];
        List<RolePermissionConstraint> constraints = [.. grants.Where(grant => grant.Permission.Type == PermissionType.Constraint).Select(grant => RolePermissionConstraint.Grant(role, grant.Permission, grant.Value))];

        // the strategy that retries a failed connection has to run the whole transaction as one unit
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await db.RolePermissions.IgnoreQueryFilters().Where(grant => grant.RoleId == role.Id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await db.RolePermissionConstraints.IgnoreQueryFilters().Where(grant => grant.RoleId == role.Id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            db.RolePermissions.AddRange(plain);
            db.RolePermissionConstraints.AddRange(constraints);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SubjectGrants>> GetAllRoleSubjectsAsync(CancellationToken cancellationToken = default)
    {
        var roles = await db.Roles.IgnoreQueryFilters().AsNoTracking().Select(role => new { role.Id, role.TenantId, role.Name }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var plain = await db.RolePermissions.IgnoreQueryFilters().AsNoTracking()
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (grant, permission) => new { grant.RoleId, permission.Code, permission.Type })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var constraints = await db.RolePermissionConstraints.IgnoreQueryFilters().AsNoTracking()
            .Join(db.Permissions.Where(permission => permission.IsActive), grant => grant.PermissionId, permission => permission.Id, (grant, permission) => new { grant.RoleId, permission.Code, permission.Type, grant.Value })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        ILookup<Guid, Row> byRole = plain.Select(grant => (grant.RoleId, Row: new Row(grant.Code, grant.Type, null)))
            .Concat(constraints.Select(grant => (grant.RoleId, Row: new Row(grant.Code, grant.Type, grant.Value))))
            .ToLookup(item => item.RoleId, item => item.Row);
        return [.. roles.Select(role => new SubjectGrants(role.TenantId, role.Name, Ordered(byRole[role.Id])))];
    }

    // what a query reads of a grant; the rows are made into grants here, in memory, in the order of the code. A code that came from more than one role gives the highest value when the values are numbers, the first otherwise.
    private sealed record Row(string Code, PermissionType Type, string? Value);

    private static List<GrantedPermission> Ordered(IEnumerable<Row> rows)
        => [.. rows.GroupBy(row => row.Code).Select(Merge).OrderBy(row => row.Code, StringComparer.Ordinal).Select(row => new GrantedPermission(row.Code, row.Type, row.Value))];

    private static Row Merge(IGrouping<string, Row> group)
    {
        List<Row> withValue = [.. group.Where(row => row.Value is not null)];
        return withValue.Count == 0
            ? group.First()
            : withValue.All(row => decimal.TryParse(row.Value, System.Globalization.CultureInfo.InvariantCulture, out _))
                ? withValue.MaxBy(row => decimal.Parse(row.Value!, System.Globalization.CultureInfo.InvariantCulture))!
                : withValue[0];
    }
}
