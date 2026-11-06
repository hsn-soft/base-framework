using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.SubscriptionDomain.Consts;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;

/// <summary>What can be changed of a subscription.</summary>
public sealed class SubscriptionUpdateDto : IValidatableObject
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    public DateTimeOffset? ValidTo { get; set; }

    [Required]
    public bool IsBlocked { get; set; }

    [StringLength(SubscriptionConsts.SettingsJsonMaxLength)]
    public string? SettingsJson { get; set; }

    // what attributes cannot say: a sale cannot be made to end in the past
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ValidTo is { } end && end <= DateTimeOffset.UtcNow)
        {
            yield return ValidationRule.Failed(nameof(ValidTo), "Validation:InvalidValue");
        }
    }
}
