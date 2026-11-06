using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Consts;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.PermissionDomain.Catalog;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Consts;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.IdentityService.EntityFrameworkCore.Permissions;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Seeding;

/// <summary>
/// The roles of the sample, under fixed ids: the two of the system, the administrator of each reseller, and the roles of a reseller that name a company or a customer. Each role is made in its own tenant and starts with the grants
/// the catalog gives its type and name (<see cref="PermissionDatabase.StartingGrantsAsync"/>). After the tenants, the companies, the customers and the permissions it names. A role that is there (even one that was deleted, which the filter hides) is
/// left as it is, so the grants an administrator took away are not given back at the next start. The normalized name is made by the entity.
/// </summary>
internal sealed class RolesSeeder(IdentityServiceDbContext db, IPermissionCatalog catalog, ICurrentTenant currentTenant) : IDataSeeder
{
    private static readonly (string Id, Guid TenantId, string Name, bool IsStatic, AppRoleType Type, string? CompanyId, string? CustomerId)[] s_defaults =
    [
        ("00000000-0000-7000-8000-00000000c001", DefaultTenantIds.System, AppRoleConsts.SystemAdmin, true, AppRoleType.System, null, null),
        ("00000000-0000-7000-8000-00000000c002", DefaultTenantIds.System, AppRoleConsts.SystemOperator, true, AppRoleType.System, null, null),
        ("00000000-0000-7000-8000-00000000c003", DefaultTenantIds.TechSummus, AppRoleConsts.TenantAdmin, true, AppRoleType.Tenant, null, null),
        ("00000000-0000-7000-8000-00000000c004", DefaultTenantIds.AudioTime, AppRoleConsts.TenantAdmin, true, AppRoleType.Tenant, null, null),
        ("00000000-0000-7000-8000-00000000c005", DefaultTenantIds.EPartner, AppRoleConsts.TenantAdmin, true, AppRoleType.Tenant, null, null),
        ("00000000-0000-7000-8000-00000000c006", DefaultTenantIds.AudioTime, "ilbak-admin", false, AppRoleType.Company, "00000000-0000-7000-8000-00000000b001", null),
        ("00000000-0000-7000-8000-00000000c007", DefaultTenantIds.AudioTime, "cnbce-admin", false, AppRoleType.Customer, null, "00000000-0000-7000-8000-00000000b002"),
    ];

    /// <summary>The ids of the default roles, which <see cref="UsersSeeder"/> makes a user for.</summary>
    internal static IReadOnlyList<Guid> DefaultRoleIds { get; } = [.. s_defaults.Select(role => Guid.Parse(role.Id))];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        HashSet<Guid> known = [.. await db.Set<AppRole>().IgnoreQueryFilters().Select(role => role.Id).ToListAsync(cancellationToken).ConfigureAwait(false)];
        foreach ((string id, Guid tenantId, string name, bool isStatic, AppRoleType type, string? companyId, string? customerId) in s_defaults.Where(role => !known.Contains(Guid.Parse(role.Id))))
        {
            // the tenant of a role and of its grants is the current one when they are saved
            using (currentTenant.Change(tenantId))
            {
                AppRole role = AppRole.Create(name, type, companyId is null ? null : Guid.Parse(companyId), customerId is null ? null : Guid.Parse(customerId), isStatic, Guid.Parse(id));
                db.Set<AppRole>().Add(role);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                StartingGrants grants = await PermissionDatabase.StartingGrantsAsync(db, catalog, role, cancellationToken).ConfigureAwait(false);
                db.Set<AppRolePermission>().AddRange(grants.Permissions);
                db.Set<AppRolePermissionConstraint>().AddRange(grants.Constraints);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
