using System.ComponentModel.DataAnnotations;
using Hhs.AuthServer.Localization;
using Hhs.IdentityService.Domain.Localization;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Serilog.Mask;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.AuthServer.Models;

public sealed class JwtPasswordTokenRequestDto : BaseTokenRequestDto, IValidatableObject
{
    [CanBeNull]
    public string TenantName { get; set; }

    [CanBeNull]
    public string Email { get; set; }

    [SensitiveData]
    [CanBeNull]
    public string Password { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var factory = validationContext.GetService(typeof(IStringLocalizerFactory)) as IStringLocalizerFactory;
        var localizer = factory?.CreateMultiple([
            typeof(AuthServerResource),
            typeof(IdentityServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        ]);

        if (!CheckSafe.NotNullOrWhiteSpace(Email, nameof(Email)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], [localizer?[nameof(Email)].ToString()]);
        }

        if (!CheckSafe.NotNullOrWhiteSpace(Password, nameof(Password)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], [localizer?[nameof(Password)].ToString()]);
        }

        if (!CheckSafe.NotNullOrWhiteSpace(ClientId, nameof(ClientId)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], [localizer?[nameof(ClientId)].ToString()]);
        }

        if (!CheckSafe.NotNullOrWhiteSpace(GrantType, nameof(GrantType)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], [localizer?[nameof(GrantType)].ToString()]);
        }
    }
}