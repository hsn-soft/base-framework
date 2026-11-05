using System.Globalization;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleDomain.Repositories;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

/// <summary>
/// The grants: what a role was granted, read through the repositories of the permissions, of the grants, of the constraints and of the roles. What is asked for the store of the services looks at every tenant (the filter of the tenants is off); what is asked for
/// one role relies on the filter. A code that more than one role gives is one grant: the highest value when the values are numbers, the first otherwise.
/// </summary>
internal sealed class EfCorePermissionGrantRepository(
    IPermissionRepository permissions,
    IAppRolePermissionRepository grants,
    IAppRolePermissionConstraintRepository constraints,
    IAppRoleRepository roles,
    IDataFilter dataFilter) : IPermissionGrantRepository
{
    public async Task<IReadOnlyList<Permission>> FindActiveByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(codes);
        return await permissions.GetListAsync(new ListQueryOptions<Permission> { Filter = permission => permission.IsActive && codes.Contains(permission.Code) }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GrantedPermission>> GetRoleGrantsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GrantedPermission> plain = await grants.GetListAsync(
            new ListQueryOptions<AppRolePermission> { Filter = grant => grant.RoleId == roleId && grant.Permission!.IsActive },
            grant => new GrantedPermission(grant.Permission!.Code, grant.Permission.Type, null),
            cancellationToken).ConfigureAwait(false);
        IReadOnlyList<GrantedPermission> limits = await constraints.GetListAsync(
            new ListQueryOptions<AppRolePermissionConstraint> { Filter = grant => grant.RoleId == roleId && grant.Permission!.IsActive },
            grant => new GrantedPermission(grant.Permission!.Code, grant.Permission.Type, grant.Value),
            cancellationToken).ConfigureAwait(false);
        return Ordered([.. plain, .. limits]);
    }

    public async Task<IReadOnlyList<GrantedPermission>> GetEffectiveGrantsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GrantedPermission> plain = await grants.GetListAsync(
            new ListQueryOptions<AppRolePermission> { Filter = grant => grant.Permission!.IsActive && grant.Role!.UserRoles.Any(link => link.UserId == userId) },
            grant => new GrantedPermission(grant.Permission!.Code, grant.Permission.Type, null),
            cancellationToken).ConfigureAwait(false);
        IReadOnlyList<GrantedPermission> limits = await constraints.GetListAsync(
            new ListQueryOptions<AppRolePermissionConstraint> { Filter = grant => grant.Permission!.IsActive && grant.Role!.UserRoles.Any(link => link.UserId == userId) },
            grant => new GrantedPermission(grant.Permission!.Code, grant.Permission.Type, grant.Value),
            cancellationToken).ConfigureAwait(false);
        return Ordered([.. plain, .. limits]);
    }

    public async Task<IReadOnlyList<SubjectGrants>> GetAllRoleSubjectsAsync(CancellationToken cancellationToken = default)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            var subjects = await roles.GetListAsync(
                new ListQueryOptions<AppRole>(),
                role => new
                {
                    role.TenantId,
                    role.Name,
                    Grants = role.Permissions.Where(grant => grant.Permission!.IsActive).Select(grant => new { grant.Permission!.Code, grant.Permission.Type }).ToList(),
                    Limits = role.PermissionConstraints.Where(grant => grant.Permission!.IsActive).Select(grant => new { grant.Permission!.Code, grant.Permission.Type, grant.Value }).ToList(),
                },
                cancellationToken).ConfigureAwait(false);
            return [.. subjects.Select(subject => new SubjectGrants(
                subject.TenantId,
                subject.Name,
                Ordered(subject.Grants.Select(grant => new GrantedPermission(grant.Code, grant.Type, null)).Concat(subject.Limits.Select(grant => new GrantedPermission(grant.Code, grant.Type, grant.Value))))))];
        }
    }

    private static List<GrantedPermission> Ordered(IEnumerable<GrantedPermission> rows)
        => [.. rows.GroupBy(row => row.Code).Select(Merge).OrderBy(row => row.Code, StringComparer.Ordinal)];

    private static GrantedPermission Merge(IGrouping<string, GrantedPermission> group)
    {
        List<GrantedPermission> withValue = [.. group.Where(row => row.Value is not null)];
        if (withValue.Count == 0)
        {
            return group.First();
        }

        return withValue.All(row => decimal.TryParse(row.Value, CultureInfo.InvariantCulture, out _))
            ? withValue.MaxBy(row => decimal.Parse(row.Value!, CultureInfo.InvariantCulture))!
            : withValue[0];
    }
}
