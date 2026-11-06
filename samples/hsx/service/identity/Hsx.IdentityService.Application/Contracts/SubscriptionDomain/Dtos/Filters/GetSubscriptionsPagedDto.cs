using HsnSoft.Base.Application.Dtos;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Filters;

public sealed class GetSubscriptionsPagedDto : PagedDataRequestDto
{
    /// <summary>Only the sales of this reseller tenant.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Only the ended ones (the history) or only the ones that go on; all when it is not given.</summary>
    public bool? IsEnded { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? CustomerId { get; set; }

    public ProductTypes? ProductType { get; set; }
}
