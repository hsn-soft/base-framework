using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.SubscriptionDomain.Consts;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;

public sealed class SubscriptionCreateDto
{
    [Required]
    [NotDefault]
    public Guid? ResellerTenantId { get; set; }

    [Required]
    [NotDefault]
    public Guid? CompanyId { get; set; }

    [Required]
    [NotDefault]
    public Guid? CustomerId { get; set; }

    [Required]
    public ProductTypes? ProductType { get; set; }

    public DateTimeOffset? ValidTo { get; set; }

    [Required]
    public bool IsBlocked { get; set; }

    [StringLength(SubscriptionConsts.SettingsJsonMaxLength)]
    public string? SettingsJson { get; set; }
}
