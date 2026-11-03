using HsnSoft.Base.Application.Dtos;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;

public sealed class SubscriptionDto : AuditedEntityDto<Guid>
{
    /// <summary>The reseller that sold it.</summary>
    public Guid TenantId { get; init; }

    /// <summary>When the sale ended (it was ended or passed to another reseller); nothing while it goes on.</summary>
    public DateTimeOffset? EndedAt { get; init; }
    public Guid CompanyId { get; init; }
    public Guid CustomerId { get; init; }
    public ProductTypes ProductType { get; init; }
    public string ScopeKey { get; init; } = string.Empty;
    public DateTimeOffset ValidFrom { get; init; }
    public DateTimeOffset? ValidTo { get; init; }
    public bool IsBlocked { get; init; }
    public string? SettingsJson { get; init; }
}
