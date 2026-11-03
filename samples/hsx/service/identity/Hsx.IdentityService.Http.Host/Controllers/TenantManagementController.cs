using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Hsx.Shared.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

/// <summary>What a reseller does to have customers, and what the system tenant does to have resellers.</summary>
[Route("api/identity-service/v1/commercial/tenant-management")]
public sealed class TenantManagementController(ITenantManagementAppService tenantManagement) : BaseServiceController
{
    /// <summary>A reseller with its administrator role and, when given, its first administrator. Only the system tenant.</summary>
    [HttpPost("resellers/create")]
    [PermissionAuthorize(HsxEndpointPermissions.Identity.TenantManagement.CreateReseller)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<ProvisionedTenantDto> CreateResellerAsync([FromBody] CreateResellerDto input, CancellationToken cancellationToken = default)
        => tenantManagement.CreateResellerAsync(input, cancellationToken);

    /// <summary>A company of the reseller and the account tenant under it for the company's people.</summary>
    [HttpPost("companies/create")]
    [PermissionAuthorize(HsxEndpointPermissions.Identity.TenantManagement.CreateCompany)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<ProvisionedTenantDto> CreateCompanyAsync([FromBody] CreateCompanyDto input, CancellationToken cancellationToken = default)
        => tenantManagement.CreateCompanyAsync(input, cancellationToken);

    /// <summary>The customer of a company, by its website; the same company and website is one customer.</summary>
    [HttpPost("customers/create")]
    [PermissionAuthorize(HsxEndpointPermissions.Identity.TenantManagement.CreateCustomer)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<CustomerDto> CreateCustomerAsync([FromBody] CreateCustomerDto input, CancellationToken cancellationToken = default)
        => tenantManagement.CreateCustomerAsync(input, cancellationToken);

    /// <summary>A product sold to a customer; the same reseller, customer and product is one subscription.</summary>
    [HttpPost("customer-subscriptions/create")]
    [PermissionAuthorize(HsxEndpointPermissions.Identity.TenantManagement.SellSubscription)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<SubscriptionDto> SellSubscriptionAsync([FromBody] SellSubscriptionDto input, CancellationToken cancellationToken = default)
        => tenantManagement.SellSubscriptionAsync(input, cancellationToken);
}
