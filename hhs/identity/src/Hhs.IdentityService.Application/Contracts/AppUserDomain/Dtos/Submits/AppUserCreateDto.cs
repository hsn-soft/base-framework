using System.ComponentModel.DataAnnotations;
using Hhs.IdentityService.Domain.AuthDomain.Consts;
using Hhs.IdentityService.Domain.Localization;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;

public sealed class AppUserCreateDto : IValidatableObject
{
    public Guid? TenantId { get; set; }

    [CanBeNull]
    public string UserName { get; set; }

    [CanBeNull]
    public string Email { get; set; }

    [CanBeNull]
    public string PhoneNumber { get; set; }

    [CanBeNull]
    public string DisplayName { get; set; }

    [CanBeNull]
    public string LanguageCode { get; set; }

    [CanBeNull]
    public string AvatarSuffixUrl { get; set; }

    public List<string> Roles { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var factory = validationContext.GetService(typeof(IStringLocalizerFactory)) as IStringLocalizerFactory;
        var localizer = factory?.CreateMultiple(new List<Type>
        {
            typeof(IdentityServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        });

        if (!CheckSafe.NotNull(TenantId, nameof(TenantId)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.IsNotEmpty], new[] { localizer?["TenantId"].ToString() });
        }

        if (!CheckSafe.NotNullOrEmpty(UserName, nameof(UserName)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], new[] { localizer?["AppUser:UserName"].ToString() });
        }
        else if (!CheckSafe.Length(UserName, nameof(UserName), AppUserConsts.UserNameMaxLength))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, AppUserConsts.UserNameMaxLength], new[] { localizer?["AppUser:UserName"].ToString() });
        }

        if (!CheckSafe.NotNullOrEmpty(Email, nameof(Email)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], new[] { localizer?["AppUser:Email"].ToString() });
        }
        else if (!CheckSafe.Length(Email, nameof(Email), AppUserConsts.EmailMaxLength))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, AppUserConsts.EmailMaxLength], new[] { localizer?["AppUser:Email"].ToString() });
        }

        if (!string.IsNullOrWhiteSpace(PhoneNumber))
        {
            if (!CheckSafe.Length(PhoneNumber, nameof(PhoneNumber), AppUserConsts.PhoneNumberMaxLength))
            {
                yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, AppUserConsts.PhoneNumberMaxLength], new[] { localizer?["AppUser:PhoneNumber"].ToString() });
            }
        }

        if (!string.IsNullOrWhiteSpace(DisplayName))
        {
            if (!CheckSafe.Length(DisplayName, nameof(DisplayName), AppUserConsts.DisplayNameMaxLength))
            {
                yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, AppUserConsts.DisplayNameMaxLength], new[] { localizer?["AppUser:DisplayName"].ToString() });
            }
        }

        if (!string.IsNullOrWhiteSpace(LanguageCode))
        {
            if (!CheckSafe.Length(LanguageCode, nameof(LanguageCode), AppUserConsts.LanguageCodeMaxLength))
            {
                yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, AppUserConsts.LanguageCodeMaxLength], new[] { localizer?["AppUser:LanguageCode"].ToString() });
            }
        }
    }
}