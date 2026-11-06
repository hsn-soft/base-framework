using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.IdentityService.Domain.AppUserDomain.Consts;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos.Submits;

/// <summary>The first administrator of a reseller that is made. There is no password: the system makes a temporary one and tells the user by <see cref="NotificationType"/>.</summary>
public sealed class AdminUserInputDto : IValidatableObject
{
    [Required]
    [StringLength(AppUserConsts.UserNameMaxLength)]
    public string? UserName { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(AppUserConsts.EmailMaxLength)]
    public string? Email { get; set; }

    [Required]
    [StringLength(AppUserConsts.DisplayNameMaxLength)]
    public string? DisplayName { get; set; }

    /// <summary>How the administrator is told its first password: by an email, or by a text message (then the phone number is given).</summary>
    public NotificationTypes NotificationType { get; set; } = NotificationTypes.Email;

    [StringLength(AppUserConsts.PhoneNumberMaxLength)]
    public string? PhoneNumber { get; set; }

    [StringLength(AppUserConsts.LanguageCodeMaxLength)]
    public string? LanguageCode { get; set; }

    // what attributes cannot say: a text message needs a phone number to go to
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NotificationType == NotificationTypes.Sms && string.IsNullOrWhiteSpace(PhoneNumber))
        {
            yield return ValidationRule.Failed(nameof(PhoneNumber), "Validation:InvalidValue");
        }

        if (!Enum.IsDefined(NotificationType))
        {
            yield return ValidationRule.Failed(nameof(NotificationType), "Validation:InvalidValue");
        }
    }
}
