using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.SubscriptionDomain.Consts;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;

/// <summary>A sale, recorded by the system tenant: a reseller sells a product to a customer. The company is the customer's.</summary>
public sealed class SubscriptionCreateDto
{
    /// <summary>The reseller that sells it.</summary>
    [Required]
    [NotDefault]
    public Guid? TenantId { get; set; }

    [Required]
    [NotDefault]
    public Guid? CustomerId { get; set; }

    [Required]
    public ProductTypes? ProductType { get; set; }

    public DateTimeOffset? ValidTo { get; set; }

    [StringLength(SubscriptionConsts.SettingsJsonMaxLength)]
    public string? SettingsJson { get; set; }
}
