using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Services;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;

namespace Hsx.IdentityService.Application.Services;

public sealed class MySubscriptionAppService(
    IRepository<Subscription, Guid> subscriptions,
    IRepository<Customer, Guid> customers,
    IRepository<Company, Guid> companies,
    IRepository<Tenant, Guid> tenants,
    IDataFilter dataFilter,
    IServiceProvider services) : ApplicationServiceBase(services), IMySubscriptionAppService
{
    public async Task<List<MySubscriptionDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        bool system = CurrentTenant.IsSystemTenant;
        string[] keys = [.. CurrentTenant.AllowedScopeKeys];
        if (!system && keys.Length == 0)
        {
            return [];
        }

        // the token names what the caller may see, whichever reseller sold it: the tenant filter is off for this read, and the scope keys are the filter
        using (dataFilter.Disable<IMultiTenant>())
        {
            IReadOnlyList<Subscription> found = await subscriptions.GetListAsync(new ListQueryOptions<Subscription> { Filter = subscription => subscription.EndedAt == null && (system || keys.Contains(subscription.ScopeKey)) }, cancellationToken).ConfigureAwait(false);
            Guid[] customerIds = [.. found.Select(subscription => subscription.CustomerId).Distinct()];
            Guid[] companyIds = [.. found.Select(subscription => subscription.CompanyId).Distinct()];
            Guid[] tenantIds = [.. found.Select(subscription => subscription.TenantId).Distinct()];
            Dictionary<Guid, string> customerDomains = (await customers.GetListAsync(new ListQueryOptions<Customer> { Filter = customer => customerIds.Contains(customer.Id) }, cancellationToken).ConfigureAwait(false)).ToDictionary(customer => customer.Id, customer => customer.Domain);
            Dictionary<Guid, string> companyTitles = (await companies.GetListAsync(new ListQueryOptions<Company> { Filter = company => companyIds.Contains(company.Id) }, cancellationToken).ConfigureAwait(false)).ToDictionary(company => company.Id, company => company.Title);
            Dictionary<Guid, string> tenantTitles = (await tenants.GetListAsync(new ListQueryOptions<Tenant> { Filter = tenant => tenantIds.Contains(tenant.Id) }, cancellationToken).ConfigureAwait(false)).ToDictionary(tenant => tenant.Id, tenant => tenant.Title);

            return
            [
                .. found.OrderBy(subscription => subscription.ScopeKey, StringComparer.Ordinal).Select(subscription => new MySubscriptionDto
                {
                    SubscriptionId = subscription.Id,
                    TenantId = subscription.TenantId,
                    TenantTitle = tenantTitles.GetValueOrDefault(subscription.TenantId, string.Empty),
                    CompanyId = subscription.CompanyId,
                    CompanyTitle = companyTitles.GetValueOrDefault(subscription.CompanyId, string.Empty),
                    CustomerId = subscription.CustomerId,
                    CustomerDomain = customerDomains.GetValueOrDefault(subscription.CustomerId, string.Empty),
                    ProductType = subscription.ProductType,
                    ScopeKey = subscription.ScopeKey,
                    ValidFrom = subscription.ValidFrom,
                    ValidTo = subscription.ValidTo,
                    IsBlocked = subscription.IsBlocked,
                }),
            ];
        }
    }
}
