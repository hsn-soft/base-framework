using System.ComponentModel.DataAnnotations;
using Hhs.IdentityService.Domain.AppUserDomain.Consts;
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
    public string TenantDomain { get; set; }

    [CanBeNull]
    public string UserName { get; set; }

    [CanBeNull]
    public string Email { get; set; }

    [CanBeNull]
    public string PhoneNumber { get; set; }

    [CanBeNull]
    public string Name { get; set; }

    [CanBeNull]
    public string Surname { get; set; }

    [CanBeNull]
    public string DefaultLanguage { get; set; }

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

        if (!CheckSafe.NotNullOrEmpty(TenantDomain, nameof(TenantDomain)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], new[] { localizer?["TenantDomain"].ToString() });
        }
        else if (!CheckSafe.Length(TenantDomain, nameof(TenantDomain), AppUserConsts.TenantDomainMaxLength))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, AppUserConsts.TenantDomainMaxLength], new[] { localizer?["TenantDomain"].ToString() });
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

        if (!string.IsNullOrWhiteSpace(Name))
        {
            if (!CheckSafe.Length(Name, nameof(Name), AppUserConsts.NameMaxLength))
            {
                yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, AppUserConsts.NameMaxLength], new[] { localizer?["AppUser:Name"].ToString() });
            }
        }

        if (!string.IsNullOrWhiteSpace(Surname))
        {
            if (!CheckSafe.Length(Surname, nameof(Surname), AppUserConsts.SurnameMaxLength))
            {
                yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, AppUserConsts.SurnameMaxLength], new[] { localizer?["AppUser:Surname"].ToString() });
            }
        }

        if (!string.IsNullOrWhiteSpace(DefaultLanguage))
        {
            if (!CheckSafe.Length(DefaultLanguage, nameof(DefaultLanguage), AppUserConsts.DefaultLanguageMaxLength))
            {
                yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, AppUserConsts.DefaultLanguageMaxLength], new[] { localizer?["AppUser:DefaultLanguage"].ToString() });
            }
        }
    }
}