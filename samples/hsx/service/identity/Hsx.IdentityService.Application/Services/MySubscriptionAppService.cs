using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Services;
using Hsx.IdentityService.Domain.CompanyDomain.Repositories;
using Hsx.IdentityService.Domain.CustomerDomain.Repositories;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;
using Hsx.IdentityService.Domain.TenantDomain.Repositories;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Services;

public sealed class MySubscriptionAppService(
    ISubscriptionRepository subscriptions,
    ICustomerRepository customers,
    ICompanyRepository companies,
    ITenantRepository tenants,
    IDataFilter dataFilter,
    IServiceProvider services) : ApplicationServiceBase(services), IMySubscriptionAppService
{
    public async Task<ListDataResultDto<MySubscriptionDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        bool system = CurrentTenant.IsSystemTenant;
        string[] keys = [.. CurrentTenant.AllowedScopeKeys];
        if (!system && keys.Length == 0)
        {
            return new ListDataResultDto<MySubscriptionDto>();
        }

        // the token names what the caller may see, whichever reseller sold it: the tenant filter is off for this read, and the scope keys are the filter
        using (dataFilter.Disable<IMultiTenant>())
        {
            // a scope key starts with the id of its customer: the database finds the subscriptions of those customers, and the keys the token names are compared here
            Guid[] keyCustomers = [.. keys.Select(key => key.Length >= 32 && Guid.TryParseExact(key[..32], "N", out Guid customer) ? customer : Guid.Empty).Where(customer => customer != Guid.Empty).Distinct()];
            IReadOnlyList<Subscription> candidates = await subscriptions.GetListAsync(new ListQueryOptions<Subscription> { Filter = subscription => subscription.EndedAt == null && (system || keyCustomers.Contains(subscription.CustomerId)) }, cancellationToken).ConfigureAwait(false);
            HashSet<string> allowed = [.. keys];
            List<Subscription> found = [.. candidates.Where(subscription => system || allowed.Contains(ScopeKeyHelper.Generate(subscription.CustomerId, subscription.ProductType)))];
            Guid[] customerIds = [.. found.Select(subscription => subscription.CustomerId).Distinct()];
            Guid[] companyIds = [.. found.Select(subscription => subscription.CompanyId).Distinct()];
            Guid[] tenantIds = [.. found.Select(subscription => subscription.TenantId).Distinct()];
            Dictionary<Guid, string> customerDomains = (await customers.GetListAsync(new ListQueryOptions<Hsx.IdentityService.Domain.CustomerDomain.Entities.Customer> { Filter = customer => customerIds.Contains(customer.Id) }, cancellationToken).ConfigureAwait(false)).ToDictionary(customer => customer.Id, customer => customer.Domain);
            Dictionary<Guid, string> companyTitles = (await companies.GetListAsync(new ListQueryOptions<Hsx.IdentityService.Domain.CompanyDomain.Entities.Company> { Filter = company => companyIds.Contains(company.Id) }, cancellationToken).ConfigureAwait(false)).ToDictionary(company => company.Id, company => company.Title);
            Dictionary<Guid, string> tenantTitles = (await tenants.GetListAsync(new ListQueryOptions<Hsx.IdentityService.Domain.TenantDomain.Entities.Tenant> { Filter = tenant => tenantIds.Contains(tenant.Id) }, cancellationToken).ConfigureAwait(false)).ToDictionary(tenant => tenant.Id, tenant => tenant.Title);

            return new ListDataResultDto<MySubscriptionDto>(
            [
                .. found.OrderBy(subscription => ScopeKeyHelper.Generate(subscription.CustomerId, subscription.ProductType), StringComparer.Ordinal).Select(subscription => new MySubscriptionDto
                {
                    SubscriptionId = subscription.Id,
                    TenantId = subscription.TenantId,
                    TenantTitle = tenantTitles.GetValueOrDefault(subscription.TenantId, string.Empty),
                    CompanyId = subscription.CompanyId,
                    CompanyTitle = companyTitles.GetValueOrDefault(subscription.CompanyId, string.Empty),
                    CustomerId = subscription.CustomerId,
                    CustomerDomain = customerDomains.GetValueOrDefault(subscription.CustomerId, string.Empty),
                    ProductType = subscription.ProductType,
                    ScopeKey = ScopeKeyHelper.Generate(subscription.CustomerId, subscription.ProductType),
                    ValidFrom = subscription.ValidFrom,
                    ValidTo = subscription.ValidTo,
                    IsBlocked = subscription.IsBlocked,
                }),
            ]);
        }
    }
}
