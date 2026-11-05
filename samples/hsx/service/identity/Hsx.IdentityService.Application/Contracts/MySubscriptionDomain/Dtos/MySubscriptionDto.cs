using HsnSoft.Base.Application.Dtos;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Dtos;

/// <summary>A subscription the caller may see the data of, as the scope selector of a client needs it: who sold it, to which customer of which company, which product, and the scope key the data of that product is kept under.</summary>
public sealed class MySubscriptionDto : IBaseDto
{
    public Guid SubscriptionId { get; init; }

    /// <summary>The reseller that sold it.</summary>
    public Guid TenantId { get; init; }

    public string TenantTitle { get; init; } = string.Empty;

    public Guid CompanyId { get; init; }

    public string CompanyTitle { get; init; } = string.Empty;

    public Guid CustomerId { get; init; }

    public string CustomerDomain { get; init; } = string.Empty;

    public ProductTypes ProductType { get; init; }

    public string ScopeKey { get; init; } = string.Empty;

    public DateTimeOffset ValidFrom { get; init; }

    public DateTimeOffset? ValidTo { get; init; }

    public bool IsBlocked { get; init; }
}
