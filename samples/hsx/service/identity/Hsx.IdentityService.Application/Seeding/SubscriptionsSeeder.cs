using HsnSoft.Base.Data;
using Hsx.IdentityService.Application.Services;
using Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;
using Hsx.IdentityService.Domain.TenantDomain.Consts;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Seeding;

/// <summary>
/// The sales every environment starts with, under fixed ids: a reseller sells a product to a customer. Each one is a real sale (<see cref="SubscriptionAppService.SellAsync"/>), so it is given to the roles of the reseller that cover it (a Tenant role,
/// the Company role of its company, the Customer role of its customer) the way every sale is. It is a seeder of the application and not of the persistence because the rule of the links is the application's; the host adds it after the others
/// (the tenants, the companies, the customers and the roles it names). A sale that is there (even one that ended, which the filter hides) is left as it is.
/// </summary>
public sealed class SubscriptionsSeeder(SubscriptionAppService sales, ISubscriptionRepository subscriptions) : IDataSeeder
{
    private static readonly (string Id, Guid TenantId, string CustomerId, ProductTypes Product)[] s_defaults =
    [
        ("00000000-0000-7000-8000-00000000d001", DefaultTenantIds.AudioTime, "00000000-0000-7000-8000-00000000b002", ProductTypes.VideoPlatform),
        ("00000000-0000-7000-8000-00000000d002", DefaultTenantIds.TechSummus, "00000000-0000-7000-8000-00000000b002", ProductTypes.Podcast),
        ("00000000-0000-7000-8000-00000000d003", DefaultTenantIds.EPartner, "00000000-0000-7000-8000-00000000b002", ProductTypes.EInvoice),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach ((string id, Guid tenantId, string customerId, ProductTypes product) in s_defaults)
        {
            Guid subscriptionId = Guid.Parse(id);
            if (!await subscriptions.AnyAsync(subscription => subscription.Id == subscriptionId, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
            {
                await sales.SellAsync(tenantId, Guid.Parse(customerId), product, null, null, subscriptionId, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
