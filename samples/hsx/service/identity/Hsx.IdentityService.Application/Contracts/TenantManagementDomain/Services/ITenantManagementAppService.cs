using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos;

namespace Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Services;

/// <summary>
/// What a reseller does to have customers: it makes a company (with the account tenant its people sign in to), adds the customers of the company and sells them a product. The system tenant makes the resellers. What is made is its tenant's: a tenant
/// sees its own and what the tenants under it made.
/// </summary>
public interface ITenantManagementAppService
{
    /// <summary>A reseller, with its administrator role and (when given) its first administrator. Only the system tenant.</summary>
    /// <exception cref="HsnSoft.Base.BaseHttpException">The caller is not the system tenant (403).</exception>
    Task<ProvisionedTenantDto> CreateResellerAsync(CreateResellerDto input, CancellationToken cancellationToken = default);

    /// <summary>A company of a reseller and the account tenant under the reseller for its people. The caller is the reseller (or the system tenant, naming it).</summary>
    /// <exception cref="HsnSoft.Base.BaseHttpException">The tenant is not a reseller (403).</exception>
    Task<ProvisionedTenantDto> CreateCompanyAsync(CreateCompanyDto input, CancellationToken cancellationToken = default);

    /// <summary>The customer of a company, by its website. Asked twice for the same company and website it is one customer.</summary>
    /// <exception cref="Hsx.IdentityService.Domain.CustomerDomain.Exceptions.CustomerDomainDuplicateException">The website is the customer of another company.</exception>
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto input, CancellationToken cancellationToken = default);

    /// <summary>A product sold to a customer by a reseller. Asked twice for the same reseller, customer and product it is one subscription.</summary>
    Task<SubscriptionDto> SellSubscriptionAsync(SellSubscriptionDto input, CancellationToken cancellationToken = default);
}
