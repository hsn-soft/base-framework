using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.SubscriptionDomain.Consts;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;

/// <summary>A sale, recorded by the system tenant: a reseller sells a product to a customer. The company is the customer's.</summary>
public sealed class SubscriptionCreateDto : IValidatableObject
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

    /// <summary>Until when the sale is good; nothing for no end.</summary>
    public DateTimeOffset? ValidTo { get; set; }

    [StringLength(SubscriptionConsts.SettingsJsonMaxLength)]
    public string? SettingsJson { get; set; }

    // what attributes cannot say: a sale that is over before it begins is a mistake
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ValidTo is { } end && end <= DateTimeOffset.UtcNow)
        {
            yield return ValidationRule.Failed(nameof(ValidTo), "Validation:InvalidValue");
        }
    }
}
