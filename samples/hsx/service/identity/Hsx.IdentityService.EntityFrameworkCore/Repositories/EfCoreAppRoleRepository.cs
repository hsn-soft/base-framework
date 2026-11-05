using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleDomain.Repositories;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore.Context;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreAppRoleRepository(IdentityServiceDbContext db, IDataFilter dataFilter) : EfCoreRepository<IdentityServiceDbContext, AppRole, Guid>(db), IAppRoleRepository
{
    // a deleted role keeps its name (the index counts it), so the filters are off for this check
    public Task<bool> NameExistsAsync(Guid tenantId, string normalizedName, Guid? exceptId = null, CancellationToken cancellationToken = default)
        => AnyAsync(role => role.TenantId == tenantId && role.NormalizedName == normalizedName && (exceptId == null || role.Id != exceptId), ignoreFilters: true, cancellationToken);

    public async Task<AppRole?> FindOfAnyTenantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            return await FindAsync(id, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<AppRole>> GetCoveringAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        Guid tenantId = subscription.TenantId;
        Guid companyId = subscription.CompanyId;
        Guid customerId = subscription.CustomerId;
        using (dataFilter.Disable<IMultiTenant>())
        {
            return await GetListAsync(
                new ListQueryOptions<AppRole>
                {
                    Filter = role => role.TenantId == tenantId
                                     && (role.RoleType == AppRoleType.Tenant
                                         || (role.RoleType == AppRoleType.Company && role.CompanyId == companyId)
                                         || (role.RoleType == AppRoleType.Customer && role.CustomerId == customerId)),
                },
                cancellationToken).ConfigureAwait(false);
        }
    }
}
