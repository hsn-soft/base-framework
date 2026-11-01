using HsnSoft.Base.Application.Dtos;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;

public sealed class SubscriptionDto : AuditedEntityDto<Guid>
{
    public Guid ResellerTenantId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid CustomerId { get; init; }
    public ProductTypes ProductType { get; init; }
    public string ScopeKey { get; init; } = string.Empty;
    public DateTimeOffset ValidFrom { get; init; }
    public DateTimeOffset? ValidTo { get; init; }
    public bool IsBlocked { get; init; }
    public string? SettingsJson { get; init; }
}
