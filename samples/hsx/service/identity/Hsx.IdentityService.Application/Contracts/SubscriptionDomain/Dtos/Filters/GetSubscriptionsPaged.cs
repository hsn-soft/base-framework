using HsnSoft.Base.Application.Dtos;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Filters;

public sealed class GetSubscriptionsPaged : PagedDataRequestDto
{
    /// <summary>Only the reseller tenant id.</summary>
    public Guid? ResellerTenantId { get; set; }

    /// <summary>Only the company id.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Only the customer id.</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>Only the product type.</summary>
    public ProductTypes? ProductType { get; set; }
}
