using HsnSoft.Base.Data;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.TenantDomain.Consts;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.Shared.Helper;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Seeding;

/// <summary>
/// The tenants every environment starts with: the system and the resellers of the sample, under the ids of <see cref="DefaultTenantIds"/>. A tenant that is there (even one that was deleted, which the filter hides) is left as it is, so what an
/// administrator changed is not undone at the next start. Before the roles and the users, which belong to a tenant.
/// </summary>
internal sealed class TenantsSeeder(IdentityServiceDbContext db) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        HashSet<Guid> known = [.. await db.Set<Tenant>().IgnoreQueryFilters().Select(tenant => tenant.Id).ToListAsync(cancellationToken).ConfigureAwait(false)];
        (Guid Id, TenantTypes Type, string Title, string Name)[] defaults =
        [
            (DefaultTenantIds.System, TenantTypes.System, "System", HsxTenants.SystemName),
            (DefaultTenantIds.TechSummus, TenantTypes.Reseller, "Tech Summus", HsxTenants.TechSummusName),
            (DefaultTenantIds.AudioTime, TenantTypes.Reseller, "Audio Time", HsxTenants.AudioTimeName),
            (DefaultTenantIds.EPartner, TenantTypes.Reseller, "ePartner", HsxTenants.EPartnerName),
        ];

        db.Set<Tenant>().AddRange(defaults.Where(tenant => !known.Contains(tenant.Id)).Select(tenant => Tenant.Create(tenant.Type, tenant.Title, tenant.Name, null, tenant.Id)));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
