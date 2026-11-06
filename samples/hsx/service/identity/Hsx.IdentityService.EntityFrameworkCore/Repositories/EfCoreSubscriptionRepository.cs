using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

internal sealed class EfCoreSubscriptionRepository(IdentityServiceDbContext db, IDataFilter dataFilter) : EfCoreRepository<IdentityServiceDbContext, Subscription, Guid>(db), ISubscriptionRepository
{
    public async Task<Subscription?> FindOfAnyTenantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            return await FindAsync(id, cancellationToken).ConfigureAwait(false);
        }
    }

    // across tenants: whoever sells it, a customer has a product once
    public Task<bool> HasActiveAsync(Guid customerId, ProductTypes product, CancellationToken cancellationToken = default)
        => AnyAsync(subscription => subscription.CustomerId == customerId && subscription.ProductType == product && subscription.EndedAt == null && !subscription.IsDeleted, ignoreFilters: true, cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetActiveIdsOfTenantAsync(Guid tenantId, Guid? companyId = null, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            IReadOnlyList<IdRow> ids = await GetListAsync(
                new ListQueryOptions<Subscription>
                {
                    Filter = subscription => subscription.TenantId == tenantId && subscription.EndedAt == null
                                             && (companyId == null || subscription.CompanyId == companyId) && (customerId == null || subscription.CustomerId == customerId),
                },
                selector: subscription => new IdRow(subscription.Id),
                cancellationToken).ConfigureAwait(false);
            return [.. ids.Select(row => row.Id)];
        }
    }
}
